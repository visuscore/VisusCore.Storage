using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.BackgroundTasks;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.Data;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;
using System;
using VisusCore.AidStack.OrchardCore.Extensions;
using VisusCore.EventBus.Core.Extensions;
using VisusCore.Storage.Abstractions.Services;
using VisusCore.Storage.Core.Events;
using VisusCore.Storage.Core.Models;
using VisusCore.Storage.Drivers;
using VisusCore.Storage.Handlers;
using VisusCore.Storage.Hubs;
using VisusCore.Storage.Indexing;
using VisusCore.Storage.Migrations;
using VisusCore.Storage.Models;
using VisusCore.Storage.Services;

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

        services.AddScoped<IContentHandler, StreamStorageConfigurationChangeHandler>();
        services.AddSingleton<StreamStorageConfigurationChangeListener>();
        services.AddReactiveEventConsumer<StreamStoragePublishedEvent>();
        services.AddReactiveEventConsumer<StreamStorageRemovedEvent>();
        services.AddReactiveEventConsumer<StreamStorageUnpublishedEvent>();
        services.AddReactiveEventConsumer<StreamStorageUpdatedEvent>();

        services.AddScoped<IStreamSegmentStorageReader, StreamSegmentStorageReader>();

        services.AddScoped<IBackgroundTask, StreamSegmentStorageMaintainerBackgroundTask>();
        services.AddScoped<StreamSegmentStorageMaintainer>();
    }

    public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider) =>
        app.UseEndpoints(endpoints =>
            endpoints.MapHub<StreamInfoHub>("storage/stream-info"));
}
