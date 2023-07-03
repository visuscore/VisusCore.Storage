using Lombiq.HelpfulLibraries.OrchardCore.Data;
using OrchardCore.Data.Migration;
using VisusCore.Storage.Core.Models;
using YesSql.Sql;

namespace VisusCore.Storage.Migrations;

public class StreamStorageSizeLimitPartIndexMigrations : DataMigration
{
    public int Create()
    {
        SchemaBuilder.CreateMapIndexTable<StreamStorageSizeLimitPartIndex>(table => table
            .MapContentPartIndex()
            .Column(model => model.EnableSizeLimit)
            .Column(model => model.SizeLimitMegabytes));

        return 1;
    }
}
