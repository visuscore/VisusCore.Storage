namespace VisusCore.Storage.Abstractions.Models;

/// <summary>
/// Represents a key for a stream segment.
/// </summary>
public interface IStreamSegmentKey
{
    /// <summary>
    /// Gets the stream id.
    /// </summary>
    string StreamId { get; }

    /// <summary>
    /// Gets the document id.
    /// </summary>
    long DocumentId { get; }

    /// <summary>
    /// Gets the timestamp elapsed in microseconds since Unix epoch.
    /// </summary>
    long TimestampUtc { get; }

    /// <summary>
    /// Gets the initializaion segment id.
    /// </summary>
    long InitId { get; }
}
