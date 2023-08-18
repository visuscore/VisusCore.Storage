using Microsoft.Extensions.Logging;
using OrchardCore.BackgroundTasks;
using OrchardCore.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.AidStack.Collections.Generic;
using VisusCore.AidStack.OrchardCore.Parts.Indexing.Models;
using VisusCore.Consumer.Abstractions.Models;
using VisusCore.Storage.Abstractions.Services;
using VisusCore.Storage.Core.Models;
using YesSql;

namespace VisusCore.Storage.Services;

[BackgroundTask(Schedule = "0 0 * * *", Description = "Storage size limit maintainer.")]
public class StreamSegmentStorageMaintainerBackgroundTask : IBackgroundTask
{
    private const int MinQueryPageSize = 10;
    private const int MaxQueryPageSize = 100000;
    private const int MaxDeletePageSize = 100000;
    private readonly IStreamSegmentStorageReader _streamSegmentStorageReader;
    private readonly StreamSegmentStorageMaintainer _streamSegmentStorageMaintainer;
    private readonly ISession _session;
    private readonly IClock _clock;
    private readonly ILogger _logger;

    public StreamSegmentStorageMaintainerBackgroundTask(
        IStreamSegmentStorageReader streamSegmentStorageReader,
        StreamSegmentStorageMaintainer streamSegmentStorageMaintainer,
        ISession session,
        IClock clock,
        ILogger<StreamSegmentStorageMaintainerBackgroundTask> logger)
    {
        _streamSegmentStorageReader = streamSegmentStorageReader;
        _streamSegmentStorageMaintainer = streamSegmentStorageMaintainer;
        _session = session;
        _clock = clock;
        _logger = logger;
    }

    public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var sizeLimits = (await _session.QueryIndex<StreamStorageSizeLimitPartIndex>()
            .Where(index => index.EnableSizeLimit && index.Latest && index.Published)
            .ListAsync())
            .ToArray()
            .OfType<ContentPartIndex>();
        var timeLimits = (await _session.QueryIndex<StreamStorageTimeLimitPartIndex>()
            .Where(index => index.EnableTimeLimit && index.Latest && index.Published)
            .ListAsync())
            .ToArray()
            .OfType<ContentPartIndex>();
        var limits = sizeLimits.Concat(timeLimits)
            .GroupBy(limit => limit.ContentItemId)
            .Select(limit => (
                StreamId: limit.Key,
                SizeLimit: limit.OfType<StreamStorageSizeLimitPartIndex>().FirstOrDefault(),
                TimeLimit: limit.OfType<StreamStorageTimeLimitPartIndex>().FirstOrDefault()));

        var calculatedSizeUsed = limits.Join(
            await limits.ToAsyncEnumerable()
                .SelectAwait(async limit =>
                {
                    if (limit.SizeLimit is not null
                        && limit.SizeLimit.EnableSizeLimit
                        && limit.SizeLimit.SizeLimitMegabytes > 0)
                    {
                        try
                        {
                            var storageUsage = await GetUsageDataByLimitAsync(limit.SizeLimit, cancellationToken);

                            return (Limit: limit, storageUsage.UsedStorageSize, storageUsage.SegmentCount);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Failed to get usage data.");
                        }
                    }

                    return (limit, 0, 0);
                })
                .ToListAsync(cancellationToken),
            record => record.StreamId,
            record => record.Limit.StreamId,
            (limit, usedStorage) => (limit.StreamId, limit.TimeLimit, limit.SizeLimit, UsedStorage: usedStorage));

        foreach (var (streamId, timeLimit, sizeLimit, usedStorage) in calculatedSizeUsed)
        {
            var segmentsToRemove = new List<IVideoStreamSegmentMetadata>();

            try
            {
                if (sizeLimit is not null && sizeLimit.EnableSizeLimit && usedStorage.UsedStorageSize > 0)
                {
                    segmentsToRemove.AddRange(
                        await GetSegmentsToRemoveAsync(
                            sizeLimit,
                            usedStorage.UsedStorageSize,
                            usedStorage.SegmentCount,
                            cancellationToken));
                }

                if (timeLimit is not null && timeLimit.EnableTimeLimit)
                {
                    segmentsToRemove.AddRange(
                        await GetSegmentsToRemoveAsync(
                            timeLimit,
                            segmentsToRemove.Any()
                                ? segmentsToRemove.Select(metadata => metadata.TimestampUtc).Max()
                                : 0,
                            cancellationToken));
                }

                await RemoveSegmentsAsync(segmentsToRemove, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to get segments to remove.");
                continue;
            }
        }
    }

    private async Task RemoveSegmentsAsync(IEnumerable<IVideoStreamSegmentMetadata> segmentsToRemove, CancellationToken cancellationToken)
    {
        if (!segmentsToRemove.Any())
        {
            return;
        }

        try
        {
            await _streamSegmentStorageMaintainer.RemoveSegmentsAsync(
                segmentsToRemove
                    .Distinct(
                        new GenericEqualityComparer<IVideoStreamSegmentMetadata>(
                            (left, right) => left?.StreamId == right?.StreamId
                                && left?.TimestampUtc == right?.TimestampUtc,
                            meta => (meta?.StreamId, meta?.TimestampUtc).GetHashCode()))
                    .Take(MaxDeletePageSize),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to remove segments.");
        }
    }

    private async Task<(
        StreamStorageSizeLimitPartIndex Limit,
        long UsedStorageSize,
        int SegmentCount)> GetUsageDataByLimitAsync(StreamStorageSizeLimitPartIndex limit, CancellationToken cancellationToken)
    {
        long usedStorageSize = 0;
        int segmentCount = 0;
        var segmentMetaPage = default(IEnumerable<IVideoStreamSegmentMetadata>);

        do
        {
            segmentMetaPage = await _streamSegmentStorageReader.GetSegmentMetasAsync(
                limit.ContentItemId,
                0,
                endTimestampUtc: null,
                segmentCount,
                MaxQueryPageSize,
                cancellationToken);

            if (segmentMetaPage?.Any() is true)
            {
                usedStorageSize += segmentMetaPage.Sum(meta => meta.Size);
                segmentCount += segmentMetaPage.Count();
            }
        }
        while (segmentMetaPage?.Any() is true && segmentMetaPage.Count() == MaxQueryPageSize);

        return (Limit: limit, UsedStorageSize: usedStorageSize, SegmentCount: segmentCount);
    }

    private async Task<IEnumerable<IVideoStreamSegmentMetadata>> GetSegmentsToRemoveAsync(
        StreamStorageSizeLimitPartIndex limit,
        long usedStorageSize,
        int segmentCount,
        CancellationToken cancellationToken)
    {
        var sizeLimitBytes = limit.SizeLimitMegabytes * 1_048_576;
        if (sizeLimitBytes <= 0 || usedStorageSize <= sizeLimitBytes || segmentCount <= 0)
        {
            return Enumerable.Empty<IVideoStreamSegmentMetadata>();
        }

        var averageSegmentSize = usedStorageSize / segmentCount;
        var segmentsToRemove = new List<IVideoStreamSegmentMetadata>();
        var queryPageSize = Convert.ToInt32(
            Math.Min(
                Math.Max(
                    (usedStorageSize - sizeLimitBytes) / averageSegmentSize, MinQueryPageSize),
                MaxQueryPageSize));
        var runningStorageSize = usedStorageSize;
        IEnumerable<IVideoStreamSegmentMetadata> segmentMetaPage;

        do
        {
            segmentMetaPage = await _streamSegmentStorageReader.GetSegmentMetasAsync(
                limit.ContentItemId,
                0,
                endTimestampUtc: null,
                segmentsToRemove.Count,
                queryPageSize,
                cancellationToken);
            var segmentsEnumerator = segmentMetaPage.GetEnumerator();
            while (runningStorageSize > sizeLimitBytes && segmentsEnumerator.MoveNext())
            {
                segmentsToRemove.Add(segmentsEnumerator.Current);
                runningStorageSize -= segmentsEnumerator.Current.Size;
            }
        }
        while (runningStorageSize > sizeLimitBytes
            && segmentMetaPage.Count() == queryPageSize
            && segmentsToRemove.Count < MaxDeletePageSize);

        return segmentsToRemove;
    }

    private async Task<IEnumerable<IVideoStreamSegmentMetadata>> GetSegmentsToRemoveAsync(
        StreamStorageTimeLimitPartIndex limit,
        long after,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetUnixTimeMilliseconds() * 1000;
        var timeLimit = limit.TimeLimitHours * 3_600_000_000;
        var until = now - timeLimit;
        if (!limit.EnableTimeLimit || timeLimit <= 0 || after > until)
        {
            return Enumerable.Empty<IVideoStreamSegmentMetadata>();
        }

        var segmentsToRemove = new List<IVideoStreamSegmentMetadata>();
        IEnumerable<IVideoStreamSegmentMetadata> segmentMetaPage;

        do
        {
            segmentMetaPage = await _streamSegmentStorageReader.GetSegmentMetasAsync(
                limit.ContentItemId,
                after,
                endTimestampUtc: until,
                segmentsToRemove.Count,
                MaxQueryPageSize,
                cancellationToken);
            segmentsToRemove.AddRange(segmentMetaPage);
        }
        while (segmentMetaPage.Count() == MaxQueryPageSize
            && segmentsToRemove.Count < MaxDeletePageSize);

        return segmentsToRemove;
    }
}
