using OrchardCore.Modules.Manifest;
using EventBusFeatureIds = VisusCore.EventBus.Constants.FeatureIds;

[assembly: Module(
    Name = "VisusCore Storage",
    Author = "VisusCore",
    Version = "0.0.1",
    Description = "Core storage module.",
    Category = "VisusCore",
    Website = "https://github.com/visuscore/VisusCore.Storage",
    Dependencies = new[]
    {
        EventBusFeatureIds.Module,
    }
)]
