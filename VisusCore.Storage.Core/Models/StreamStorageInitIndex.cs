namespace VisusCore.Storage.Core.Models;

public class StreamStorageInitIndex : StreamStorageEntityIndex
{
    public long DocumentId { get; set; }
    public string Provider { get; set; }
    public long TimestampUtc { get; set; }
    public long Size { get; set; }
    public long CreatedUtc { get; set; }
}
