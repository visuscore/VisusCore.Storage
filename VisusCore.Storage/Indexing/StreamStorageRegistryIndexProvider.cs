using VisusCore.Storage.Core.Models;
using YesSql.Indexes;

namespace VisusCore.Storage.Indexing;

public class StreamStorageRegistryIndexProvider : IndexProvider<StreamStorageRegistry>
{
    public override void Describe(DescribeContext<StreamStorageRegistry> context) =>
        context.For<StreamStorageRegistryIndex>()
            .Map(streamStorageRegistry => new StreamStorageRegistryIndex
            {
                StreamId = streamStorageRegistry.StreamId,
            });
}
