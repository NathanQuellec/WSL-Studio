using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using Serilog;

namespace WSLStudio.Helpers;

public static class ArchiveHelper
{
    // we merge multiple tar by removing EOF marker for each, except for the last one.
    // the new archive is just the result of all archives data with only one EOF marker.
    public static async Task MergeArchive(List<string> tarPathList, string destPath)
    {
        Log.Information($"Merging archive files ...");

        try
        {
            using var mergedArchive = File.Open(destPath, FileMode.Append);
            for (var i = 0; i < tarPathList.Count - 1; i++)
            {
                using var tarFile = File.Open(tarPathList[i], FileMode.Open, FileAccess.ReadWrite);
                var tarFileSize = tarFile.Length;
                tarFile.SetLength(tarFileSize - 1024);  // remove archive's eof marker
                await tarFile.CopyToAsync(mergedArchive);
            }
            using var lastTarFile = File.Open(tarPathList.Last(), FileMode.Open, FileAccess.ReadWrite);
            await lastTarFile.CopyToAsync(mergedArchive);
        }
        catch(Exception ex)
        {
            Log.Error($"Failed to merged archive files - Caused by exception : {ex}");
        }
    }
       
    public static async Task<string?> DecompressArchive(string path)
    {
        Log.Information($"Decompressing archive file ...");
        try
        {
            await using var tarGzFile = File.OpenRead(path);
            var newTarFilePath = path.Replace(".gz", " ").Trim();  // remove gz extension
            using var newTarFile = File.Create(newTarFilePath);

            await using var tarGzExtract = new GZipStream(tarGzFile, CompressionMode.Decompress);
            await tarGzExtract.CopyToAsync(newTarFile);

            tarGzFile.Close();
            newTarFile.Close();
            File.Delete(path);

            return newTarFilePath;
        }
        catch(Exception ex)
        {
            Log.Error($"Failed to decompress archive file - Caused by exception : {ex}");
            return null;
        }
    }
}
