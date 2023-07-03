using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using VisusCore.Storage.Models;

namespace VisusCore.Storage.Migrations;

public class StreamStorageModePartMigrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public StreamStorageModePartMigrations(IContentDefinitionManager contentDefinitionManager) =>
        _contentDefinitionManager = contentDefinitionManager;

    public int Create()
    {
        _contentDefinitionManager.AlterPartDefinition<StreamStorageModePart>(definition => definition
            .Configure(definition => definition
                .Attachable()
                .WithDisplayName("Stream storage mode")));

        return 1;
    }
}
