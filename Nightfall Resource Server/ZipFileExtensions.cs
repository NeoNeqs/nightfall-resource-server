using System.IO.Compression;

namespace Nightfall.ResourceServer;

public static class ZipFileExtensions
{
    public static bool AddFile(this ZipArchive archive, string sourceFileName, string entryName)
    {
        try
        {
            _ = archive.CreateEntryFromFile(sourceFileName, entryName);
        }
        catch
        {
            return false;
        }

        return true;
    }
}