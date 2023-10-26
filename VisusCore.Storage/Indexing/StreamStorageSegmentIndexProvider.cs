using System;
using VisusCore.Storage.Core.Models;
using YesSql.Indexes;

namespace VisusCore.Storage.Indexing;

public class StreamStorageSegmentIndexProvider : IndexProvider<StreamStorageSegment>
{
    public override void Describe(DescribeContext<StreamStorageSegment> context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.For<StreamStorageSegmentIndex>()
            .Map(streamStorageSegment => new StreamStorageSegmentIndex
            {
                StreamId = streamStorageSegment.StreamId,
                Provider = streamStorageSegment.Provider,
                Duration = streamStorageSegment.Duration,
                FrameCount = streamStorageSegment.FrameCount,
                InitId = streamStorageSegment.InitId,
                TimestampProvided = streamStorageSegment.TimestampProvided,
                TimestampUtc = streamStorageSegment.TimestampUtc,
                Size = streamStorageSegment.Size,
                CreatedUtc = streamStorageSegment.CreatedUtc,
            });
    }
}
