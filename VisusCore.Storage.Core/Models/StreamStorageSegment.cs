namespace VisusCore.Storage.Core.Models;

public class StreamStorageSegment : StreamStorageEntity
{
    public string Provider { get; set; }
    public long InitId { get; set; }
    public long TimestampUtc { get; set; }
    public long Duration { get; set; }
    public long? TimestampProvided { get; set; }
    public long FrameCount { get; set; }
    public long Size { get; set; }
    public long CreatedUtc { get; set; }
}
