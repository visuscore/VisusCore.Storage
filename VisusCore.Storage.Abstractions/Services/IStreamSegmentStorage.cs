using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.Consumer.Abstractions.Models;
using VisusCore.Consumer.Abstractions.Services;
using VisusCore.Storage.Abstractions.Models;

namespace VisusCore.Storage.Abstractions.Services;

/// <summary>
/// Represents a storage for video stream segments.
/// </summary>
public interface IStreamSegmentStorage : IVideoStreamSegmentConsumer
{
    /// <summary>
    /// Gets the storage name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the storage provider.
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// Gets the <see cref="IVideoStreamSegment"/> for the given <paramref name="keys"/>.
    /// </summary>
    /// <typeparam name="TStreamSegmentKey">The key type.</typeparam>
    Task<IEnumerable<IVideoStreamSegment>> GetSegmentsByKeyAsync<TStreamSegmentKey>(
        IEnumerable<TStreamSegmentKey> keys,
        Func<TStreamSegmentKey, IVideoStreamInit, byte[], Task<IVideoStreamSegment>> converterAsync,
        CancellationToken cancellationToken = default)
        where TStreamSegmentKey : IStreamSegmentKey;

    /// <summary>
    /// Deletes the <see cref="IVideoStreamSegment"/> for the given <paramref name="keys"/>.
    /// </summary>
    /// <typeparam name="TStreamSegmentKey">The key type.</typeparam>
    Task DeleteSegmentsByKeyAsync<TStreamSegmentKey>(
        IEnumerable<TStreamSegmentKey> keys,
        CancellationToken cancellationToken = default)
        where TStreamSegmentKey : IStreamSegmentKey;
}
