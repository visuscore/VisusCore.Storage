namespace VisusCore.Storage.Core.Events;

public class StreamStorageRemovedEvent : StreamStorageEvent
{
    public StreamStorageRemovedEvent()
    {
    }

    public StreamStorageRemovedEvent(string streamId)
        : base(streamId)
    {
    }
}
