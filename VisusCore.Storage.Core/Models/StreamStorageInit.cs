namespace VisusCore.Storage.Core.Models;

public class StreamStorageInit : StreamStorageEntity
{
    public string Provider { get; set; }
    public long TimestampUtc { get; set; }
    public long Size { get; set; }
    public long CreatedUtc { get; set; }
}
