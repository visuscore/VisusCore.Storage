using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using System.Threading.Tasks;
using VisusCore.Storage.Models;
using VisusCore.Storage.ViewModels;

namespace VisusCore.Storage.Drivers;

public class StreamStorageModeDisplayDriver : ContentPartDisplayDriver<StreamStorageModePart>
{
    public override IDisplayResult Edit(StreamStorageModePart part, BuildPartEditorContext context) =>
        Initialize<StreamStorageModeEditViewModel>(GetEditorShapeType(context), viewModel =>
            viewModel.StorageMode = part.StorageMode);

    public override async Task<IDisplayResult> UpdateAsync(
        StreamStorageModePart part,
        IUpdateModel updater,
        UpdatePartEditorContext context)
    {
        var viewModel = new StreamStorageModeEditViewModel();

        await context.Updater.TryUpdateModelAsync(viewModel, Prefix);

        part.StorageMode = viewModel.StorageMode;

        return await EditAsync(part, context);
    }
}
