using Lombiq.HelpfulLibraries.OrchardCore.Data;
using OrchardCore.Data.Migration;
using VisusCore.Storage.Core.Models;
using YesSql.Sql;

namespace VisusCore.Storage.Migrations;

public class StreamStorageMigrations : DataMigration
{
    public int Create()
    {
        SchemaBuilder.CreateMapIndexTable<StreamStorageRegistryIndex>(table => table
            .Column(model => model.StreamId, column => column.WithCommonUniqueIdLength()));

        SchemaBuilder.CreateMapIndexTable<StreamStorageInitIndex>(table => table
            .Column(model => model.StreamId, column => column.WithCommonUniqueIdLength())
            .Column(model => model.Provider, column => column.WithLength(1024))
            .Column(model => model.TimestampUtc)
            .Column(model => model.Size)
            .Column(model => model.CreatedUtc));

        var indexColumns = new[]
        {
            nameof(StreamStorageInitIndex.StreamId),
            nameof(StreamStorageInitIndex.Provider),
            nameof(StreamStorageInitIndex.TimestampUtc),
        };

        SchemaBuilder.AlterIndexTable<StreamStorageInitIndex>(table => table
            .CreateIndex(
                $"IDX_{nameof(StreamStorageInitIndex)}_" + string.Join('_', indexColumns),
                indexColumns));

        SchemaBuilder.CreateMapIndexTable<StreamStorageSegmentIndex>(table => table
            .Column(model => model.StreamId, column => column.WithCommonUniqueIdLength())
            .Column(model => model.Provider, column => column.WithLength(1024))
            .Column(model => model.InitId)
            .Column(model => model.TimestampUtc)
            .Column(model => model.Duration)
            .Column(model => model.TimestampProvided, column => column.Nullable())
            .Column(model => model.FrameCount)
            .Column(model => model.Size)
            .Column(model => model.CreatedUtc));

        indexColumns = new[]
        {
            nameof(StreamStorageSegmentIndex.StreamId),
            nameof(StreamStorageSegmentIndex.Provider),
            nameof(StreamStorageSegmentIndex.InitId),
            nameof(StreamStorageSegmentIndex.TimestampUtc),
        };

        SchemaBuilder.AlterIndexTable<StreamStorageSegmentIndex>(table => table
            .CreateIndex(
                $"IDX_{nameof(StreamStorageSegmentIndex)}_" + string.Join('_', indexColumns),
                indexColumns));

        return 1;
    }
}
