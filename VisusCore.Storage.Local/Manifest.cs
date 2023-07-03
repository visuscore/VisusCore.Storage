using OrchardCore.Modules.Manifest;
using StorageFeatureIds = VisusCore.Storage.Constants.FeatureIds;

[assembly: Module(
    Name = "VisusCore Local File Storage",
    Author = "VisusCore",
    Version = "0.0.1",
    Description = "Stores stream segments in local files.",
    Category = "VisusCore",
    Website = "https://github.com/visuscore/VisusCore.Storage",
    Dependencies = new[]
    {
        StorageFeatureIds.Module,
    }
)]
