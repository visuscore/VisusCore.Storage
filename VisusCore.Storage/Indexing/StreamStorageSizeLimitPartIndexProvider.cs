using OrchardCore.ContentManagement;
using System;
using VisusCore.AidStack.OrchardCore.Parts.Indexing;
using VisusCore.Storage.Core.Models;
using VisusCore.Storage.Models;

namespace VisusCore.Storage.Indexing;

public class StreamStorageSizeLimitPartIndexProvider
    : ContentPartIndexProvider<StreamStorageSizeLimitPart, StreamStorageSizeLimitPartIndex>
{
    public StreamStorageSizeLimitPartIndexProvider(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    protected override StreamStorageSizeLimitPartIndex CreateIndex(
        StreamStorageSizeLimitPart part,
        ContentItem contentItem) =>
        new()
        {
            ContentItemId = contentItem.ContentItemId,
            ContentItemVersionId = contentItem.ContentItemVersionId,
            ContentType = contentItem.ContentType,
            Latest = contentItem.Latest,
            Published = contentItem.Published,
            EnableSizeLimit = part.EnableSizeLimit,
            SizeLimitMegabytes = part.SizeLimitMegabytes,
        };
}
