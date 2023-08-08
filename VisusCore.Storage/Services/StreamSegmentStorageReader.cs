using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.Consumer.Abstractions.Models;
using VisusCore.Storage.Abstractions.Services;
using VisusCore.Storage.Core.Models;
using VisusCore.Storage.Models;
using YesSql;

namespace VisusCore.Storage.Services;

public class StreamSegmentStorageReader : IStreamSegmentStorageReader
{
    private readonly IEnumerable<IStreamSegmentStorage> _streamSegmentStorages;
    private readonly ISession _session;

    public StreamSegmentStorageReader(IEnumerable<IStreamSegmentStorage> streamSegmentStorages, ISession session)
    {
        _streamSegmentStorages = streamSegmentStorages;
        _session = session;
    }

    public async Task<IEnumerable<IVideoStreamSegment>> GetSegmentsAsync(
        string streamId,
        long startTimestampUtc,
        long? endTimestampUtc,
        int? skip,
        int? take,
        CancellationToken cancellationToken = default)
    {
        var metas = await GetSegmentIndexesAsync(streamId, startTimestampUtc, endTimestampUtc, skip, take);

        return (await metas.GroupBy(meta => meta.Provider)
            .ToAsyncEnumerable()
            .SelectManyAwait(
                async item =>
                {
                    var provider = _streamSegmentStorages.FirstOrDefault(storage => storage.Provider == item.Key);
                    if (provider is null)
                    {
                        return Enumerable.Empty<IVideoStreamSegment>()
                            .ToAsyncEnumerable();
                    }

                    return (await provider.GetSegmentsByKeyAsync(
                        item,
                        converterAsync: (index, init, data) =>
                            Task.FromResult<IVideoStreamSegment>(new VideoStreamSegment
                            {
                                StreamId = index.StreamId,
                                Metadata = SegmentIndexToSegmentMetadata(index),
                                Init = init,
                                Data = data,
                            }),
                        cancellationToken))
                        .ToAsyncEnumerable();
                })
            .ToListAsync(cancellationToken))
            .OrderBy(segment => segment.Metadata.TimestampUtc);
    }

    public async Task<IEnumerable<IVideoStreamSegmentMetadata>> GetSegmentMetasAsync(
        string streamId,
        long startTimestampUtc,
        long? endTimestampUtc,
        int? skip,
        int? take,
        CancellationToken cancellationToken = default) =>
        (await GetSegmentIndexesAsync(streamId, startTimestampUtc, endTimestampUtc, skip, take))
            .Select(SegmentIndexToSegmentMetadata);

    public Task<IVideoStreamSegment> GetSegmentAroundAsync(
        string streamId,
        long expectedTimestampUtc,
        CancellationToken cancellationToken = default) =>
        GetSegmentAroundAsync(streamId, expectedTimestampUtc, latestBefore: false, cancellationToken);

    public async Task<IVideoStreamSegmentMetadata> GetSegmentMetaAroundAsync(string streamId, long expectedTimestampUtc) =>
        SegmentIndexToSegmentMetadata(await GetSegmentIndexAroundAsync(streamId, expectedTimestampUtc));

    public Task<IVideoStreamSegment> GetSegmentLatestBeforeAsync(
        string streamId,
        long expectedTimestampUtc,
        CancellationToken cancellationToken = default) =>
        GetSegmentAroundAsync(streamId, expectedTimestampUtc, latestBefore: true, cancellationToken);

    public async Task<IVideoStreamSegmentMetadata> GetSegmentMetaLatestBeforeAsync(string streamId, long expectedTimestampUtc) =>
        SegmentIndexToSegmentMetadata(await GetSegmentIndexAroundAsync(streamId, expectedTimestampUtc, latestBefore: true));

    private async Task<IVideoStreamSegment> GetSegmentAroundAsync(
        string streamId,
        long expectedTimestampUtc,
        bool latestBefore,
        CancellationToken cancellationToken = default)
    {
        var meta = await GetSegmentIndexAroundAsync(streamId, expectedTimestampUtc, latestBefore);
        if (meta is null)
        {
            return null;
        }

        var provider = _streamSegmentStorages.FirstOrDefault(storage => storage.Provider == meta.Provider);
        if (provider is null)
        {
            return null;
        }

        return (await provider.GetSegmentsByKeyAsync(
            new[] { meta },
            converterAsync: (index, init, data) =>
                Task.FromResult<IVideoStreamSegment>(new VideoStreamSegment
                {
                    StreamId = index.StreamId,
                    Metadata = SegmentIndexToSegmentMetadata(index),
                    Init = init,
                    Data = data,
                }),
            cancellationToken))
            .FirstOrDefault();
    }

    private async Task<IEnumerable<StreamStorageSegmentIndex>> GetSegmentIndexesAsync(
        string streamId,
        long startTimestampUtc,
        long? endTimestampUtc,
        int? skip,
        int? take)
    {
        var storageIndexQuery = _session.QueryIndex<StreamStorageSegmentIndex>()
            .Where(index =>
                index.StreamId == streamId
                && index.TimestampUtc >= startTimestampUtc);
        if (endTimestampUtc is not null)
        {
            storageIndexQuery = storageIndexQuery.Where(index => index.TimestampUtc <= endTimestampUtc.Value);
        }

        storageIndexQuery = storageIndexQuery.OrderBy(index => index.TimestampUtc);
        if (skip is not null)
        {
            storageIndexQuery = storageIndexQuery.Skip(skip.Value);
        }

        if (take is not null)
        {
            storageIndexQuery = storageIndexQuery.Take(take.Value);
        }

        return (await storageIndexQuery.ListAsync())
            .ToArray();
    }

    private Task<StreamStorageSegmentIndex> GetSegmentIndexAroundAsync(
        string streamId,
        long expectedTimestampUtc,
        bool latestBefore = false)
    {
        var query = _session.QueryIndex<StreamStorageSegmentIndex>()
            .Where(index =>
                index.StreamId == streamId
                && index.TimestampUtc <= expectedTimestampUtc);
        query = latestBefore
            ? query.OrderByDescending(index => index.TimestampUtc)
            : query.Where(index => index.TimestampUtc + index.Duration >= expectedTimestampUtc)
                .OrderBy(index => index.TimestampUtc);

        return query.FirstOrDefaultAsync();
    }

    private static VideoStreamSegmentMetadata SegmentIndexToSegmentMetadata(StreamStorageSegmentIndex indexRecord) =>
        new()
        {
            Duration = indexRecord.Duration,
            FrameCount = indexRecord.FrameCount,
            StreamId = indexRecord.StreamId,
            TimestampProvided = indexRecord.TimestampProvided,
            TimestampUtc = indexRecord.TimestampUtc,
        };
}

internal sealed class VideoStreamSegment : IVideoStreamSegment
{
    private readonly byte[] _data;

    public string StreamId { get; init; }

    public IVideoStreamSegmentMetadata Metadata { get; init; }

    public IVideoStreamInit Init { get; init; }

    public ReadOnlySpan<byte> Data { get => _data; init => _data = value.ToArray(); }
}
