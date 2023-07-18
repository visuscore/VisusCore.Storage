using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Handlers;
using System;
using System.Threading.Tasks;
using Tingle.EventBus;
using VisusCore.Storage.Core.Events;
using VisusCore.Storage.Models;

namespace VisusCore.Storage.Handlers;

public class StreamStorageConfigurationChangeHandler : ContentHandlerBase
{
    private readonly IEventPublisher _eventPublisher;

    public StreamStorageConfigurationChangeHandler(IEventPublisher eventPublisher) =>
        _eventPublisher = eventPublisher;

    public override Task PublishedAsync(PublishContentContext context) =>
        PublishEventConditionallyAsync(context);

    public override Task RemovedAsync(RemoveContentContext context) =>
        PublishEventConditionallyAsync(context);

    public override Task UnpublishedAsync(PublishContentContext context) =>
        PublishEventConditionallyAsync(context, unpublish: true);

    public override Task UpdatedAsync(UpdateContentContext context) =>
        PublishEventConditionallyAsync(context);

    private async Task PublishEventConditionallyAsync(ContentContextBase context, bool unpublish = false)
    {
        if (!context.ContentItem.Has<StreamStorageModePart>()
            && !context.ContentItem.Has<StreamStorageProviderPart>()
            && !context.ContentItem.Has<StreamStorageSizeLimitPart>()
            && !context.ContentItem.Has<StreamStorageTimeLimitPart>())
        {
            return;
        }

        switch (context)
        {
            case PublishContentContext when unpublish:
                await _eventPublisher.PublishAsync(
                    new StreamStorageUnpublishedEvent(context.ContentItem.ContentItemId));
                break;
            case PublishContentContext when !unpublish:
                await _eventPublisher.PublishAsync(
                    new StreamStoragePublishedEvent(context.ContentItem.ContentItemId));
                break;
            case RemoveContentContext:
                await _eventPublisher.PublishAsync(
                    new StreamStorageRemovedEvent(context.ContentItem.ContentItemId));
                break;
            case UpdateContentContext:
                await _eventPublisher.PublishAsync(
                    new StreamStorageUpdatedEvent(context.ContentItem.ContentItemId));
                break;
            default:
                throw new InvalidOperationException($"Unsupported {nameof(context)} type '{context.GetType().FullName}'.");
        }
    }
}
