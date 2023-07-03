using VisusCore.AidStack.Attributes;

namespace VisusCore.Storage.ViewModels;

public class StreamStorageSizeLimitEditViewModel
{
    public bool EnableSizeLimit { get; set; } = true;
    [RequiredIf<bool>(PropertyName = nameof(EnableSizeLimit), PropertyValue = true)]
    public int SizeLimitMegabytes { get; set; } = 1024;
}
