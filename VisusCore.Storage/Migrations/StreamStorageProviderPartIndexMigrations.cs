using Lombiq.HelpfulLibraries.OrchardCore.Data;
using OrchardCore.Data.Migration;
using VisusCore.Storage.Core.Models;
using YesSql.Sql;

namespace VisusCore.Storage.Migrations;

public class StreamStorageProviderPartIndexMigrations : DataMigration
{
    public int Create()
    {
        SchemaBuilder.CreateMapIndexTable<StreamStorageProviderPartIndex>(table => table
            .MapContentPartIndex()
            .Column(model => model.Provider, column => column.Nullable().WithLength(1024)));

        return 1;
    }
}
