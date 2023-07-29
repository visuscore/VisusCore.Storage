using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.FileDB;

namespace VisusCore.Storage.Disk.Services;

public class BlobDatabaseCacheAccessor : IDisposable
{
    private readonly MemoryCache _blobDatabaseCache;
    private bool _disposed;

    public BlobDatabaseCacheAccessor(ILoggerFactory loggerFactory) =>
        _blobDatabaseCache = new MemoryCache(new MemoryCacheOptions(), loggerFactory);

    public Task InvokeOnReadWriteLockAsync(
        string databasePath,
        Func<BlobDatabase, Task> actionAsync,
        CancellationToken cancellationToken = default) =>
        GetOrCreate(databasePath).InvokeOnReadWriteLockAsync(actionAsync, cancellationToken);

    public Task<TResult> InvokeOnReadWriteLockAsync<TResult>(
        string databasePath,
        Func<BlobDatabase, Task<TResult>> actionAsync,
        CancellationToken cancellationToken = default) =>
        GetOrCreate(databasePath).InvokeOnReadWriteLockAsync(actionAsync, cancellationToken);

    public Task InvokeOnReadLockAsync(
        string databasePath,
        Func<BlobDatabase, Task> actionAsync,
        CancellationToken cancellationToken = default) =>
        GetOrCreate(databasePath).InvokeOnReadLockAsync(actionAsync, cancellationToken);

    public Task<TResult> InvokeOnReadLockAsync<TResult>(
        string databasePath,
        Func<BlobDatabase, Task<TResult>> actionAsync,
        CancellationToken cancellationToken = default) =>
        GetOrCreate(databasePath).InvokeOnReadLockAsync(actionAsync, cancellationToken);

    private BlobDatabaseCacheEntry GetOrCreate(string databasePath) =>
        _blobDatabaseCache.GetOrCreate(databasePath, CreateBlobDatabaseCacheEntry(databasePath));

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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _blobDatabaseCache.Dispose();
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
}
