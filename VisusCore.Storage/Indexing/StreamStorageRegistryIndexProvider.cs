using System;
using VisusCore.Storage.Core.Models;
using YesSql.Indexes;

namespace VisusCore.Storage.Indexing;

public class StreamStorageRegistryIndexProvider : IndexProvider<StreamStorageRegistry>
{
    public override void Describe(DescribeContext<StreamStorageRegistry> context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.For<StreamStorageRegistryIndex>()
            .Map(streamStorageRegistry => new StreamStorageRegistryIndex
            {
                StreamId = streamStorageRegistry.StreamId,
            });
    }
}
