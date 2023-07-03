using VisusCore.AidStack.OrchardCore.Parts.Indexing.Models;

namespace VisusCore.Storage.Core.Models;

public class StreamStorageSizeLimitPartIndex : ContentPartIndex
{
    public bool EnableSizeLimit { get; set; }
    public int SizeLimitMegabytes { get; set; }
}
