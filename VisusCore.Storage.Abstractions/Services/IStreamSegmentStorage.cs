using VisusCore.Consumer.Abstractions.Services;

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
}
