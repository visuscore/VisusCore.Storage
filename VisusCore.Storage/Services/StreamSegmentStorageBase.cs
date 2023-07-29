using OrchardCore.Modules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.Consumer.Abstractions.Models;
using VisusCore.Storage.Abstractions.Models;
using VisusCore.Storage.Abstractions.Services;
using VisusCore.Storage.Core.Events;
using VisusCore.Storage.Core.Models;
using YesSql;

namespace VisusCore.Storage.Services;

public abstract class StreamSegmentStorageBase : IStreamSegmentStorage, IDisposable, IAsyncDisposable
{
    private readonly IStore _store;
    private readonly StreamStorageConfigurationChangeListener _configurationChangeListener;
    private readonly ConcurrentDictionary<string, StreamStorageConsumerContext> _contexts = new();
    private readonly SemaphoreSlim _configurationChangeSubscriptionsLock = new(1, 1);
    private IAsyncDisposable _streamStoragePublishedSubscription;
    private IAsyncDisposable _streamStorageRemovedSubscription;
    private IAsyncDisposable _streamStorageUnpublishedSubscription;
    private IAsyncDisposable _streamStorageUpdatedSubscription;
    private bool _disposed;
    private bool _disposedAsync;

    public abstract string DisplayName { get; }
    public abstract string Provider { get; }
    protected IClock Clock { get; }

    protected StreamSegmentStorageBase(
        IStore store,
        IClock clock,
        StreamStorageConfigurationChangeListener configurationChangeListener)
    {
        _store = store;
        Clock = clock;
        _configurationChangeListener = configurationChangeListener;
    }

    private async Task EnsureConfigurationChangeSubscriptionsAsync()
    {
        await _configurationChangeSubscriptionsLock.WaitAsync();

        try
        {
            if (_streamStoragePublishedSubscription is null)
            {
                _streamStoragePublishedSubscription = await _configurationChangeListener.StreamStoragePublished
                    .SubscribeAsync(async deviceEvent => await UpdateConsumerContextAsync(deviceEvent));
                _streamStorageRemovedSubscription = await _configurationChangeListener.StreamStorageRemoved
                    .SubscribeAsync(async deviceEvent => await UpdateConsumerContextAsync(deviceEvent));
                _streamStorageUnpublishedSubscription = await _configurationChangeListener.StreamStorageUnpublished
                    .SubscribeAsync(async deviceEvent => await UpdateConsumerContextAsync(deviceEvent));
                _streamStorageUpdatedSubscription = await _configurationChangeListener.StreamStorageUpdated
                    .SubscribeAsync(async deviceEvent => await UpdateConsumerContextAsync(deviceEvent));
            }
        }
        finally
        {
            _configurationChangeSubscriptionsLock.Release();
        }
    }

    public async Task ConsumeAsync(IVideoStreamSegment segment, CancellationToken cancellationToken = default)
    {
        await EnsureConfigurationChangeSubscriptionsAsync();

        var consumerContext = await _contexts.GetValueOrAddIfMissingAsync(
            segment.StreamId,
            (streamId) => CreateConsumerContextAsync(streamId, segment.Init, cancellationToken));
        if (consumerContext is null || !consumerContext.Consuming)
        {
            return;
        }

        using var session = CreateSession();
        var storageContext = CreateStorageContext(consumerContext, session);

        await consumerContext.ConsumeLock.WaitAsync(cancellationToken);
        try
        {
            if (consumerContext.LatestInit is null || !consumerContext.LatestInit.Init.SequenceEqual(segment.Init.Init))
            {
                consumerContext.LatestInit = await SaveInitAsync(storageContext, segment.Init, cancellationToken);
            }

            await SaveSegmentAsync(storageContext, segment, cancellationToken);
        }
        finally
        {
            consumerContext.ConsumeLock.Release();
        }

        await session.SaveChangesAsync();
    }

    protected ISession CreateSession() => _store.CreateSession();

    public abstract Task<IEnumerable<IVideoStreamSegment>> GetSegmentsByKeyAsync<TStreamSegmentKey>(
        IEnumerable<TStreamSegmentKey> keys,
        Func<TStreamSegmentKey, IVideoStreamInit, byte[], Task<IVideoStreamSegment>> converterAsync,
        CancellationToken cancellationToken = default)
        where TStreamSegmentKey : IStreamSegmentKey;

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
        using var session = CreateSession();
        var consumerContext = await UpdateConsumerContextAsync(session, new() { StreamId = streamId });

        consumerContext.LatestInit = !consumerContext.Consuming
                ? null
                : await GetLatestOrSaveInitAsync(CreateStorageContext(consumerContext, session), init, cancellationToken);

        await session.SaveChangesAsync();

        return consumerContext;
    }

    private async Task UpdateConsumerContextAsync(StreamStorageEvent storageEvent)
    {
        if (!_contexts.TryGetValue(storageEvent.StreamId, out var consumerContext))
        {
            return;
        }

        if (storageEvent is StreamStorageRemovedEvent)
        {
            _contexts.Remove(storageEvent.StreamId, out _);

            return;
        }

        using var session = CreateSession();

        await consumerContext.ConsumeLock.WaitAsync();
        try
        {
            await UpdateConsumerContextAsync(session, consumerContext);
            consumerContext.LatestInit = !consumerContext.Consuming
                    ? null
                    : await GetLatestInitAsync(CreateStorageContext(consumerContext, session));
        }
        finally
        {
            consumerContext.ConsumeLock.Release();
        }
    }

    private async Task<StreamStorageConsumerContext> UpdateConsumerContextAsync(
        ISession session,
        StreamStorageConsumerContext consumerContext)
    {
        var storageMode = await session.QueryIndex<StreamStorageModePartIndex>()
            .Where(index =>
                index.Latest
                && index.Published
                && index.ContentItemId == consumerContext.StreamId)
            .FirstOrDefaultAsync();
        var storageProvider = await session.QueryIndex<StreamStorageProviderPartIndex>()
            .Where(index =>
                index.Latest
                && index.Published
                && index.ContentItemId == consumerContext.StreamId)
            .FirstOrDefaultAsync();
        var consuming = storageMode?.Mode is EStorageMode.Store
                && GetType().FullName.EqualsOrdinalIgnoreCase(storageProvider?.Provider);

        consumerContext.Consuming = consuming;

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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DisposeAsync()
                    .AsTask()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                _configurationChangeSubscriptionsLock.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposedAsync || _disposed)
        {
            return;
        }

        if (_streamStoragePublishedSubscription is not null)
        {
            await _streamStoragePublishedSubscription.DisposeAsync();
        }

        if (_streamStorageRemovedSubscription is not null)
        {
            await _streamStorageRemovedSubscription.DisposeAsync();
        }

        if (_streamStorageUnpublishedSubscription is not null)
        {
            await _streamStorageUnpublishedSubscription.DisposeAsync();
        }

        if (_streamStorageUpdatedSubscription is not null)
        {
            await _streamStorageUpdatedSubscription.DisposeAsync();
        }

        Dispose(disposing: false);
        _disposedAsync = true;
        GC.SuppressFinalize(this);
    }
}

internal sealed class StreamStorageConsumerContext
{
    public string StreamId { get; set; }
    public bool Consuming { get; set; }
    public SemaphoreSlim ConsumeLock { get; } = new(1, 1);
    public IVideoStreamInit LatestInit { get; set; }
}

internal sealed class StreamSegmentStorageContext : IStreamSegmentStorageContext
{
    public string StreamId { get; set; }
    public ISession Session { get; set; }
    public IVideoStreamInit LatestInit { get; set; }
}
