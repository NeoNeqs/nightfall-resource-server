using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;

namespace Nightfall.ResourceServer;

[ApiController]
[Route("patch/")]
public class FileController : ControllerBase
{
#if DEBUG
    private readonly IWebHostEnvironment _env;

    public FileController(IWebHostEnvironment env)
    {
        _env = env;
        UpdateCache();
    }

    private string GetPatch(string hash) => Path.Combine(Root, hash, TargetFile);
    private string Root => Path.Combine(_env.ContentRootPath, "tests");

#else
    public FileController()
    {
        UpdateCache();
    }

    private static string GetPatch(string hash) => Path.Combine(Root, hash, TargetFile);
    private static string Root => "/volume";
#endif


    private const string TargetFile = "latest.zip";

    private static readonly Dictionary<string, Dictionary<string, string>> Updates = new();


    private void UpdateCache()
    {
        Updates.Clear();
        EnumerationOptions options = new() {AttributesToSkip = FileAttributes.Hidden, ReturnSpecialDirectories = false};

        IEnumerable<string> dirs;
        try
        {
            dirs = Directory.EnumerateDirectories(Root, "*", options);
        }
        catch
        {
            return;
        }

        foreach (string dir in dirs)
        {
            string file = Path.Combine(dir, ".info");

            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(file);
            }
            catch
            {
                continue;
            }

            Updates[Path.GetFileName(dir)] = ParseInfoFile(lines);
        }

        if (!Updates.ContainsKey("latest"))
        {
            // TODO: give a reason for exiting
            Environment.Exit(-1);
        }
    }

    private static Dictionary<string, string> ParseInfoFile(IEnumerable<string> lines)
    {
        Dictionary<string, string> result = new();

        foreach (string line in lines)
        {
            string[] split = line.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (split.Length != 2)
            {
                continue;
            }

            if (split[0].IsValidFileName())
            {
                continue;
            }

            if (!split[1].IsSha256String())
            {
                continue;
            }

            result[split[0]] = split[1];
        }

        return result;
    }

    [HttpPost]
    [RequestSizeLimit(500000)]
    public IActionResult Post(Dictionary<string, string> receivedChecksums)
    {
        if (!receivedChecksums.ContainsKey("*update_hash") || !receivedChecksums["*update_hash"].IsSha256String())
        {
            return BadRequest();
        }

        // TODO: schedule a delete for this file
        string tempFile = Path.GetTempFileName();

        // TODO: make it async
        using ZipArchive archive = ZipFile.Open(tempFile, ZipArchiveMode.Create);

        foreach ((string fileName, string checksum) in Updates["latest"])
        {
            if (receivedChecksums.TryGetValue(fileName, out string? receivedChecksum))
            {
                if (!receivedChecksum.IsSha256String()) continue;
                if (receivedChecksum == checksum) continue;
                
                // look for the patch of this file or send a full fil
                var file = $"{fileName}.patch";
                string filePath = Path.Combine(Root, receivedChecksums["*update_hash"], "patch", file);
                if (archive.AddFile(filePath, file))
                {
                    
                }
                try
                {
                   
                    
                    _ = archive.CreateEntryFromFile(filePath, file);
                }
                catch
                {
                    try
                    {
                        // get the full file then
                        archive.CreateEntryFromFile(Path.Combine(Root, "latest", "files", fileName), fileName);
                    }
                    catch
                    {
                        // That means problem if we continue here. SHould never happen!
                        continue;
                    }
                }
            }
            else
            {
                // send full file
            }
        }
        
        return null;
    }
    
    

    [HttpGet("{patchHash}")]
    public IActionResult Get([Required] string patchHash)
    {
        if (!patchHash.IsSha256String())
        {
            return BadRequest("Not a valid sha256 hash!");
        }

        Stream stream;
        try
        {
            stream = OpenFileRead(GetPatch(patchHash));
        }
        catch (FileNotFoundException)
        {
            // Did not find 'latest.zip' file, but the directory exists, which means the client has the latest update installed.
            return BadRequest();
        }
        catch (DirectoryNotFoundException)
        {
            // Did not found directory, could mean lots of things: maliciously crafted url, corrupted or really out of date client.
            // In that case just return the latest update (not a patch).

            // NOTE: 'latest' folder should be a link to another folder that actually contains the latest update.
            // This is used to know if the server ever needs to send anything or just return NoContent
            try
            {
                stream = OpenFileRead(GetPatch("latest"));
            }
            catch (Exception)
            {
                return Problem("Internal server error.");
            }
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

        return File(stream, "application/zip", TargetFile, true);
    }

    private static Stream OpenFileRead(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
    }
}