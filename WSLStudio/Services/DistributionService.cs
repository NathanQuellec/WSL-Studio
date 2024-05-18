using System.Globalization;
using Community.Wsl.Sdk;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using Serilog;
using WSLStudio.Contracts.Services;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Messages;
using WSLStudio.Models;
using WSLStudio.Services.Factories;


namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private const string WSL_UNC_PATH = @"\\wsl$";

    private readonly IList<Distribution> _distros;
    private readonly WslApi _wslApi;

    private readonly IDistributionInfosService _distroInfosService;
    private readonly ISnapshotService _snapshotService;

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

                    var distro = new DistributionBuilder()
                        .WithId(Guid.Parse(subKey))
                        .WithName(distroName)
                        .WithPath(distroPath)
                        .WithWslVersion(wslVersion)
                        .WithOsName(_distroInfosService.GetOsInfos(distroName, distroPath, "NAME"))
                        .WithOsVersion(_distroInfosService.GetOsInfos(distroName, distroPath, "VERSION"))
                        .WithSize(_distroInfosService.GetSize(distroPath))
                        .WithUsers(_distroInfosService.GetDistributionUsers(distroName, distroPath))
                        .WithSnapshots(_snapshotService.GetDistributionSnapshots(distroPath))
                        .Build();

                    distro.SnapshotsTotalSize = distro.Snapshots
                        .Sum(snapshot => decimal.Parse(snapshot.Size, CultureInfo.InvariantCulture))
                        .ToString(CultureInfo.InvariantCulture);

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


    // TODO : Refactor
    public async Task<Distribution?> CreateDistribution(string distroName, string creationMode, string resourceOrigin)
    {
        var distroFolder = Path.Combine(App.DistroDirPath, distroName);

        try
        {
            DistributionFactory factory = creationMode switch
            {
                "Dockerfile" => new DockerfileDistributionFactory(),
                "Archive" => new ArchiveDistributionFactory(),
                "Docker Hub" => new DockerHubDistributionFactory(),
                _ => throw new NullReferenceException(),
            };

            var newDistro = await factory.CreateDistribution(distroName, resourceOrigin, distroFolder);

            // fetch distro infos created by WSL
            var distro = _wslApi
                .GetDistributionList()
                .FirstOrDefault(distro => distro.DistroName == newDistro.Name);

            await TerminateDistribution(distroName); // to read ext4 file
            newDistro.Id = distro.DistroId;
            newDistro.Path = distro.BasePath;
            newDistro.WslVersion = distro.WslVersion;
            newDistro.OsName = _distroInfosService.GetOsInfos(newDistro.Name, newDistro.Path, "NAME");
            newDistro.OsVersion = _distroInfosService.GetOsInfos(newDistro.Name, newDistro.Path, "VERSION");
            newDistro.Size = _distroInfosService.GetSize(newDistro.Path);
            newDistro.Users = _distroInfosService.GetDistributionUsers(newDistro.Name, newDistro.Path);

            this._distros.Add(newDistro);

            return newDistro;
        }
        catch (Exception ex)
        {
            Log.Error($"Error while creating wsl distribution - Caused by {ex}");
            throw;
        }
    }

    public async Task RemoveDistribution(Distribution distribution)
    {
        var process = new ProcessBuilder("cmd.exe")
            .SetArguments($"/c wsl --unregister {distribution.Name}")
            .SetCreateNoWindow(true)
            .Build();
        process.Start();

        await process.WaitForExitAsync();

        if (process.HasExited)
        {
            _distros.Remove(distribution);
            RemoveDistributionFolder(distribution);
            Log.Information($"DistributionService successfully deleted {distribution.Name}");
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
     * TODO REFACTOR
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

                var oldDistroName = distribution.Name;
                await TerminateDistribution(newDistroName); // solve error when opening file system just after renaming distro
                var isFolderRenamed = RenameDistributionFolder(oldDistroName, newDistroName);

                if (isFolderRenamed)
                {
                    var newDistroPath = distribution.Path.Replace(oldDistroName, newDistroName);
                    distroSubkeys.SetValue("DistributionName", newDistroName);
                    distroSubkeys.SetValue("BasePath", newDistroPath);
                    distribution.Name = newDistroName;
                    distribution.Path = newDistroPath;
                }

                distroSubkeys.Close();
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

    private static bool RenameDistributionFolder(string oldDistroName, string newDistroName)
    {
        var oldDistroPath = Path.Combine(App.AppDirPath, oldDistroName);
        var newDistroPath = Path.Combine(App.AppDirPath, newDistroName);

        try
        {
            if (!Directory.Exists(oldDistroPath))
            {
                Log.Information("Source directory does not exist.");
                throw new DirectoryNotFoundException();
            }
            //await TerminateDistribution(newDistroName);
            File.Copy(oldDistroPath, newDistroPath);
            Directory.Move(oldDistroPath, newDistroPath);
            Log.Information("Directory renamed successfully.");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Error renaming directory: " + ex.Message);
            return false;
        }
    }

    public void LaunchDistribution(Distribution distribution)
    {
        try
        {
            var process = new ProcessBuilder("cmd.exe")
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
            var process = new ProcessBuilder("cmd.exe")
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
            var process = new ProcessBuilder("cmd.exe")
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

            var processBuilder = new ProcessBuilder("explorer.exe")
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
        var process = new ProcessBuilder("cmd.exe")
            .SetArguments($"/c wsl ~ -d {distribution.Name} code .")
            .Build();
        process.Start();
    }

    public void OpenDistroWithWinTerm(Distribution distribution)
    {
        try
        {
            var process = new ProcessBuilder("cmd.exe")
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