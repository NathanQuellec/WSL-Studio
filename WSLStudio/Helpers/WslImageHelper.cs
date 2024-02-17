using DiscUtils.Ext;
using DiscUtils.Streams;
using System.Text.RegularExpressions;
using System.Text;
using Serilog;

namespace WSLStudio.Helpers;

/**
 * Class that make operations on images of wsl distributions (ext4.vhdx file).
 */
public class WslImageHelper
{
    private readonly string _vhdxImagePath;

    public WslImageHelper(string vhdxImagePath)
    {
        _vhdxImagePath = vhdxImagePath;
    }

    /** <summary>
     *  Read a specific file from a wsl image (ext4.vhdx) and return the results in ASCII
     * </summary>
     */
    public string ReadFile(string fileToExtract) 
    {
        Log.Information("Reading distribution image file for extraction");
        try
        {
            // open vhdx and get bytes stream 
            using var vhdxFile = new DiscUtils.Vhdx.DiskImageFile(_vhdxImagePath, FileAccess.Read);
            using SparseStream vhdxStream = vhdxFile.OpenContent(null, Ownership.None);

            // get ext4 object based on vhdx stream, and open the specified file
            using var ext4File = new ExtFileSystem(vhdxStream);
            using SparseStream fileStream = ext4File.OpenFile(fileToExtract, FileMode.Open);

            var bytesToRead = (int)fileStream.Length;
            var buffer = new byte[bytesToRead];
            var pos = 0;

            /*
               Stream.Read can read less than the number of bytes requested by the third parameter, 
               so we use a loop to be sure that we read all the bytes of the file.
            */
            while (pos < bytesToRead)
            {
                var bytesRead = fileStream.Read(buffer, pos, bytesToRead - pos);
                pos += bytesRead;
            }

            var fileText = Encoding.UTF8.GetString(buffer);

            fileStream.Close();
            vhdxStream.Close();

            return fileText;
        }
        catch (FileNotFoundException ex)
        {
            Log.Error($"Cannot find file to extract - Caused by exception {ex}");
            throw;
        }
        catch (IOException ex)
        {
            Log.Error($"Cannot read distribution image file - Caused by exception {ex}");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Cannot extract image file - Caused by exception {ex}");
            throw;
        }
    }
}