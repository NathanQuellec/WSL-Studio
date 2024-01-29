using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
