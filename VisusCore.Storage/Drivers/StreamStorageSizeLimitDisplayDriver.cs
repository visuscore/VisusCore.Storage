using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using System;
using System.Threading.Tasks;
using VisusCore.Storage.Models;
using VisusCore.Storage.ViewModels;

namespace VisusCore.Storage.Drivers;

public class StreamStorageSizeLimitDisplayDriver : ContentPartDisplayDriver<StreamStorageSizeLimitPart>
{
    public override IDisplayResult Edit(StreamStorageSizeLimitPart part, BuildPartEditorContext context) =>
        Initialize<StreamStorageSizeLimitEditViewModel>(GetEditorShapeType(context), viewModel =>
        {
            viewModel.EnableSizeLimit = part.EnableSizeLimit;
            viewModel.SizeLimitMegabytes = part.SizeLimitMegabytes;
        });

    public override async Task<IDisplayResult> UpdateAsync(
        StreamStorageSizeLimitPart part,
        IUpdateModel updater,
        UpdatePartEditorContext context)
    {
        if (part is null)
        {
            throw new ArgumentNullException(nameof(part));
        }

        if (updater is null)
        {
            throw new ArgumentNullException(nameof(updater));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var viewModel = new StreamStorageSizeLimitEditViewModel();

        await context.Updater.TryUpdateModelAsync(viewModel, Prefix);

        part.EnableSizeLimit = viewModel.EnableSizeLimit;
        part.SizeLimitMegabytes = viewModel.SizeLimitMegabytes;

        return await EditAsync(part, context);
    }
}
