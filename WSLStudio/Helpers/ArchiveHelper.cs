using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;

namespace WSLStudio.Helpers;

public static class ArchiveHelper
{
    public static async Task MergeArchive(List<string> tarPathList, string destPath)
    {
        using var mergedArchive = File.Open(destPath, FileMode.Append);
        for (int i = 0; i < tarPathList.Count - 1; i++)
        {
            using var currentTarFile = File.OpenRead(tarPathList[i]);
            using var memoryStream = new MemoryStream();
            await currentTarFile.CopyToAsync(memoryStream);
            var bytesArray = memoryStream.ToArray();
            bytesArray = bytesArray.SkipLast(1024).ToArray(); // remove archive end marker
            await mergedArchive.WriteAsync(bytesArray);
            currentTarFile.Dispose();
        }
        using var mem2 = new MemoryStream();
        using var lastTarFile = File.OpenRead(tarPathList.Last());
        await lastTarFile.CopyToAsync(mem2); // TOO LONG CHECK FILESTREAM
        var arr2 = mem2.ToArray();
        await mergedArchive.WriteAsync(arr2, 0, arr2.Length);
        mergedArchive.Close();
        lastTarFile.Close();
    }
       
    /**<summary>Decompress .tar.gz file to get a .tar file and return path of the new file</summary> **/

    public static async Task<string> DecompressArchive(string path)
    {
        await using var tarGzFile = File.OpenRead(path);
        var gzipInputStream = new GZipInputStream(tarGzFile);

        var newTarFilePath = path.Replace(".gz", " ").Trim();  // remove gz extension
        var newTarFile = File.Create(newTarFilePath);

        await using var tarGzExtract = new GZipStream(tarGzFile, CompressionMode.Decompress);
        await tarGzExtract.CopyToAsync(newTarFile);

        newTarFile.Close();
        tarGzFile.Close();
        File.Delete(path);
        return newTarFilePath;
    }
}
