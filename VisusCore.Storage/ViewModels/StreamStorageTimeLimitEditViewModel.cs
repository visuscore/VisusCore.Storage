using VisusCore.AidStack.Attributes;

namespace VisusCore.Storage.ViewModels;

public class StreamStorageTimeLimitEditViewModel
{
    public bool EnableTimeLimit { get; set; }
    [RequiredIf<bool>(PropertyName = nameof(EnableTimeLimit), PropertyValue = true)]
    public int TimeLimitHours { get; set; } = 24;
}
