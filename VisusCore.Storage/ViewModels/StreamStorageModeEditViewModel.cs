using System.ComponentModel.DataAnnotations;
using VisusCore.Storage.Core.Models;

namespace VisusCore.Storage.ViewModels;

public class StreamStorageModeEditViewModel
{
    [Required]
    public EStorageMode StorageMode { get; set; } = EStorageMode.None;
}
