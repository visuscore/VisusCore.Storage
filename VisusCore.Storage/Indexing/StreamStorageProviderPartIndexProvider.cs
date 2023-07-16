using OrchardCore.ContentManagement;
using System;
using VisusCore.AidStack.OrchardCore.Parts.Indexing;
using VisusCore.Storage.Core.Models;
using VisusCore.Storage.Models;

namespace VisusCore.Storage.Indexing;

public class StreamStorageProviderPartIndexProvider
    : ContentPartIndexProvider<StreamStorageProviderPart, StreamStorageProviderPartIndex>
{
    public StreamStorageProviderPartIndexProvider(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    protected override StreamStorageProviderPartIndex CreateIndex(
        StreamStorageProviderPart part,
        ContentItem contentItem) =>
        new()
        {
            ContentItemId = contentItem.ContentItemId,
            ContentItemVersionId = contentItem.ContentItemVersionId,
            ContentType = contentItem.ContentType,
            Latest = contentItem.Latest,
            Published = contentItem.Published,
            Provider = part.Provider,
        };
}
