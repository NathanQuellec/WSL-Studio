using DiscUtils.Ext;
using DiscUtils.Streams;
using System.Text.RegularExpressions;
using System.Text;

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
        try
        {
            // open vhdx and get bytes stream 
            using var vhdxFile = new DiscUtils.Vhdx.DiskImageFile(_vhdxImagePath, FileAccess.Read);
            using SparseStream vhdxStream = vhdxFile.OpenContent(null, Ownership.None);

            // get ext4 object based on vhdx stream, and open the specified file
            using var ext4Stream = new ExtFileSystem(vhdxStream);
            using SparseStream fileStream = ext4Stream.OpenFile(fileToExtract, FileMode.Open);

            int bytesToRead = (int)fileStream.Length;
            byte[] buffer = new byte[bytesToRead];
            int pos = 0;

            /*
               Stream.Read can read less than the number of bytes requested by the third parameter, 
               so we use a loop to be sure that we read all the bytes of the file.
            */
            while (pos < bytesToRead)
            {
                int bytesRead = fileStream.Read(buffer, pos, bytesToRead - pos);
                pos += bytesRead;
            }

            var fileText = Encoding.UTF8.GetString(buffer);

            return fileText;
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine("File not found : " + ex.Message);
            throw;
        }
        catch (IOException ex)
        {
            Console.WriteLine("Cannot read distribution image file : " + ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}