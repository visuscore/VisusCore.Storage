using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;
using VisusCore.Consumer.Abstractions.Models;
using VisusCore.Storage.Abstractions.Services;

namespace VisusCore.Storage.Hubs;

public class StreamInfoHub : Hub
{
    private readonly IStreamSegmentStorageReader _storageReader;

    public StreamInfoHub(IStreamSegmentStorageReader storageReader) =>
        _storageReader = storageReader;

    public Task<IEnumerable<IVideoStreamSegmentMetadata>> GetSegmentsAsync(
        string streamId,
        long? from,
        long? to,
        int? skip,
        int? take) =>
        _storageReader.GetSegmentMetasAsync(
            streamId,
            from ?? 0,
            to,
            skip,
            take,
            Context.ConnectionAborted);
}
