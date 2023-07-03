using OrchardCore.ContentManagement;
using VisusCore.Storage.Core.Models;

namespace VisusCore.Storage.Models;

public class StreamStorageModePart : ContentPart
{
    public EStorageMode StorageMode { get; set; } = EStorageMode.None;
}
