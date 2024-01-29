using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpCompress;

namespace WSLStudio.Helpers;
public static class FilesHelper
{
    public static string? CreateDirectory(string parentDirPath, string dirName)
    {
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
            Console.WriteLine(ex.Message);
            return null;
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
}
