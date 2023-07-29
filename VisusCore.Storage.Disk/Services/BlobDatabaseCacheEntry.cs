using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.FileDB;

namespace VisusCore.Storage.Disk.Services;

public class BlobDatabaseCacheEntry : IDisposable
{
    private readonly string _path;
    private readonly SemaphoreSlim _readLock = new(1, 1);
    private readonly SemaphoreSlim _readWriteLock = new(1, 1);
    private BlobDatabase _blobDatabaseReader;
    private BlobDatabase _blobDatabaseReaderWriter;
    private bool _disposed;

    public BlobDatabaseCacheEntry(string path) => _path = path;

    public async Task InvokeOnReadLockAsync(
        Func<BlobDatabase, Task> actionAsync,
        CancellationToken cancellationToken = default)
    {
        await _readLock.WaitAsync(cancellationToken);

        try
        {
            _blobDatabaseReader ??= new BlobDatabase(_path, FileAccess.Read);

            await actionAsync(_blobDatabaseReader);
        }
        finally
        {
            _readLock.Release();
        }
    }

    public async Task<TResult> InvokeOnReadLockAsync<TResult>(
        Func<BlobDatabase, Task<TResult>> actionAsync,
        CancellationToken cancellationToken = default)
    {
        await _readLock.WaitAsync(cancellationToken);

        try
        {
            _blobDatabaseReader ??= new BlobDatabase(_path, FileAccess.Read);

            return await actionAsync(_blobDatabaseReader);
        }
        finally
        {
            _readLock.Release();
        }
    }

    public async Task InvokeOnReadWriteLockAsync(
        Func<BlobDatabase, Task> actionAsync,
        CancellationToken cancellationToken = default)
    {
        await _readWriteLock.WaitAsync(cancellationToken);

        try
        {
            _blobDatabaseReaderWriter ??= new BlobDatabase(_path, FileAccess.ReadWrite);

            await actionAsync(_blobDatabaseReaderWriter);
        }
        finally
        {
            _readWriteLock.Release();
        }
    }

    public async Task<TResult> InvokeOnReadWriteLockAsync<TResult>(
        Func<BlobDatabase, Task<TResult>> actionAsync,
        CancellationToken cancellationToken = default)
    {
        await _readWriteLock.WaitAsync(cancellationToken);

        try
        {
            _blobDatabaseReaderWriter ??= new BlobDatabase(_path, FileAccess.ReadWrite);

            return await actionAsync(_blobDatabaseReaderWriter);
        }
        finally
        {
            _readWriteLock.Release();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _readLock.Dispose();
                _readWriteLock.Dispose();
                _blobDatabaseReader?.Dispose();
                _blobDatabaseReaderWriter?.Dispose();
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
