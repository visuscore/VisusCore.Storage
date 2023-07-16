using YesSql.Indexes;

namespace VisusCore.Storage.Core.Models;

public abstract class StreamStorageEntityIndex : MapIndex
{
    public string StreamId { get; set; }
}
