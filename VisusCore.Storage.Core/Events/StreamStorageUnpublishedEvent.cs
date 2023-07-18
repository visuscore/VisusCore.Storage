namespace VisusCore.Storage.Core.Events;

public class StreamStorageUnpublishedEvent : StreamStorageEvent
{
    public StreamStorageUnpublishedEvent()
    {
    }

    public StreamStorageUnpublishedEvent(string streamId)
        : base(streamId)
    {
    }
}
