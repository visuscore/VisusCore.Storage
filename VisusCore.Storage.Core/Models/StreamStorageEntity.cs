using OrchardCore.Entities;

namespace VisusCore.Storage.Core.Models;

public abstract class StreamStorageEntity : Entity
{
    public long Id { get; set; }
    public string StreamId { get; set; }
}
