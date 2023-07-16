using OrchardCore.Modules.Manifest;
using StorageFeatureIds = VisusCore.Storage.Constants.FeatureIds;

[assembly: Module(
    Name = "VisusCore Disk Storage",
    Author = "VisusCore",
    Version = "0.0.1",
    Description = "Stores stream segments in files.",
    Category = "VisusCore",
    Website = "https://github.com/visuscore/VisusCore.Storage",
    Dependencies = new[]
    {
        StorageFeatureIds.Module,
    }
)]
