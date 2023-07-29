using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;
using OrchardCore.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
    private readonly BlobDatabaseCacheAccessor _blobDatabaseCache;
    private readonly ISiteService _siteService;
    private readonly IStringLocalizer T;
    private string _siteName;

    public override string DisplayName => T["Disk"];
    public override string Provider => typeof(StreamSegmentDiskStorage).FullName;

    public StreamSegmentDiskStorage(
        IStore store,
        IClock clock,
        StreamStorageConfigurationChangeListener configurationChangeListener,
        IOptions<DiskStorageOptions> options,
        BlobDatabaseCacheAccessor blobDatabaseCache,
        ISiteService siteService,
        IStringLocalizer<StreamSegmentDiskStorage> stringLocalizer,
        ILoggerFactory loggerFactory)
        : base(store, clock, configurationChangeListener)
    {
        _options = options;
        _blobDatabaseCache = blobDatabaseCache;
        _siteService = siteService;
        T = stringLocalizer;
    }

    public override async Task<IEnumerable<IVideoStreamSegment>> GetSegmentsByKeyAsync<TStreamSegmentKey>(
        IEnumerable<TStreamSegmentKey> keys,
        Func<TStreamSegmentKey, IVideoStreamInit, byte[], Task<IVideoStreamSegment>> converterAsync,
        CancellationToken cancellationToken = default)
    {
        using var session = CreateSession();
        var initCache = new ConcurrentDictionary<string, VideoStreamInit>();
        return await keys.GroupBy(key => key.StreamId)
            .ToAsyncEnumerable()
            .SelectManyAwait(async streamGroup =>
            {
                var directory = await GetCreateStreamRootDirectoryAsync(streamGroup.Key);
                var initDbPath = Path.Combine(directory.FullName, InitDBFileName);
                return (await streamGroup.ToAsyncEnumerable()
                    .SelectAwait(async key =>
                    {
                        var init = await initCache.GetValueOrAddIfMissingAsync(
                            GetKeyForInitRecord(streamGroup.Key, key.InitId),
                            initKey => _blobDatabaseCache.InvokeOnReadLockAsync(
                                initDbPath,
                                initDb =>
                                {
                                    var initEntry = initDb.ListFiles().Find(entry => entry.FileName == initKey);
                                    using var initStream = new MemoryStream();
                                    initDb.Read(initEntry.ID, initStream);
                                    initStream.Seek(0, SeekOrigin.Begin);

                                    return Task.FromResult(new VideoStreamInit
                                    {
                                        Id = key.InitId,
                                        StreamId = streamGroup.Key,
                                        Init = new ReadOnlySpan<byte>(initStream.ToArray()),
                                    });
                                },
                                cancellationToken));

                        var dbPath = Path.Combine(directory.FullName, GetDatabaseNameForSegment(key.TimestampUtc));

                        return await _blobDatabaseCache.InvokeOnReadLockAsync(
                            dbPath,
                            segmentDb =>
                            {
                                var segmentKey = GetKeyForSegmentRecord(streamGroup.Key, key.DocumentId);
                                var segmentEntry = segmentDb.ListFiles().Find(entry => entry.FileName == segmentKey);
                                using var segmentStream = new MemoryStream();

                                segmentDb.Read(segmentEntry.ID, segmentStream);
                                segmentStream.Seek(0, SeekOrigin.Begin);

                                return converterAsync.Invoke(key, init, segmentStream.ToArray());
                            },
                            cancellationToken);
                    })
                    .ToListAsync(cancellationToken))
                    .ToAsyncEnumerable();
            })
            .ToListAsync(cancellationToken);
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

        return await _blobDatabaseCache.InvokeOnReadLockAsync(
            dbPath,
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
        await _blobDatabaseCache.InvokeOnReadWriteLockAsync(
            dbPath,
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
        await _blobDatabaseCache.InvokeOnReadWriteLockAsync(
            dbPath,
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

    private static string GetDatabaseNameForSegment(IVideoStreamSegment segment) =>
        GetDatabaseNameForSegment(segment.Metadata.TimestampUtc);

    private static string GetDatabaseNameForSegment(long timestampUtc)
    {
        var timeStamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampUtc / 1000);

        return $"{timeStamp.ToString("yyyyMMddH", CultureInfo.InvariantCulture)}.db";
    }

    private static string GetKeyForSegmentRecord(StreamStorageSegment segmentRecord) =>
        GetKeyForSegmentRecord(segmentRecord.StreamId, segmentRecord.Id);

    private static string GetKeyForSegmentRecord(string streamId, long documentId) =>
        GetKeyForRecord(streamId, documentId, "dat");

    private static string GetKeyForInitRecord(StreamStorageInit initRecord) =>
        GetKeyForRecord(initRecord.StreamId, initRecord.Id, "dat");

    private static string GetKeyForInitRecord(StreamStorageInitIndex initRecord) =>
        GetKeyForRecord(initRecord.StreamId, initRecord.DocumentId, "dat");

    private static string GetKeyForInitRecord(string streamId, long documentId) =>
        GetKeyForRecord(streamId, documentId, "dat");

    private static string GetKeyForRecord(string streamId, long documentId, string extension) =>
        $"{streamId}-{documentId.ToString(CultureInfo.InvariantCulture)}.{extension}";
}

internal sealed class VideoStreamInit : IVideoStreamInit
{
    private readonly byte[] _init;

    public long Id { get; set; }
    public string StreamId { get; init; }
    public ReadOnlySpan<byte> Init { get => _init; init => _init = value.ToArray(); }
}
