namespace VisusCore.Storage.Core.Events;

public class StreamStorageUpdatedEvent : StreamStorageEvent
{
    public StreamStorageUpdatedEvent()
    {
    }

    public StreamStorageUpdatedEvent(string streamId)
        : base(streamId)
    {
    }
}
