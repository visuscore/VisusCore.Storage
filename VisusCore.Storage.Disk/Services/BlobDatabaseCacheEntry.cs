using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.FileDB;

namespace VisusCore.Storage.Disk.Services;

public class BlobDatabaseCacheEntry : IDisposable
{
    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private BlobDatabase _blobDatabase;
    private bool _disposed;

    public BlobDatabaseCacheEntry(string path) => _path = path;

    public async Task InvokeOnLockAsync(
        Func<BlobDatabase, Task> actionAsync,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            _blobDatabase ??= new BlobDatabase(_path, FileAccess.ReadWrite);
            await actionAsync(_blobDatabase);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<TResult> InvokeOnLockAsync<TResult>(
        Func<BlobDatabase, Task<TResult>> actionAsync,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            _blobDatabase ??= new BlobDatabase(_path, FileAccess.ReadWrite);
            return await actionAsync(_blobDatabase);
        }
        finally
        {
            _lock.Release();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _lock.Dispose();
                _blobDatabase?.Dispose();
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
