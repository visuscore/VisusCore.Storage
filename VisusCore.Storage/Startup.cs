using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using VisusCore.AidStack.OrchardCore.Extensions;
using VisusCore.Storage.Core.Models;
using VisusCore.Storage.Drivers;
using VisusCore.Storage.Indexing;
using VisusCore.Storage.Migrations;
using VisusCore.Storage.Models;

namespace VisusCore.Storage;

public class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddDataMigration<StreamStorageModePartMigrations>();
        services.AddDataMigration<StreamStorageModePartIndexMigrations>();
        services.AddScopedContentPartIndexProvider<
            StreamStorageModePartIndexProvider,
            StreamStorageModePart,
            StreamStorageModePartIndex>();
        services.AddContentPart<StreamStorageModePart>()
            .UseDisplayDriver<StreamStorageModeDisplayDriver>();

        services.AddDataMigration<StreamStorageSizeLimitPartMigrations>();
        services.AddDataMigration<StreamStorageSizeLimitPartIndexMigrations>();
        services.AddScopedContentPartIndexProvider<
            StreamStorageSizeLimitPartIndexProvider,
            StreamStorageSizeLimitPart,
            StreamStorageSizeLimitPartIndex>();
        services.AddContentPart<StreamStorageSizeLimitPart>()
            .UseDisplayDriver<StreamStorageSizeLimitDisplayDriver>();

        services.AddDataMigration<StreamStorageTimeLimitPartMigrations>();
        services.AddDataMigration<StreamStorageTimeLimitPartIndexMigrations>();
        services.AddScopedContentPartIndexProvider<
            StreamStorageTimeLimitPartIndexProvider,
            StreamStorageTimeLimitPart,
            StreamStorageTimeLimitPartIndex>();
        services.AddContentPart<StreamStorageTimeLimitPart>()
            .UseDisplayDriver<StreamStorageTimeLimitDisplayDriver>();

        services.AddDataMigration<StreamStorageProviderPartMigrations>();
        services.AddDataMigration<StreamStorageProviderPartIndexMigrations>();
        services.AddScopedContentPartIndexProvider<
            StreamStorageProviderPartIndexProvider,
            StreamStorageProviderPart,
            StreamStorageProviderPartIndex>();
        services.AddContentPart<StreamStorageProviderPart>()
            .UseDisplayDriver<StreamStorageProviderDisplayDriver>();

        services.AddDataMigration<StreamStorageMigrations>();
        services.AddIndexProvider<StreamStorageRegistryIndexProvider>();
        services.AddIndexProvider<StreamStorageInitIndexProvider>();
        services.AddIndexProvider<StreamStorageSegmentIndexProvider>();
    }
}
