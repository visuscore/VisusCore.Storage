using Lombiq.HelpfulLibraries.OrchardCore.Data;
using OrchardCore.Data.Migration;
using VisusCore.Storage.Core.Models;
using YesSql.Sql;

namespace VisusCore.Storage.Migrations;

public class StreamStorageTimeLimitPartIndexMigrations : DataMigration
{
    public int Create()
    {
        SchemaBuilder.CreateMapIndexTable<StreamStorageTimeLimitPartIndex>(table => table
            .MapContentPartIndex()
            .Column(model => model.EnableTimeLimit)
            .Column(model => model.TimeLimitHours));

        return 1;
    }
}
