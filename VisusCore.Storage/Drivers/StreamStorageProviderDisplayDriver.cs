using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisusCore.Storage.Abstractions.Services;
using VisusCore.Storage.Models;
using VisusCore.Storage.ViewModels;

namespace VisusCore.Storage.Drivers;

public class StreamStorageProviderDisplayDriver : ContentPartDisplayDriver<StreamStorageProviderPart>
{
    private readonly IEnumerable<IStreamSegmentStorage> _storages;
    private readonly IStringLocalizer T;

    public StreamStorageProviderDisplayDriver(
        IEnumerable<IStreamSegmentStorage> storages,
        IStringLocalizer<StreamStorageProviderDisplayDriver> stringLocalizer)
    {
        _storages = storages;
        T = stringLocalizer;
    }

    public override IDisplayResult Edit(StreamStorageProviderPart part, BuildPartEditorContext context) =>
        Initialize<StreamStorageProviderEditViewModel>(GetEditorShapeType(context), viewModel =>
        {
            viewModel.Provider = part.Provider;
            viewModel.Providers = new[]
            {
                new SelectListItem
                {
                    Text = T["None"],
                    Value = string.Empty,
                    Selected = string.IsNullOrEmpty(part.Provider),
                },
            }
            .Concat(
                _storages.Select(storage =>
                    new SelectListItem
                    {
                        Text = storage.DisplayName,
                        Value = storage.Provider,
                        Selected = part.Provider == storage.Provider,
                    }));
        });

    public override async Task<IDisplayResult> UpdateAsync(
        StreamStorageProviderPart part,
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

        var viewModel = new StreamStorageProviderEditViewModel();

        await context.Updater.TryUpdateModelAsync(viewModel, Prefix);

        part.Provider = viewModel.Provider;

        return await EditAsync(part, context);
    }
}
