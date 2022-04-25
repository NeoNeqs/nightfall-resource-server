using Microsoft.AspNetCore.Mvc;

namespace Nightfall.ResourceServer;

[ApiController]
[Route("update/")]
public class UpdateController : ControllerBase
{
    private readonly CacheService _cacheService;

    public UpdateController(CacheService cacheService)
    {
        _cacheService = cacheService;
    }

    [HttpPost]
    [RequestSizeLimit(3000)]
    public async Task<IActionResult> Post(Dictionary<string, string> receivedChecksums)
    {
        if (!receivedChecksums.ContainsKey("*update_hash"))
        {
            return BadRequest();
        }

        MemoryStream result;
        try
        {
            result = await Task.Run(() => _cacheService.FindDeltas(receivedChecksums));
        }
        catch
        {
            return Problem("Internal server issue.");
        }

        HttpContext.Response.RegisterForDispose(result);
        
        result.Position = 0;
        if (result.Length <= 22)
        {
            return NoContent();
        }

        return File(result, "application/zip", "update.zip");
    }
}