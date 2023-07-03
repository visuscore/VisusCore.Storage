using Lombiq.HelpfulLibraries.OrchardCore.Data;
using OrchardCore.Data.Migration;
using VisusCore.Storage.Core.Models;
using YesSql.Sql;

namespace VisusCore.Storage.Migrations;

public class StreamStorageModePartIndexMigrations : DataMigration
{
    public int Create()
    {
        SchemaBuilder.CreateMapIndexTable<StreamStorageModePartIndex>(table => table
            .MapContentPartIndex()
            .Column(model => model.StorageMode));

        return 1;
    }
}
