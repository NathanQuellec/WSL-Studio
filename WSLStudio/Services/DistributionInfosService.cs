using System.Globalization;
using System.Text.RegularExpressions;
using Serilog;
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
    public string GetOsInfos(Distribution distribution, string field)
    {
        Log.Information($"Fetching OS information of distribution {distribution.Name} ...");

        var osInfosPattern = $@"(\b{field}="")(.*?)""";
        var osInfosFile = Path.Combine("etc", "os-release");
        var osInfosFileFallBack = Path.Combine("usr", "lib", "os-release");
        string osInfos;

        try
        {
            osInfos = GetOsInfosFromVhdx(distribution.Path, osInfosFile, osInfosPattern);
        }
        catch (FileNotFoundException ex)
        {
            // fallback following os-release specs : https://www.freedesktop.org/software/systemd/man/os-release.html

            Log.Error($"Didn't find /etc/os-release, retry with fallback file - Caused by exception : {ex}");
            osInfos = GetOsInfosFromVhdx(distribution.Path, osInfosFileFallBack, osInfosPattern);
        }
        catch (IOException ex)
        {
            /*  if we cannot read ext4.dhdx, that means the distribution is running
                and we can get os-release file from the file system located at \\wsl$\distroname\...
             */

            Log.Error($"Another process is already reading ext4.vhdx - Caused by exception : {ex}");
            osInfos = GetOsInfosFromFileSystem(distribution.Name, osInfosPattern);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed fetch OS information - Caused by exception {ex}");
            osInfos = "Unknown";
        }

        return string.IsNullOrEmpty(osInfos) ? "Unknown" : osInfos;
    }

    private static string GetOsInfosFromVhdx(string distroPath, string osInfosFilePath, string osInfosPattern)
    {
        Log.Information($"Fetching OS information from WSL vhdx image ...");

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
            Log.Error($"Didn't find /usr/lib/os-release - Caused by exception : {ex}");
            throw;
        }
        catch (IOException ex)
        {
            Log.Error($"Another process is already reading ext4.vhdx - Caused by exception : {ex}");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed fetch OS information from WSL vhdx image - Caused by exception {ex}");
            throw;
        }
    }

    private static string GetOsInfosFromFileSystem(string distroName, string osInfosPattern)
    {
        Log.Information($"Fetching OS information from os-release file of {distroName} FS ...");

        var osInfosFilePath = Path.Combine(WSL_UNC_PATH, distroName, "etc", "os-release");

        try
        {
            var osInfosFile = new FileInfo(osInfosFilePath);
            if (osInfosFile.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                // we cannot read a symlink, so we use the fallback os-release file located at /usr/lib/os-release
                Log.Warning("/etc/os-release is a symbolic link to /usr/lib/os-release");
                osInfosFilePath = Path.Combine(WSL_UNC_PATH, distroName, "usr", "lib", "os-release");
            }

           // using var streamReader = new StreamReader(osInfosFilePath);
            var content = File.ReadAllText(osInfosFilePath);
            var osInfos = Regex.Match(content, osInfosPattern).Groups[2].Value;

            return (string.IsNullOrEmpty(osInfos) ? "Unknown" : osInfos);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to fetch os infos from os-release file - Caused by exception : {ex} ");
            return "Unknown";
        }
    }

    public string GetSize(string distroPath)
    {
        Log.Information("Getting distribution size from wsl vhdx image ...");

        try
        {
            var diskLocation = Path.Combine(distroPath, "ext4.vhdx");
            var diskFile = new FileInfo(diskLocation);
            var sizeInGB = (decimal)diskFile.Length / 1024 / 1024 / 1024;

            return Math.Round(sizeInGB, 2).ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to get distribution size from wsl vhdx image - Caused by exception : {ex} ");
            return "0";
        }
    }

    public List<string> GetDistributionUsers(Distribution distribution)
    {
        Log.Information("Getting distribution's users list ...");

        const string userShellPattern = @"/bin/(.*?)sh$";
        var usersList = new List<string>();

        try
        {
            usersList = GetUsersFromExt4(distribution.Path, userShellPattern);

        }
        catch (IOException ex)
        {
            Log.Error($"Failed to get distro users from ext4.vhdx image file - Caused by exception : {ex}");
            usersList = GetUsersFromFileSystem(distribution.Name, userShellPattern);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to get distro users from file system - Caused by exception : {ex}");
            usersList.Add("Unknown");
        }

        return usersList;
    }

    private static List<string> GetUsersFromExt4(string distroPath, string userShellPattern)
    {
        Log.Information("Getting distribution's users list from wsl vhdx image ...");

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
        catch (IOException ex)
        {
            Log.Error($"Cannot read ext4.vhdx image file - Caused by exception : {ex}");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Cannot get list of users from /etc/passwd file - Caused by exception : {ex}");
            return new List<string>() { "Unknown" };
        }
    }

    private static List<string> GetUsersFromFileSystem(string distroName, string userShellPattern)
    {
        Log.Information("Getting distribution's users list from distro FS ...");

        var passwdFilePath = Path.Combine(WSL_UNC_PATH, distroName, "etc", "passwd");

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
        catch (Exception ex)
        {
            Log.Error($"Cannot get list of users from /etc/passwd file : {ex}");
            return new List<string>() { "Unknown" };
        }
    }
}