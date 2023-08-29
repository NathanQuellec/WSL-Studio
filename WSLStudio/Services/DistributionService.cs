using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using ABI.Windows.UI.Text;
using Community.Wsl.Sdk;
using WSLStudio.Helpers;
using Microsoft.Win32;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Services.Factories;
using DiscUtils;
using DiscUtils.Dmg;
using DiscUtils.Ext;
using DiscUtils.Streams;
using DiscUtils.Vhdx;


namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private const string WSL_UNC_PATH = @"\\wsl$";
    private const string APP_FOLDER = "WslStudio";

    private readonly IList<Distribution> _distros;
    private readonly WslApi _wslApi;

    private readonly ISnapshotService _snapshotService;

    public DistributionService(ISnapshotService snapshotService)
    {
        _distros = new List<Distribution>();
        _wslApi = new WslApi();
        _snapshotService = snapshotService;
    }

    // TODO : Refactor InitDistributionsList
    public async Task InitDistributionsList()
    {
        try
        {
            var lxssRegPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");
            var lxssSubKeys = Registry.CurrentUser.OpenSubKey(lxssRegPath);

            foreach (var subKey in lxssSubKeys.GetSubKeyNames())
            {
                // we iterate only on distros registry keys
                if (!subKey.StartsWith('{') || !subKey.EndsWith('}'))
                {
                    continue;
                }

                var distroRegPath = Path.Combine(lxssRegPath, subKey);
                var distroSubkeys = Registry.CurrentUser.OpenSubKey(distroRegPath);
                var distroName = (string)distroSubkeys.GetValue("DistributionName");

                // Filter Docker special-purpose internal Linux distros 
                if (distroName != "docker-desktop" && distroName != "docker-desktop-data")
                {
                    var distroPath = (string)distroSubkeys.GetValue("BasePath");
                    var wslVersion = (int)distroSubkeys.GetValue("Version");

                    var distro = new Distribution()
                    {
                        Id = Guid.Parse(subKey),
                        Name = distroName,
                        Path = distroPath,
                        WslVersion = wslVersion,
                      //  Users = GetDistributionUsers(distroName),
                       // Snapshots = _snapshotService.GetDistributionSnapshots(distroPath),
                    };

                    distro.OsName = GetOsInfos(distro, "NAME");
                    distro.OsVersion = GetOsInfos(distro, "VERSION");
                    distro.Size = GetSize(distroPath);

                    this._distros.Add(distro);
                }
                distroSubkeys.Close();
            }
            lxssSubKeys.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static string GetOsInfos(Distribution distro, string field)
    {
        var osInfosPattern = $@"(\b{field}="")(.*?)""";
        var osInfosFile = Path.Combine("etc","os-release");
        var osInfosFileFallBack = Path.Combine("usr", "lib", "os-release");
        string osInfos;

        try
        {
            osInfos = GetOsInfosFromExt4(distro.Path, osInfosFile, osInfosPattern);
        }
        catch (FileNotFoundException ex)
        {
            // following os-release specs : https://www.freedesktop.org/software/systemd/man/os-release.html

            Console.WriteLine("Didn't find /etc/os-release, retry with fallback file");

            osInfos = GetOsInfosFromExt4(distro.Path, osInfosFileFallBack, osInfosPattern);
        }
        catch (IOException ex)
        {
            /*  if we cannot read ext4.dhdx, that means the distribution is running
                and we can get os-release file from the file system located at \\wsl$\distroname\...
             */

            Console.WriteLine("Another process is already reading ext4.vhdx");

            osInfos = GetOsInfosFromFileSystem(distro.Name, osInfosPattern);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);

            osInfos = "Unknown";
        }

        return string.IsNullOrEmpty(osInfos) ? "Unknown" : osInfos;
    }

    private static string GetOsInfosFromExt4(string distroPath, string osInfosFilePath, string osInfosPattern)
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
            Console.WriteLine("Didn't find /usr/lib/os-release");
            throw;
        }
        catch (IOException ex)
        {
            Console.WriteLine("Another process is already reading ext4.vhdx");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private static string GetOsInfosFromFileSystem(string distroName, string osInfosPattern)
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
            var osInfos = Regex.Match(streamReader.ReadToEnd(), osInfosPattern).Groups[2].Value;

            return (string.IsNullOrEmpty(osInfos) ? "Unknown" : osInfos);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Cannot get os infos from os-release file : " + ex.Message);
            return "Unknown";
        }
    }

    private static string GetSize(string distroPath)
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

    private static List<string> GetDistributionUsers(string distroName)
    {
        var passwdFilePath = Path.Combine(WSL_UNC_PATH, distroName, "etc", "passwd");
        var userShellPattern = @"/bin/(.*?)sh$";
        var usersList = new List<string>();

        try
        {

            Console.WriteLine("----------------GET USERS LIST----------------");
            using var streamReader = new StreamReader(passwdFilePath);

            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                var userShellRegex = Regex.Match(line, userShellPattern);

                // get first column of passwd file when matching regex (i.e. get user field)
                if (userShellRegex.Success)
                {
                    usersList.Add(line.Split(':')[0]);
                }
            }
            streamReader.Close();
            return usersList;
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("/etc/passwd file doesn't exist : " + e.Message);
            usersList.Add("Unknown");
            return usersList;
        }
        catch (IOException e)
        {
            Console.WriteLine("Cannot open or read /etc/passwd file : " + e.Message);
            usersList.Add("Unknown");
            return usersList;
        }
        catch (Exception e)
        {
            Console.WriteLine("Cannot get list of users from /etc/passwd file : " + e.Message);
            usersList.Add("Unknown");
            return usersList;
        }
    }

    

    public IEnumerable<Distribution> GetAllDistributions()
    {
        return _distros;
    }

    private static string CreateDistributionFolder(string distroName)
    {
        try
        {
            var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var appPath = Path.Combine(roamingPath, APP_FOLDER);

            if (!Directory.Exists(appPath))
            {
                Directory.CreateDirectory(appPath);
            }

            var distroFolder = Path.Combine(appPath, distroName);

            if (!Directory.Exists(distroFolder))
            {
                Directory.CreateDirectory(distroFolder);
            }

            return distroFolder;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return "";
        }
    }

    public async Task<Distribution?> CreateDistribution(string distroName, string creationMode, string resourceOrigin)
    {
        try
        {

            var distroFolder = CreateDistributionFolder(distroName);

            DistributionFactory factory = creationMode switch
            {
                "Dockerfile" => new DockerfileDistributionFactory(),
                "Archive" => new ArchiveDistributionFactory(),
                "Docker Hub" => new DockerHubDistributionFactory(),
                _ => throw new NullReferenceException(),
            };

            var newDistro = await factory.CreateDistribution(distroName, resourceOrigin, distroFolder);
            var distro = _wslApi
                .GetDistributionList()
                .FirstOrDefault(distro => distro.DistroName == newDistro.Name);

            TerminateDistribution(newDistro);

            newDistro.Id = distro.DistroId;
            newDistro.Path = distro.BasePath;
            newDistro.WslVersion = distro.WslVersion;
            newDistro.OsName = GetOsInfos(newDistro, "NAME");
            newDistro.OsVersion = GetOsInfos(newDistro, "VERSION");
            newDistro.Size = GetSize(newDistro.Path);
            newDistro.Users = GetDistributionUsers(newDistro.Name);

            this._distros.Add(newDistro);

            return newDistro;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    public void RemoveDistribution(Distribution distribution)
    {
        var process = new ProcessBuilderHelper("cmd.exe")
            .SetArguments($"/c wsl --unregister {distribution?.Name}")
            .SetCreateNoWindow(true)
            .Build();
        process.Start();

        if (distribution != null)
        {
            _distros.Remove(distribution);
            Console.WriteLine($"[INFO] Distribution {distribution?.Name} deleted");
        }
        else
        {
            throw new ArgumentNullException();
        }
    }

    /**
     * Rename distro name in the Windows Registry.
     * With MSIX packaging, this type of actions make changes in a virtual registry and do not edit the real one.
     * Because we want to modify the system's user registry, we use flexible virtualization in Package.appxmanifest file.
     */
    public bool RenameDistribution(Distribution distribution, string newDistroName)
    {
        Console.WriteLine($"[INFO] Editing Registry for {distribution.Name} with key : {distribution.Id}");
        var lxssRegPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");
        var lxsSubKeys = Registry.CurrentUser.OpenSubKey(lxssRegPath);

        foreach (var subKey in lxsSubKeys.GetSubKeyNames())
        {
            if (subKey != $"{{{distribution.Id.ToString()}}}")
            {
                continue;
            }

            var distroRegPath = Path.Combine(lxssRegPath, subKey);
            var distroSubkeys = Registry.CurrentUser.OpenSubKey(distroRegPath, true);
            Console.WriteLine(distroSubkeys.GetValue("DistributionName"));
            distroSubkeys.SetValue("DistributionName", newDistroName);
            Console.WriteLine($"OK {subKey}");
            distroSubkeys.Close();
            lxsSubKeys.Close();
            //this.RenameDistributionFolder(distribution.Name, newDistroName);
            return true;
        }
        lxsSubKeys.Close();
        return false;
    }

    // TODO : Rename distro folder
    /*public void RenameDistributionFolder(string distroName, string newDistroName)
    {
        var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var distroPath = Path.Combine(roamingPath, APP_FOLDER, distroName);
        var newDistroPath = Path.Combine(roamingPath, APP_FOLDER, newDistroName);

        try
        {
            if (Directory.Exists(distroPath))
            {
                Directory.Move(distroPath, newDistroPath);
                Console.WriteLine("Directory renamed successfully.");
            }
            else
            {
                Console.WriteLine("Source directory does not exist.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error renaming directory: " + e.Message);
        }
    }*/

    public void LaunchDistribution(Distribution distribution)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl ~ -d {distribution?.Name}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(true)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            Console.WriteLine($"[INFO] Process ID : {process.Id} and NAME : {process.ProcessName} started");
            distribution?.RunningProcesses.Add(process);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Process start failed for distro {distribution.Name}, reason : {ex}");
        }

    }

    public async void StopDistribution(Distribution distribution)
    {
        if (distribution.RunningProcesses.Count == 0)
        {
            Console.WriteLine($"[ERROR] Try to execute StopDistribution method but " +
                            $"they are no processes running for {distribution!.Name}");
        }
        else
        {
            foreach (var process in distribution.RunningProcesses)
            {

                process.CloseMainWindow();
                await process.WaitForExitAsync();

                if (process.HasExited)
                {
                    Console.WriteLine($"[INFO] Process ID : {process.Id} and " +
                                      $"NAME : {process.ProcessName} is closed");
                }
                else
                {
                    process.Kill();
                }
            }
            distribution.RunningProcesses.Clear();
        }
    }

    private void TerminateDistribution(Distribution distribution)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/wsl -t {distribution.Name}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public void OpenDistributionFileSystem(Distribution distribution)
    {
        var distroPath = Path.Combine(WSL_UNC_PATH, $"{distribution.Name}");
        var processBuilder = new ProcessBuilderHelper("explorer.exe")
            .SetArguments(distroPath)
            .Build();
        processBuilder.Start();
    }

    public void OpenDistributionWithVsCode(Distribution distribution)
    {
        var process = new ProcessBuilderHelper("cmd.exe")
            .SetArguments($"/c wsl ~ -d {distribution.Name} code .")
            .Build();
        process.Start();
    }

    public void OpenDistroWithWinTerm(Distribution distribution)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wt wsl ~ -d {distribution?.Name}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            Console.WriteLine($"[INFO] Process ID : {process.Id} and NAME : {process.ProcessName} started");
           // distribution?.RunningProcesses.Add(process);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Process start failed for distro {distribution.Name}, reason : {ex}");
        }
    }
}