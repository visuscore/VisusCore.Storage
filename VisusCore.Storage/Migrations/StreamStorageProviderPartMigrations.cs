using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using VisusCore.Storage.Models;

namespace VisusCore.Storage.Migrations;

public class StreamStorageProviderPartMigrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public StreamStorageProviderPartMigrations(IContentDefinitionManager contentDefinitionManager) =>
        _contentDefinitionManager = contentDefinitionManager;

    public int Create()
    {
        _contentDefinitionManager.AlterPartDefinition<StreamStorageProviderPart>(definition => definition
            .Configure(definition => definition
                .Attachable()
                .WithDisplayName("Stream storage provider")));

        return 1;
    }
}
