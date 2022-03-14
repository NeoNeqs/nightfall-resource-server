using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Nightfall.ResourceServer;

[ApiController]
[Route("patch/")]
public class FileController : ControllerBase
{
#if DEBUG
    private readonly IWebHostEnvironment _hostingEnvironment;

    public FileController(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment;
    }
#endif

    [HttpGet("{patchHash}")]
    public IActionResult Get([Required] string patchHash)
    {
        if (!patchHash.IsSha256String())
        {
            return BadRequest("Not a valid sha256 hash!");
        }

#if DEBUG
        string realPath = Path.Combine(_hostingEnvironment.ContentRootPath, "tests", patchHash, "latest.patch");
#else
        string realPath = Path.Combine("/volume", patchHash, "latest.patch");
#endif

        Stream stream;
        try
        {
            stream = new FileStream(realPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                FileOptions.Asynchronous);
        }
        catch (Exception _)
        {
#if DEBUG
            return BadRequest(_.Message);
#else
            return BadRequest();
#endif
        }

        HttpContext.Response.RegisterForDisposeAsync(stream);

        return File(stream, "application/octet-stream", "latest.patch", true);
    }
}