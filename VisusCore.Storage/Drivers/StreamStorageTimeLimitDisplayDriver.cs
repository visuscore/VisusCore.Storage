using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using System.Threading.Tasks;
using VisusCore.Storage.Models;
using VisusCore.Storage.ViewModels;

namespace VisusCore.Storage.Drivers;

public class StreamStorageTimeLimitDisplayDriver : ContentPartDisplayDriver<StreamStorageTimeLimitPart>
{
    public override IDisplayResult Edit(StreamStorageTimeLimitPart part, BuildPartEditorContext context) =>
        Initialize<StreamStorageTimeLimitEditViewModel>(GetEditorShapeType(context), viewModel =>
        {
            viewModel.EnableTimeLimit = part.EnableTimeLimit;
            viewModel.TimeLimitHours = part.TimeLimitHours;
        });

    public override async Task<IDisplayResult> UpdateAsync(
        StreamStorageTimeLimitPart part,
        IUpdateModel updater,
        UpdatePartEditorContext context)
    {
        var viewModel = new StreamStorageTimeLimitEditViewModel();

        await context.Updater.TryUpdateModelAsync(viewModel, Prefix);

        part.EnableTimeLimit = viewModel.EnableTimeLimit;
        part.TimeLimitHours = viewModel.TimeLimitHours;

        return await EditAsync(part, context);
    }
}
