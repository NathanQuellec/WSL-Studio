using System.Globalization;
using System.Text.RegularExpressions;
using WSLStudio.Contracts.Services;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services;

/** <summary>
 * Fetch informations about distributions (os, users, ...)
 * </summary>
 */
public class DistributionInfosService : IDistributionInfosService
{
    private const string WSL_UNC_PATH = @"\\wsl$";

    /*  To get distributions infos, we try at first to read the image "ext4.vhdx" and open the file /etc/os-release.
        If we cannot read ext4.dhdx, that means the distribution is runningand we can get os-release file from the 
        file system located at \\wsl$\distroname\...
    */
    public string GetOsInfos(Distribution distro, string field)
    {
        var osInfosPattern = $@"(\b{field}="")(.*?)""";
        var osInfosFile = Path.Combine("etc", "os-release");
        var osInfosFileFallBack = Path.Combine("usr", "lib", "os-release");
        string osInfos;

        try
        {
            osInfos = GetOsInfosFromExt4(distro.Path, osInfosFile, osInfosPattern);
        }
        catch (FileNotFoundException ex)
        {
            // fallback following os-release specs : https://www.freedesktop.org/software/systemd/man/os-release.html

            Console.WriteLine("Didn't find /etc/os-release, retry with fallback file : " + ex.Message);
            osInfos = GetOsInfosFromExt4(distro.Path, osInfosFileFallBack, osInfosPattern);
        }
        catch (IOException ex)
        {
            /*  if we cannot read ext4.dhdx, that means the distribution is running
                and we can get os-release file from the file system located at \\wsl$\distroname\...
             */

            Console.WriteLine("Another process is already reading ext4.vhdx : " + ex.Message);
            osInfos = GetOsInfosFromFileSystem(distro.Name, osInfosPattern);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            osInfos = "Unknown";
        }

        return string.IsNullOrEmpty(osInfos) ? "Unknown" : osInfos;
    }

    private string GetOsInfosFromExt4(string distroPath, string osInfosFilePath, string osInfosPattern)
    {
        var wslImagePath = Path.Combine(distroPath, "ext4.vhdx");

        try
        {

            var wslImageHelper = new WslImageHelper(wslImagePath);
            var fileContent = wslImageHelper.ReadFile(osInfosFilePath);
            var osInfos = Regex.Match(fileContent, osInfosPattern)
                .Groups[2].Value;

            return osInfos;
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine("Didn't find /usr/lib/os-release : " + ex.Message);
            throw;
        }
        catch (IOException ex)
        {
            Console.WriteLine("Another process is already reading ext4.vhdx : " + ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private string GetOsInfosFromFileSystem(string distroName, string osInfosPattern)
    {
        var osInfosFilePath = Path.Combine(WSL_UNC_PATH, distroName, "etc", "os-release");

        try
        {
            var osInfosFile = new FileInfo(osInfosFilePath);

            if (osInfosFile.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                // we cannot read a symlink, so we use the fallback os-release file located at /usr/lib/os-release
                Console.WriteLine("/etc/os-release is a symbolic link to /usr/lib/os-release");
                osInfosFilePath = Path.Combine(WSL_UNC_PATH, distroName, "usr", "lib", "os-release");
            }

            using var streamReader = new StreamReader(osInfosFilePath);
            var content = streamReader.ReadToEnd();
            var osInfos = Regex.Match(content, osInfosPattern).Groups[2].Value;

            return (string.IsNullOrEmpty(osInfos) ? "Unknown" : osInfos);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Cannot get os infos from os-release file : " + ex.Message);
            return "Unknown";
        }
    }

    public string GetSize(string distroPath)
    {
        try
        {
            var diskLocation = Path.Combine(distroPath, "ext4.vhdx");
            var diskFile = new FileInfo(diskLocation);
            var sizeInGB = (decimal)diskFile.Length / 1024 / 1024 / 1024;

            return Math.Round(sizeInGB, 2).ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return "0";
        }
    }

    public List<string> GetDistributionUsers(Distribution distribution)
    {
        const string userShellPattern = @"/bin/(.*?)sh$";
        var usersList = new List<string>();

        try
        {
            usersList = GetUsersFromExt4(distribution.Path, userShellPattern);

        }
        catch (IOException e)
        {
            Console.WriteLine("Cannot get users from ext4.vhdx image file : " + e.Message);
            usersList = GetUsersFromFileSystem(distribution.Name, userShellPattern);
        }
        catch (Exception e)
        {
            Console.WriteLine("Cannot get list of users : " + e.Message);
            usersList.Add("Unknown");
        }

        return usersList;
    }

    private List<string> GetUsersFromExt4(string distroPath, string userShellPattern)
    {
        var passwdFilePath = Path.Combine("etc", "passwd");
        var wslImagePath = Path.Combine(distroPath, "ext4.vhdx");

        try
        {
            var wslImageHelper = new WslImageHelper(wslImagePath);
            var fileContent = wslImageHelper.ReadFile(passwdFilePath);
            var lines = fileContent.Split('\n');

            var users = lines
                .Where(line => Regex.Match(line, userShellPattern).Success)
                .Select(line => line.Split(':')[0])
                .ToList();

            return users;
        }
        catch (IOException e)
        {
            Console.WriteLine("Cannot read ext4.vhdx image file : " + e.Message);
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine("Cannot get list of users from /etc/passwd file : " + e.Message);
            return new List<string>() { "Unknown" };
        }
    }

    private List<string> GetUsersFromFileSystem(string distroName, string userShellPattern)
    {
        var passwdFilePath = Path.Combine(WSL_UNC_PATH, distroName, "etc", "passwd");
        var usersList = new List<string>();

        try
        {
            using var streamReader = new StreamReader(passwdFilePath);
            var users = streamReader.ReadToEnd()
                .Split("\n")
                .Where(line => Regex.Match(line, userShellPattern).Success)
                .Select(line => line.Split(':')[0])
                .ToList();

            return users;
        }
        catch (Exception e)
        {
            Console.WriteLine("Cannot get list of users from /etc/passwd file : " + e.Message);
            return new List<string>() { "Unknown" };
        }
    }
}