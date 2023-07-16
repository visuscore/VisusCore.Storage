using OrchardCore.Modules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.Consumer.Abstractions.Models;
using VisusCore.Storage.Abstractions.Services;
using VisusCore.Storage.Core.Models;
using YesSql;

namespace VisusCore.Storage.Services;

public abstract class StreamSegmentStorageBase : IStreamSegmentStorage
{
    private readonly IStore _store;
    private readonly ConcurrentDictionary<string, StreamStorageConsumerContext> _contexts = new();

    public abstract string DisplayName { get; }
    public abstract string Provider { get; }
    protected IClock Clock { get; }

    protected StreamSegmentStorageBase(IStore store, IClock clock)
    {
        _store = store;
        Clock = clock;
    }

    public async Task ConsumeAsync(IVideoStreamSegment segment, CancellationToken cancellationToken = default)
    {
        var consumerContext = await _contexts.GetValueOrAddIfMissingAsync(
            segment.StreamId,
            (streamId) => CreateConsumerContextAsync(streamId, segment.Init, cancellationToken));
        if (consumerContext is null || !consumerContext.Consuming)
        {
            return;
        }

        using var session = _store.CreateSession();
        var storageContext = CreateStorageContext(consumerContext, session);

        if (consumerContext.LatestInit is null || !consumerContext.LatestInit.Init.SequenceEqual(segment.Init.Init))
        {
            consumerContext.LatestInit = await SaveInitAsync(storageContext, segment.Init, cancellationToken);
        }

        await SaveSegmentAsync(storageContext, segment, cancellationToken);

        await session.SaveChangesAsync();
    }

    protected abstract Task<IVideoStreamInit> GetLatestInitAsync(
        IStreamSegmentStorageContext context,
        CancellationToken cancellationToken = default);
    protected abstract Task<IVideoStreamInit> SaveInitAsync(
        IStreamSegmentStorageContext context,
        IVideoStreamInit init,
        CancellationToken cancellationToken = default);
    protected abstract Task SaveSegmentAsync(
        IStreamSegmentStorageContext context,
        IVideoStreamSegment segment,
        CancellationToken cancellationToken = default);

    private async Task<IVideoStreamInit> GetLatestOrSaveInitAsync(
        IStreamSegmentStorageContext context,
        IVideoStreamInit init,
        CancellationToken cancellationToken = default)
    {
        var latestInit = await GetLatestInitAsync(context, cancellationToken);
        if (latestInit == null || !latestInit.Init.SequenceEqual(init.Init))
        {
            return await SaveInitAsync(context, init, cancellationToken);
        }

        return latestInit;
    }

    private async Task<StreamStorageConsumerContext> CreateConsumerContextAsync(
        string streamId,
        IVideoStreamInit init,
        CancellationToken cancellationToken = default)
    {
        using var session = _store.CreateSession();
        var storageMode = await session.QueryIndex<StreamStorageModePartIndex>()
            .Where(index =>
                index.Latest
                && index.Published
                && index.ContentItemId == streamId)
            .FirstOrDefaultAsync();
        var storageProvider = await session.QueryIndex<StreamStorageProviderPartIndex>()
            .Where(index =>
                index.Latest
                && index.Published
                && index.ContentItemId == streamId)
            .FirstOrDefaultAsync();
        var consuming = storageMode?.Mode is EStorageMode.Store
                && GetType().FullName.EqualsOrdinalIgnoreCase(storageProvider?.Provider);
        var consumerContext = new StreamStorageConsumerContext
        {
            StreamId = streamId,
            Consuming = consuming,
        };
        consumerContext.LatestInit = !consuming
                ? null
                : await GetLatestOrSaveInitAsync(CreateStorageContext(consumerContext, session), init, cancellationToken);

        await session.SaveChangesAsync();

        return consumerContext;
    }

    private static StreamSegmentStorageContext CreateStorageContext(
        StreamStorageConsumerContext consumerContext,
        ISession session) =>
        new()
        {
            StreamId = consumerContext.StreamId,
            LatestInit = consumerContext.LatestInit,
            Session = session,
        };
}

internal sealed class StreamStorageConsumerContext
{
    public string StreamId { get; set; }
    public bool Consuming { get; set; }
    public IVideoStreamInit LatestInit { get; set; }
}

internal sealed class StreamSegmentStorageContext : IStreamSegmentStorageContext
{
    public string StreamId { get; set; }
    public ISession Session { get; set; }
    public IVideoStreamInit LatestInit { get; set; }
}
