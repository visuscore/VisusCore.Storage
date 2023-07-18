using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;
using OrchardCore.Settings;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.Consumer.Abstractions.Models;
using VisusCore.Storage.Core.Models;
using VisusCore.Storage.Disk.Models;
using VisusCore.Storage.Services;
using YesSql;

namespace VisusCore.Storage.Disk.Services;

public class StreamSegmentDiskStorage : StreamSegmentStorageBase
{
    private const string InitDBFileName = "init.db";
    private readonly IOptions<DiskStorageOptions> _options;
    private readonly ISiteService _siteService;
    private readonly IStringLocalizer T;
    private readonly MemoryCache _blobDatabaseCache;
    private string _siteName;

    public override string DisplayName => T["Disk"];
    public override string Provider => typeof(StreamSegmentDiskStorage).FullName;

    public StreamSegmentDiskStorage(
        IStore store,
        IClock clock,
        StreamStorageConfigurationChangeListener configurationChangeListener,
        IOptions<DiskStorageOptions> options,
        ISiteService siteService,
        IStringLocalizer<StreamSegmentDiskStorage> stringLocalizer,
        ILoggerFactory loggerFactory)
        : base(store, clock, configurationChangeListener)
    {
        _options = options;
        _siteService = siteService;
        T = stringLocalizer;
        _blobDatabaseCache = new MemoryCache(new MemoryCacheOptions(), loggerFactory);
    }

    protected override async Task<IVideoStreamInit> GetLatestInitAsync(
        IStreamSegmentStorageContext context,
        CancellationToken cancellationToken = default)
    {
        var initRecord = await context.Session.QueryIndex<StreamStorageInitIndex>()
            .Where(index =>
                index.StreamId == context.StreamId
                && index.Provider == Provider)
            .OrderByDescending(index => index.TimestampUtc)
            .Take(1)
            .FirstOrDefaultAsync();
        if (initRecord is null)
        {
            return default;
        }

        var directory = await GetCreateStreamRootDirectoryAsync(context.StreamId);
        var dbPath = Path.Combine(directory.FullName, InitDBFileName);
        var blobCacheEntry = _blobDatabaseCache.GetOrCreate(dbPath, CreateBlobDatabaseCacheEntry(dbPath));

        return await blobCacheEntry.InvokeOnLockAsync(
            initDb =>
            {
                var initEntry = initDb.ListFiles().Find(entry => entry.FileName == GetKeyForInitRecord(initRecord));
                using var initStream = new MemoryStream();

                initDb.Read(initEntry.ID, initStream);
                initStream.Seek(0, SeekOrigin.Begin);

                return Task.FromResult(new VideoStreamInit
                {
                    Id = initRecord.Id,
                    StreamId = initRecord.StreamId,
                    Init = new ReadOnlySpan<byte>(initStream.ToArray()),
                });
            },
            cancellationToken);
    }

    protected override async Task<IVideoStreamInit> SaveInitAsync(
        IStreamSegmentStorageContext context,
        IVideoStreamInit init,
        CancellationToken cancellationToken = default)
    {
        var directory = await GetCreateStreamRootDirectoryAsync(context.StreamId);
        var initRecord = new StreamStorageInit
        {
            StreamId = context.StreamId,
            Provider = Provider,
            TimestampUtc = Clock.GetUnixTimeMilliseconds() * 1000,
            Size = init.Init.Length,
            CreatedUtc = Clock.GetUnixTimeMilliseconds() * 1000,
        };

        context.Session.Save(initRecord);

        var dbPath = Path.Combine(directory.FullName, InitDBFileName);
        var blobCacheEntry = _blobDatabaseCache.GetOrCreate(dbPath, CreateBlobDatabaseCacheEntry(dbPath));
        await blobCacheEntry.InvokeOnLockAsync(
            initDb =>
            {
                using var initStream = new MemoryStream(init.Init.ToArray());
                initDb.Store(GetKeyForInitRecord(initRecord), initStream);

                return Task.CompletedTask;
            },
            cancellationToken);

        return new VideoStreamInit
        {
            Id = initRecord.Id,
            StreamId = initRecord.StreamId,
            Init = init.Init,
        };
    }

    protected override async Task SaveSegmentAsync(
        IStreamSegmentStorageContext context,
        IVideoStreamSegment segment,
        CancellationToken cancellationToken = default)
    {
        if (context.LatestInit is not VideoStreamInit latestInit)
        {
            throw new InvalidOperationException("Stream init is not available.");
        }

        var directory = await GetCreateStreamRootDirectoryAsync(context.StreamId);
        var segmentRecord = new StreamStorageSegment
        {
            Provider = Provider,
            StreamId = context.StreamId,
            InitId = latestInit.Id,
            TimestampUtc = segment.Metadata.TimestampUtc,
            Duration = segment.Metadata.Duration,
            TimestampProvided = segment.Metadata.TimestampProvided,
            FrameCount = segment.Metadata.FrameCount,
            Size = segment.Data.Length,
            CreatedUtc = Clock.GetUnixTimeMilliseconds() * 1000,
        };

        context.Session.Save(segmentRecord);

        var dbPath = Path.Combine(directory.FullName, GetDatabaseNameForSegment(segment));
        var blobCacheEntry = _blobDatabaseCache.GetOrCreate(dbPath, CreateBlobDatabaseCacheEntry(dbPath));
        await blobCacheEntry.InvokeOnLockAsync(
            segmentDb =>
            {
                using var initStream = new MemoryStream(segment.Data.ToArray());
                segmentDb.Store(GetKeyForSegmentRecord(segmentRecord), initStream);

                return Task.CompletedTask;
            },
            cancellationToken);
    }

    private async Task<DirectoryInfo> GetCreateStreamRootDirectoryAsync(string streamId)
    {
        var directory = new DirectoryInfo(await GetStreamRootPathAsync(streamId));
        if (!directory.Exists)
        {
            directory.Create();
        }

        return directory;
    }

    private async Task<string> GetStreamRootPathAsync(string streamId)
    {
        if (string.IsNullOrEmpty(_options.Value.RootPath))
        {
            throw new InvalidOperationException("Disk storage root path is not configured.");
        }

        _siteName ??= (await _siteService.GetSiteSettingsAsync()).SiteName;

        return Path.Combine(_options.Value.RootPath, _siteName, "streams", streamId);
    }

    private static Func<ICacheEntry, BlobDatabaseCacheEntry> CreateBlobDatabaseCacheEntry(string path) =>
        entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromSeconds(10))
                .RegisterPostEvictionCallback((_, value, _, _) =>
                {
                    if (value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                });

            return new BlobDatabaseCacheEntry(path);
        };

    private static string GetDatabaseNameForSegment(IVideoStreamSegment segment)
    {
        var timeStamp = DateTimeOffset.FromUnixTimeMilliseconds(segment.Metadata.TimestampUtc / 1000);

        return $"{timeStamp.ToString("yyyyMMddH", CultureInfo.InvariantCulture)}.db";
    }

    private static string GetKeyForSegmentRecord(StreamStorageSegment segmentRecord) =>
        GetKeyForRecord(segmentRecord.StreamId, segmentRecord.Id, "dat");

    private static string GetKeyForInitRecord(StreamStorageInit initRecord) =>
        GetKeyForRecord(initRecord.StreamId, initRecord.Id, "dat");

    private static string GetKeyForInitRecord(StreamStorageInitIndex initRecord) =>
        GetKeyForRecord(initRecord.StreamId, initRecord.DocumentId, "dat");

    private static string GetKeyForRecord(string streamId, long documentId, string extension) =>
        $"{streamId}-{documentId.ToString(CultureInfo.InvariantCulture)}.{extension}";

    protected override void Dispose(bool disposing)
    {
        _blobDatabaseCache.Dispose();

        base.Dispose(disposing);
    }
}

internal sealed class VideoStreamInit : IVideoStreamInit
{
    private readonly byte[] _init;

    public long Id { get; set; }
    public string StreamId { get; init; }
    public ReadOnlySpan<byte> Init { get => _init; init => _init = value.ToArray(); }
}
