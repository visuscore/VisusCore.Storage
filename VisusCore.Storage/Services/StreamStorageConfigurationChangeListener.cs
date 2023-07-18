using System;
using VisusCore.EventBus.Core.Services;
using VisusCore.Storage.Core.Events;

namespace VisusCore.Storage.Services;

public class StreamStorageConfigurationChangeListener
{
    private readonly ReactiveEventConsumer<StreamStoragePublishedEvent> _streamStoragePublished;
    private readonly ReactiveEventConsumer<StreamStorageRemovedEvent> _streamStorageRemoved;
    private readonly ReactiveEventConsumer<StreamStorageUnpublishedEvent> _streamStorageUnpublished;
    private readonly ReactiveEventConsumer<StreamStorageUpdatedEvent> _streamStorageUpdated;

    public IAsyncObservable<StreamStoragePublishedEvent> StreamStoragePublished => _streamStoragePublished.Events;
    public IAsyncObservable<StreamStorageRemovedEvent> StreamStorageRemoved => _streamStorageRemoved.Events;
    public IAsyncObservable<StreamStorageUnpublishedEvent> StreamStorageUnpublished => _streamStorageUnpublished.Events;
    public IAsyncObservable<StreamStorageUpdatedEvent> StreamStorageUpdated => _streamStorageUpdated.Events;

    public StreamStorageConfigurationChangeListener(
        ReactiveEventConsumer<StreamStoragePublishedEvent> streamStoragePublished,
        ReactiveEventConsumer<StreamStorageRemovedEvent> streamStorageRemoved,
        ReactiveEventConsumer<StreamStorageUnpublishedEvent> streamStorageUnpublished,
        ReactiveEventConsumer<StreamStorageUpdatedEvent> streamStorageUpdated)
    {
        _streamStoragePublished = streamStoragePublished;
        _streamStorageRemoved = streamStorageRemoved;
        _streamStorageUnpublished = streamStorageUnpublished;
        _streamStorageUpdated = streamStorageUpdated;
    }
}
