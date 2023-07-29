using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VisusCore.Storage.Abstractions.Services;

namespace VisusCore.Storage.Controllers;

[Route("storage/stream-info")]
public class StreamInfoController : Controller
{
    private readonly IStreamSegmentStorageReader _storageReader;

    public StreamInfoController(IStreamSegmentStorageReader storageReader) =>
        _storageReader = storageReader;

    [HttpGet("{streamId}/segments")]
    public async Task<IActionResult> GetSegments(
        string streamId,
        long? from,
        long? to,
        int? skip,
        int? take)
    {
        var segments = await _storageReader.GetSegmentMetasAsync(
            streamId,
            from ?? 0,
            to,
            skip,
            take,
            HttpContext.RequestAborted);

        return Json(segments);
    }
}
