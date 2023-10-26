using OrchardCore.ContentManagement;
using System;
using VisusCore.AidStack.OrchardCore.Parts.Indexing;
using VisusCore.Storage.Core.Models;
using VisusCore.Storage.Models;

namespace VisusCore.Storage.Indexing;

public class StreamStorageModePartIndexProvider
    : ContentPartIndexProvider<StreamStorageModePart, StreamStorageModePartIndex>
{
    public StreamStorageModePartIndexProvider(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    protected override StreamStorageModePartIndex CreateIndex(
        StreamStorageModePart part,
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
            Mode = part.StorageMode,
        };
    }
}
