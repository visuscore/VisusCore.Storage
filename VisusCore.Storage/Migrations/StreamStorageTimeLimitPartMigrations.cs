using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using VisusCore.Storage.Models;

namespace VisusCore.Storage.Migrations;

public class StreamStorageTimeLimitPartMigrations : DataMigration
{
    private readonly IContentDefinitionManager _contentDefinitionManager;

    public StreamStorageTimeLimitPartMigrations(IContentDefinitionManager contentDefinitionManager) =>
        _contentDefinitionManager = contentDefinitionManager;

    public int Create()
    {
        _contentDefinitionManager.AlterPartDefinition<StreamStorageTimeLimitPart>(definition => definition
            .Configure(definition => definition
                .Attachable()
                .WithDisplayName("Stream storage size limit")));

        return 1;
    }
}
