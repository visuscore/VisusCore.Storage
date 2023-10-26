using OrchardCore.ContentManagement;
using System;
using VisusCore.AidStack.OrchardCore.Parts.Indexing;
using VisusCore.Storage.Core.Models;
using VisusCore.Storage.Models;

namespace VisusCore.Storage.Indexing;

public class StreamStorageTimeLimitPartIndexProvider
    : ContentPartIndexProvider<StreamStorageTimeLimitPart, StreamStorageTimeLimitPartIndex>
{
    public StreamStorageTimeLimitPartIndexProvider(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    protected override StreamStorageTimeLimitPartIndex CreateIndex(
        StreamStorageTimeLimitPart part,
        ContentItem contentItem)
    {
        if (part is null)
        {
            throw new ArgumentNullException(nameof(part));
        }

        if (contentItem is null)
        {
            throw new ArgumentNullException(nameof(contentItem));
        }

        return new()
        {
            ContentItemId = contentItem.ContentItemId,
            ContentItemVersionId = contentItem.ContentItemVersionId,
            ContentType = contentItem.ContentType,
            Latest = contentItem.Latest,
            Published = contentItem.Published,
            EnableTimeLimit = part.EnableTimeLimit,
            TimeLimitHours = part.TimeLimitHours,
        };
    }
}
