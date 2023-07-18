namespace VisusCore.Storage.Core.Events;

public class StreamStoragePublishedEvent : StreamStorageEvent
{
    public StreamStoragePublishedEvent()
    {
    }

    public StreamStoragePublishedEvent(string streamId)
        : base(streamId)
    {
    }
}
