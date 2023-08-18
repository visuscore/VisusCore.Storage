using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisusCore.Storage.Abstractions.Services;
using VisusCore.Storage.Models;

namespace VisusCore.Storage.Hubs;

public class StreamInfoHub : Hub
{
    private readonly IStreamSegmentStorageReader _storageReader;

    public StreamInfoHub(IStreamSegmentStorageReader storageReader) =>
        _storageReader = storageReader;

    public async Task<VideoStreamSegmentMetadata[]> GetSegmentsAsync(
        string streamId,
        long? from,
        long? to,
        int? skip,
        int? take) =>
        (await _storageReader.GetSegmentMetasAsync(
            streamId,
            from ?? 0,
            to,
            skip,
            take,
            Context.ConnectionAborted))
        .Select(metadata => new VideoStreamSegmentMetadata
        {
            Duration = metadata.Duration,
            FrameCount = metadata.FrameCount,
            StreamId = metadata.StreamId,
            TimestampProvided = metadata.TimestampProvided,
            TimestampUtc = metadata.TimestampUtc,
            Size = metadata.Size,
        })
        .ToArray();
}
