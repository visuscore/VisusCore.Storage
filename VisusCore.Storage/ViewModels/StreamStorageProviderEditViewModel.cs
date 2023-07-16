using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace VisusCore.Storage.ViewModels;

public class StreamStorageProviderEditViewModel
{
    public string Provider { get; set; }
    public IEnumerable<SelectListItem> Providers { get; set; }
}
