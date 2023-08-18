using VisusCore.Storage.Abstractions.Models;

namespace VisusCore.Storage.Core.Models;

public sealed class StreamStorageSegment : StreamStorageEntity, IStreamSegmentKey
{
    public string Provider { get; set; }
    public long InitId { get; set; }
    public long TimestampUtc { get; set; }
    public long Duration { get; set; }
    public long? TimestampProvided { get; set; }
    public long FrameCount { get; set; }
    public long Size { get; set; }
    public long CreatedUtc { get; set; }

    long IStreamSegmentKey.DocumentId => Id;
}
