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
using Serilog;


namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private const string WSL_UNC_PATH = @"\\wsl$";

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
        Log.Information("Fetching distributions list from windows registry ...");
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
            Log.Error($"Failed to fetch distributions list from windows registry - Caused by exception : {ex}");
        }
    }


    public IEnumerable<Distribution> GetAllDistributions()
    {
        return _distros;
    }

    public async Task<Distribution?> CreateDistribution(string distroName, string creationMode, string resourceOrigin)
    {
        var distroFolder = FilesHelper.CreateDirectory(App.AppDirPath, distroName);

        try
        {

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

            await TerminateDistribution(newDistro.Name); // to read ext4 file

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
            FilesHelper.RemoveDirectory(distroFolder);
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
            RemoveDistributionFolder(distribution);
            Log.Information($"DistributionService successfully deleted {distribution?.Name}");
        }
    }

    private static void RemoveDistributionFolder(Distribution distribution)
    {
        var distroPath = Directory.GetParent(distribution.Path).FullName;

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
    public async Task<bool> RenameDistribution(Distribution distribution, string newDistroName)
    {
        Log.Information($"Editing registry for {distribution.Name} with key : {distribution.Id}");
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

                distribution.Name = newDistroName;
                await TerminateDistribution(distribution.Name); // solve open file system error just after renaming distro
                return true;
            }

            lxsSubKeys.Close();
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to edit registry");
            return false;
        }
    }

    /*private async void RenameDistributionFolder(Distribution distribution, string newDistroName)
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
            Log.Information($"New distribution process is running wiht ID : {process.Id} and NAME : {process.ProcessName} started");
            distribution?.RunningProcesses.Add(process);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start process for launching distribution {distribution.Name} - Caused by exception : {ex}");
        }

    }

    /** Workaround to solve file system access error (Issue : https://github.com/microsoft/wsl/issues/5307)
        Because a distribution need to be running to use its file system, 
        we quickly start and stop the corresponding distribution to avoid an error  
    **/
    private static void BackgroundLaunchDistribution(Distribution distribution)
    {
        Log.Information($"Launching distribution {distribution.Name} in background ...");

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
            Log.Error($"Failed to start process for launching distribution in the background - Caused by exception : {ex}");
        }
    }

    public async void StopDistribution(Distribution distribution)
    {
        if (distribution.RunningProcesses.Count == 0)
        {
            Log.Warning($"Trying to stop {distribution.Name} but they aren't processes running for it");
        }
        else
        {
            foreach (var process in distribution.RunningProcesses)
            {

                process.CloseMainWindow();
                await process.WaitForExitAsync();

                if (process.HasExited)
                {
                    Log.Information($"Process ID : {process.Id} and NAME : {process.ProcessName} is closed");
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
        Log.Information($"Terminating distribution {distroName} ...");

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
            Log.Error($"Failed to start process to terminate distribution - Caused by exception {ex}");
        }
    }

    public async void OpenDistributionFileSystem(Distribution distribution)
    {
        var distroFileSystem = Path.Combine(WSL_UNC_PATH, $"{distribution.Name}");
        try
        {
            var distroIsRunning = await WslHelper.CheckRunningDistribution(distribution.Name);

            if (!distroIsRunning)
            {
                BackgroundLaunchDistribution(distribution);
            }

            var processBuilder = new ProcessBuilderHelper("explorer.exe")
                .SetArguments(distroFileSystem)
                .Build();
            processBuilder.Start();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start process for opening distribution file system - Caused by exception : {ex}");
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
            Log.Information($"Process ID : {process.Id} and NAME : {process.ProcessName} started");
           // distribution?.RunningProcesses.Add(process);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start process for opening distro with WinTerm - Caused by exception : {ex}");
        }
    }
}