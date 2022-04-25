using System.IO.Compression;

namespace Nightfall.ResourceServer;

public class CacheService : IHostedService
{
    private static readonly Dictionary<string, Dictionary<string, string>> Updates = new();

    private static string _latestUpdateHash = null!;

    private readonly IHostApplicationLifetime _applicationLifetime;

    private readonly ILogger<CacheService> _logger;
#if DEBUG
    private readonly IWebHostEnvironment _env;
    private string Root => Path.Combine(_env.ContentRootPath, "tests");

    public CacheService(IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime,
        ILogger<CacheService> logger)
    {
        _env = env;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }
#else
    private static string Root => "/volume";

    public CacheService(IHostApplicationLifetime applicationLifetime, Logger<CacheService> logger)
    {
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }
#endif

    public Task StartAsync(CancellationToken cancellationToken) => Task.Run(CacheUpdates, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Compares <paramref name="receivedChecksums"/> with <see cref="Updates"/> ('latest' key)
    /// and finds files and patches to send
    /// </summary>
    /// <param name="receivedChecksums"></param>
    /// <returns></returns>
    public MemoryStream FindDeltas(Dictionary<string, string> receivedChecksums)
    {
        var outputStream = new MemoryStream();
        using var archiveStream = new ZipArchive(outputStream, ZipArchiveMode.Create, true);

        // Check if the update hash exists on the server.
        if (Updates.ContainsKey(receivedChecksums["*update_hash"]))
        {
            // Loop over latest update and compare it with client's version
            foreach ((string fileName, string checksum) in Updates[_latestUpdateHash])
            {
                // Removing matched entries so that excess entries that are not present in the latest update can be dealt with
                if (receivedChecksums.Remove(fileName, out string? receivedChecksum))
                {
                    // Seems like the file is present on the client. Compare checksums.

                    // Checksums match -> skip
                    if (checksum == receivedChecksum) continue;

                    // Checksums are different -> look for a patch and zip it
                    bool success = ZipPatchFile(archiveStream, receivedChecksums["*update_hash"], fileName);
                    if (!success)
                    {
                        // Zip the whole file instead
                        _ = ZipLatestFile(archiveStream, fileName);
                    }

                    continue;
                }

                // Client doesn't have the file. Zip the whole file.
                _ = ZipLatestFile(archiveStream, fileName);
            }

            // Handle leftover entries 
            foreach ((string fileName, _) in receivedChecksums)
            {
                if (fileName.StartsWith('*')) continue;
                // File isn't present in the latest update. Put an empty entry to inform that this file should be deleted 
                // empty .patch file means that there is nothing to patch (duh) which indicates that file should be deleted.
                // It has to be a .patch file since any empty file could be used by application in some way
                _ = archiveStream.CreateEntry($"{fileName}.patch");
            }
        }
        else
        {
            // Update hash doesn't represent any known update.
            // This could mean several things:
            //      1. The update is missing on the server
            //      2. One of the file sent by the client was modified thus the update hash is different (obsolete, read TODO below)
            //      3. Malformed data
            // In that case zip all files from latest update
            // TODO: Each update should have a file that contains the update hash so that it's not calculated by the client to avoid mismatch if files were modified by the client.
            foreach ((string fileName, string checksum) in Updates[_latestUpdateHash])
            {
                // Client has a file but it could be not up to date.
                if (receivedChecksums.Remove(fileName, out string? receivedChecksum))
                {
                    // Checksums match - don't zip this file
                    if (checksum == receivedChecksum) continue;

                    // Otherwise send the whole file because the folder with the update doesn't exist and so the patch file.
                    _ = ZipLatestFile(archiveStream, fileName);
                    continue;
                }

                // Client doesn't have the file. Send the whole file.
                _ = ZipLatestFile(archiveStream, fileName);
            }

            // Handle leftover entries 
            foreach ((string fileName, _) in receivedChecksums)
            {
                if (fileName.StartsWith('*')) continue;
                // Those entries aren't present in the latest update and they should be deleted.
                _ = archiveStream.CreateEntry($"{fileName}.patch");
            }
        }

        return outputStream;
    }

    private void CacheUpdates()
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
            _logger.LogCritical("Could not list directories of {Path}", Root);
            _applicationLifetime.StopApplication();
            return;
        }

        string file;
        string[] lines;
        foreach (string dir in dirs)
        {
            file = Path.Combine(dir, ".info");

            try
            {
                lines = File.ReadAllLines(file);
            }
            catch
            {
                _logger.LogWarning("The info file {infoFile} was not found or access was denied.", file);
                continue;
            }

            Updates[Path.GetFileName(dir)] = ParseInfoFile(lines);
        }

        file = Path.Combine(Root, ".info");
        try
        {
            lines = File.ReadAllLines(file);
        }
        catch
        {
            _logger.LogCritical("The info file {infoFile} was not found or access was denied.", file);
            _applicationLifetime.StopApplication();
            return;
        }

        var parsedInfoFile = ParseInfoFile(lines);
        if (!parsedInfoFile.ContainsKey("latest"))
        {
            _logger.LogCritical("The info file {infoFile} needs to have a mapping to the latest update.", file);
            _applicationLifetime.StopApplication();
            return;
        }

        _latestUpdateHash = parsedInfoFile["latest"];


        // There should be at least 1 update
        if (!Updates.ContainsKey(_latestUpdateHash))
        {
            _logger.LogCritical("There is not update that would point to the latest update with hash {hash}",
                _latestUpdateHash);
            _applicationLifetime.StopApplication();
            return;
        }

        // There should at least be one file in an update
        if (Updates[_latestUpdateHash].Count != 0) return;

        _logger.LogCritical("The latest update should have at least 1 file.");
        _applicationLifetime.StopApplication();
    }

    /// <summary>
    /// Parses <paramref name="lines"/> as an info file.  
    /// Info file is a list of key-value pairs of file names and their checksums, separated by a space.
    /// </summary>
    /// <remarks>
    /// Format:
    ///     file_name checksum
    ///     another_file another_checksum
    /// </remarks>
    /// <param name="lines"></param>
    /// <returns>Parsed info file as <see cref="Dictionary{TKey, TValue}"/></returns>
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

            if (!split[0].IsValidFileName())
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

    private bool ZipLatestFile(ZipArchive archive, string fileName)
    {
        string fileFullPath = Path.Combine(Root, _latestUpdateHash, "files", fileName);

        try
        {
            _ = archive.CreateEntryFromFile(fileFullPath, fileName);
        }
        catch
        {
            return false;
        }

        return true;
    }

    private bool ZipPatchFile(ZipArchive archive, string updateHash, string fileName)
    {
        var patchFileName = $"{fileName}.patch";
        string patchFileFullPath = Path.Combine(Root, updateHash, "patch", patchFileName);

        try
        {
            _ = archive.CreateEntryFromFile(patchFileFullPath, patchFileName);
        }
        catch
        {
            return false;
        }

        return true;
    }
}