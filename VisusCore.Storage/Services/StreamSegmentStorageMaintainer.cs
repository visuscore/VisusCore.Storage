using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.AidStack.Extensions;
using VisusCore.Consumer.Abstractions.Models;
using VisusCore.Storage.Abstractions.Services;
using VisusCore.Storage.Core.Models;
using YesSql;
using YesSql.Services;

namespace VisusCore.Storage.Services;

public class StreamSegmentStorageMaintainer
{
    private const int BatchSize = 10;
    private readonly IEnumerable<IStreamSegmentStorage> _streamSegmentStorages;
    private readonly ISession _session;
    private readonly ILogger _logger;

    public StreamSegmentStorageMaintainer(
        IEnumerable<IStreamSegmentStorage> streamSegmentStorages,
        ISession session,
        ILogger<StreamSegmentStorageMaintainer> logger)
    {
        _streamSegmentStorages = streamSegmentStorages;
        _session = session;
        _logger = logger;
    }

    public async Task RemoveSegmentsAsync(
        IEnumerable<IVideoStreamSegmentMetadata> segments,
        CancellationToken cancellationToken = default)
    {
        var segmentIndexes = await segments.GroupBy(segment => segment.StreamId)
            .ToAsyncEnumerable()
            .SelectManyAwait(async group =>
            {
                var indexes = new List<StreamStorageSegmentIndex>();
                foreach (var partition in group.Partition(BatchSize))
                {
                    try
                    {
                        indexes.AddRange(
                            (await _session.QueryIndex<StreamStorageSegmentIndex>()
                                .Where(index =>
                                    index.StreamId == group.Key
                                    && index.TimestampUtc.IsIn(partition.Select(metadata => metadata.TimestampUtc)))
                                .ListAsync())
                                .ToArray());
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Failed to get segment indexes.");
                    }
                }

                return indexes.ToAsyncEnumerable();
            })
            .ToListAsync(cancellationToken);

        foreach (var segmentGroup in segmentIndexes.GroupBy(index => index.Provider))
        {
            var provider = _streamSegmentStorages.FirstOrDefault(storage => storage.Provider == segmentGroup.Key);
            if (provider is null)
            {
                continue;
            }

            await provider.DeleteSegmentsByKeyAsync(segmentGroup, cancellationToken);
        }
    }
}
