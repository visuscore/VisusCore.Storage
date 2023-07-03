using OrchardCore.ContentManagement;

namespace VisusCore.Storage.Models;

public class StreamStorageSizeLimitPart : ContentPart
{
    public bool EnableSizeLimit { get; set; } = true;
    public int SizeLimitMegabytes { get; set; } = 1024;
}
