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
using System.IO;


namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private const string WSL_UNC_PATH = @"\\wsl$";
    private const string APP_FOLDER = "WslStudio";
    private static readonly string Roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private readonly IList<Distribution> _distros;
    private readonly WslApi _wslApi;

    private readonly IDistributionInfosService _distroInfosService;
    private readonly  ISnapshotService _snapshotService;

    public DistributionService(ISnapshotService snapshotService, IDistributionInfosService distroInfosService)
    {
        _distros = new List<Distribution>();
        _wslApi = new WslApi();

        _distroInfosService = distroInfosService;
        _snapshotService = snapshotService;
    }

    // TODO : Refactor InitDistributionsList
    public void InitDistributionsList()
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
                    };

                    distro.OsName = _distroInfosService.GetOsInfos(distro, "NAME");
                    distro.OsVersion = _distroInfosService.GetOsInfos(distro, "VERSION");
                    distro.Size = _distroInfosService.GetSize(distroPath);
                    distro.Users = _distroInfosService.GetDistributionUsers(distro);
                    distro.Snapshots = _snapshotService.GetDistributionSnapshots(distroPath);

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


    public IEnumerable<Distribution> GetAllDistributions()
    {
        return _distros;
    }

    private static string CreateDistributionFolder(string distroName)
    {
        try
        {
            var appPath = Path.Combine(Roaming, APP_FOLDER);

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

            if (!Directory.Exists(distroFolder))
            {
                throw new DirectoryNotFoundException();
            }

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

            TerminateDistribution(newDistro.Name); // to read ext4 file

            newDistro.Id = distro.DistroId;
            newDistro.Path = distro.BasePath;
            newDistro.WslVersion = distro.WslVersion;
            newDistro.OsName = _distroInfosService.GetOsInfos(newDistro, "NAME");
            newDistro.OsVersion = _distroInfosService.GetOsInfos(newDistro, "VERSION");
            newDistro.Size = _distroInfosService.GetSize(newDistro.Path);
            newDistro.Users = _distroInfosService.GetDistributionUsers(newDistro);

            this._distros.Add(newDistro);

            return newDistro;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            RemoveDistributionFolder(distroName);
            throw;
        }
    }

    public async Task RemoveDistribution(Distribution distribution)
    {
        var process = new ProcessBuilderHelper("cmd.exe")
            .SetArguments($"/c wsl --unregister {distribution?.Name}")
            .SetCreateNoWindow(true)
            .Build();
        process.Start();

        await process.WaitForExitAsync();

        if (process.HasExited)
        {
            _distros.Remove(distribution);
            RemoveDistributionFolder(distribution.Name);
            Console.WriteLine($"[INFO] Distribution {distribution?.Name} deleted");
        }
    }

    public void RemoveDistributionFolder(string distroName)
    {
        var distroPath = Path.Combine(Roaming, APP_FOLDER, distroName);

        if (Directory.Exists(distroPath))
        {
            Directory.Delete(distroPath, true);
        }
    }

    /**
     * Rename distro name in the Windows Registry.
     * With MSIX packaging, this type of actions make changes in a virtual registry and do not edit the real one.
     * Because we want to modify the system's user registry, we use flexible virtualization in Package.appxmanifest file.
     */
    // TODO : Refactor
    public bool RenameDistribution(Distribution distribution, string newDistroName)
    {
        Console.WriteLine($"[INFO] Editing Registry for {distribution.Name} with key : {distribution.Id}");
        var lxssRegPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");

        try
        {
            using var lxsSubKeys = Registry.CurrentUser.OpenSubKey(lxssRegPath);

            foreach (var subKey in lxsSubKeys.GetSubKeyNames())
            {
                if (subKey != $"{{{distribution.Id}}}")
                {
                    continue;
                }

                var distroRegPath = Path.Combine(lxssRegPath, subKey);
                var distroSubkeys = Registry.CurrentUser.OpenSubKey(distroRegPath, true);

                distroSubkeys.SetValue("DistributionName", newDistroName);
                distroSubkeys.Close();
                // TODO : Change distro path
                RenameDistributionFolder(distribution, newDistroName);

                distribution.Name = newDistroName;
                distribution.Path = distribution.Path.Replace(distribution.Name, newDistroName); // rename distro name in distro path

                return true;
            }

            lxsSubKeys.Close();
          //  TerminateDistribution(newDistroName);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    private async void RenameDistributionFolder(Distribution distribution, string newDistroName)
    {
        var distroPath = Path.Combine(Roaming, APP_FOLDER, distribution.Name);
        var newDistroPath = Path.Combine(Roaming, APP_FOLDER, newDistroName);

        try
        {
            if (Directory.Exists(distroPath))
            {
                await TerminateDistribution(newDistroName);
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
    }

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

    private static async Task<bool> CheckRunningDistribution(Distribution distribution)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments("/c wsl --list --running --quiet")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();

            var output = process.StandardOutput.ReadToEndAsync().GetAwaiter().GetResult();
            await process.WaitForExitAsync();
            var sanitizedOutput = output.Replace("\0", "").Replace("\r", "");  // remove special character
            var runningDistros = sanitizedOutput.Split("\n");

            return runningDistros.Contains(distribution.Name);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Process start failed for distro {distribution.Name}, reason : {ex}");
            return false;
        }
    }

    /** Workaround to solve file system access error (Issue : https://github.com/microsoft/wsl/issues/5307)
        Because a distribution need to be running to use its file system, 
        we quickly start and stop the corresponding distribution to avoid an error  
    **/
    private static void BackgroundLaunchDistribution(Distribution distribution)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl -d {distribution?.Name}")
                .SetCreateNoWindow(true)
                .SetUseShellExecute(false)
                .Build();
            process.Start();

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

    private static async Task TerminateDistribution(string distroName)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl --terminate {distroName}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async void OpenDistributionFileSystem(Distribution distribution)
    {
        var distroPath = Path.Combine(WSL_UNC_PATH, $"{distribution.Name}");
        try
        {
            var distroIsRunning = await CheckRunningDistribution(distribution);

            if (!distroIsRunning)
            {
                BackgroundLaunchDistribution(distribution);
            }

            var processBuilder = new ProcessBuilderHelper("explorer.exe")
                .SetArguments(distroPath)
                .Build();
            processBuilder.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        
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