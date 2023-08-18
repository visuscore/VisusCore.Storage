using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.FileDB;

namespace VisusCore.Storage.Disk.Services;

public class BlobDatabaseCacheEntry : IDisposable
{
    private readonly string _path;
    private readonly SemaphoreSlim _readWriteLock = new(1, 1);
    // TODO: Consider using tar archive instead of BlobDatabase.
    private BlobDatabase _blobDatabaseReaderWriter;
    private bool _disposed;

    public BlobDatabaseCacheEntry(string path) => _path = path;

    public Task InvokeOnReadLockAsync(
        Func<BlobDatabase, Task> actionAsync,
        CancellationToken cancellationToken = default) =>
        InvokeOnReadWriteLockAsync(actionAsync, cancellationToken);

    public Task<TResult> InvokeOnReadLockAsync<TResult>(
        Func<BlobDatabase, Task<TResult>> actionAsync,
        CancellationToken cancellationToken = default) =>
        InvokeOnReadWriteLockAsync(actionAsync, cancellationToken);

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

    private void CleanUp(bool canDelete)
    {
        if (canDelete)
        {
            try
            {
                File.Delete(_path);
            }
            catch
            {
                // TODO: Do something here.
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _readWriteLock.Dispose();
                bool canDelete = _blobDatabaseReaderWriter?.ListFiles().Length == 0;
                _blobDatabaseReaderWriter?.Dispose();
                CleanUp(canDelete);
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
