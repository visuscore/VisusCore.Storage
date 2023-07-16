using VisusCore.Consumer.Abstractions.Models;
using YesSql;

namespace VisusCore.Storage.Services;

/// <summary>
/// Represents a storage context for video stream segment storage providers.
/// </summary>
public interface IStreamSegmentStorageContext
{
    /// <summary>
    /// Gets the stream identifier.
    /// </summary>
    string StreamId { get; }

    /// <summary>
    /// Gets the storage session.
    /// </summary>
    ISession Session { get; }

    /// <summary>
    /// Gets the latest init.
    /// </summary>
    IVideoStreamInit LatestInit { get; }
}
