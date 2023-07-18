namespace VisusCore.Storage.Core.Events;

public abstract class StreamStorageEvent
{
    public string StreamId { get; set; }

    protected StreamStorageEvent()
    {
    }

    protected StreamStorageEvent(string streamId) =>
        StreamId = streamId;
}
