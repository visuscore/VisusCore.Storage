using VisusCore.Consumer.Abstractions.Models;

namespace VisusCore.Storage.Models;

public class VideoStreamSegmentMetadata : IVideoStreamSegmentMetadata
{
    public string StreamId { get; init; }

    public long TimestampUtc { get; init; }

    public long Duration { get; init; }

    public long? TimestampProvided { get; init; }

    public long FrameCount { get; init; }
}
