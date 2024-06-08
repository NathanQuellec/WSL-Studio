using ICSharpCode.SharpZipLib.GZip;
using Serilog;
using WSLStudio.Exceptions;

namespace WSLStudio.Helpers;
public static class FilesHelper
{
    public static string? CreateDirectory(string parentDirPath, string dirName)
    {
        Log.Information("Creating new directory ...");
        try
        {
            var newDirPath = Path.Combine(parentDirPath, dirName);

            if (!Directory.Exists(newDirPath))
            {
                Directory.CreateDirectory(newDirPath);

            }

            return newDirPath;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create new directory - Caused by exception {ex}");
            return null;
        }
    }

    public static void RemoveDirectory(string dirPath)
    {
        try
        {
            Directory.Delete(dirPath);
        }
        catch (DirectoryNotFoundException ex)
        {
            Log.Error($"Cannot failed directory to remove at {dirPath}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to remove {dirPath} - Caused by exception {ex}");
        }
    }

    public static void RemoveDirContent(string dirPath)
    {
        var dirInfo = new DirectoryInfo(dirPath);

        foreach (var file in dirInfo.EnumerateFiles())
        {
            file.Delete();
        }

        foreach (var subDir in dirInfo.EnumerateDirectories())
        {
            subDir.Delete();
        }
    }

    public static async Task ExtractGzFile(string sourceFile, string destFile)
    {
        Log.Information($"Extracting snapshot from .tar to .tar.gz in location {sourceFile}");
        try
        {

            await using var gZipInputStream = new GZipInputStream(File.OpenRead(sourceFile));
            await using var extractedFile = File.Create(destFile);
            await gZipInputStream.CopyToAsync(extractedFile);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to extract file {sourceFile} - Caused by exception : {ex}");
            throw new GzFileExtractionException();
        }
    }
}
