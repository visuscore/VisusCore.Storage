using System;
using VisusCore.Storage.Core.Models;
using YesSql.Indexes;

namespace VisusCore.Storage.Indexing;

public class StreamStorageInitIndexProvider : IndexProvider<StreamStorageInit>
{
    public override void Describe(DescribeContext<StreamStorageInit> context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.For<StreamStorageInitIndex>()
            .Map(streamStorageInit => new StreamStorageInitIndex
            {
                StreamId = streamStorageInit.StreamId,
                Provider = streamStorageInit.Provider,
                TimestampUtc = streamStorageInit.TimestampUtc,
                Size = streamStorageInit.Size,
                CreatedUtc = streamStorageInit.CreatedUtc,
            });
    }
}
