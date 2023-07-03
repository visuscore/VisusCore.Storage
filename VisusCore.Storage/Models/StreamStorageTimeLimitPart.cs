using OrchardCore.ContentManagement;

namespace VisusCore.Storage.Models;

public class StreamStorageTimeLimitPart : ContentPart
{
    public bool EnableTimeLimit { get; set; }
    public int TimeLimitHours { get; set; } = 24;
}
