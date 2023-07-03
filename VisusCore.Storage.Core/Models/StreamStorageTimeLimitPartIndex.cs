using VisusCore.AidStack.OrchardCore.Parts.Indexing.Models;

namespace VisusCore.Storage.Core.Models;

public class StreamStorageTimeLimitPartIndex : ContentPartIndex
{
    public bool EnableTimeLimit { get; set; }
    public int TimeLimitHours { get; set; }
}
