using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VisusCore.Consumer.Abstractions.Models;

namespace VisusCore.Storage.Abstractions.Services;

/// <summary>
/// Represents a storage reader for video stream segments.
/// </summary>
public interface IStreamSegmentStorageReader
{
    /// <summary>
    /// Gets segments for the given <paramref name="streamId"/> and <paramref name="startTimestampUtc"/>.
    /// </summary>
    /// <param name="streamId">The stream id.</param>
    /// <param name="startTimestampUtc">The timestamp where the searh is starting from.</param>
    Task<IEnumerable<IVideoStreamSegment>> GetSegmentsAsync(
            string streamId,
            long startTimestampUtc,
            long? endTimestampUtc,
            int? skip,
            int? take,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets segment metadata for the given <paramref name="streamId"/> and <paramref name="startTimestampUtc"/>.
    /// </summary>
    /// <param name="streamId">The stream id.</param>
    /// <param name="startTimestampUtc">The timestamp where the searh is starting from.</param>
    Task<IEnumerable<IVideoStreamSegmentMetadata>> GetSegmentMetasAsync(
        string streamId,
        long startTimestampUtc,
        long? endTimestampUtc,
        int? skip,
        int? take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the segment where the <paramref name="expectedTimestampUtc"/> is in.
    /// </summary>
    Task<IVideoStreamSegment> GetSegmentAroundAsync(
        string streamId,
        long expectedTimestampUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the segment metadata where the <paramref name="expectedTimestampUtc"/> is in.
    /// </summary>
    Task<IVideoStreamSegmentMetadata> GetSegmentMetaAroundAsync(string streamId, long expectedTimestampUtc);
}
