using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Community.Wsl.Sdk;
using Docker.DotNet;
using WSLStudio.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.Win32;

namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private const string WSL_UNC_PATH = @"\\wsl.localhost";

    private IList<Distribution> _distros = new List<Distribution>();
    private readonly WslApi _wslApi = new();

    public void InitDistributionsList()
    {
        try
        {
            var apiDistroList = _wslApi?.GetDistributionList()
                // Filter Docker special-purpose internal Linux distros 
                .Where(distro => (distro.DistroName != "docker-desktop") &&
                                 (distro.DistroName != "docker-desktop-data"))
                .Select(distro => new Distribution()
                {
                    Id = distro.DistroId,
                    Path = distro.BasePath,
                    IsDefault = distro.IsDefault,
                    WslVersion = distro.WslVersion,
                    Name = distro.DistroName,
                });

            if (apiDistroList == null)
                return;

            foreach (var distro in apiDistroList) 
                this._distros.Add(distro);
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine("INFO: No WSL distributions found in the system");
        }
    }

    public IEnumerable<Distribution> GetAllDistributions()
    {
        return _distros;
    }

    public void CreateDistribution()
    {
        DockerClient client = new DockerClientConfiguration(
                new Uri("npipe//./pipe/docker_engine"))
            .CreateClient();


    }

    public void RemoveDistribution(Distribution? distribution)
    {
        var process =  new ProcessBuilderHelper("cmd.exe")
            .SetArguments($"/c wsl --unregister {distribution?.Name}")
            .SetRedirectStandardOutput(false)
            .SetUseShellExecute(false)
            .SetCreateNoWindow(true)
            .Build();
        process.Start();

        if (distribution != null)
        {
            _distros.Remove(distribution);
            Debug.WriteLine($"[INFO] Distribution {distribution?.Name} deleted");
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
    public void RenameDistribution(Distribution? distribution)
    {
        Debug.WriteLine(this._distros);
        Debug.WriteLine($"[INFO] Editing Registry for {distribution.Name} with key : {distribution.Id}");
        var lxssRegPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");
        var lxsSubKeys = Registry.CurrentUser.OpenSubKey(lxssRegPath);

        foreach (var subKey in lxsSubKeys.GetSubKeyNames())
        {
            if (subKey == $"{{{distribution?.Id.ToString()}}}")
            {
                var distroRegPath = Path.Combine(lxssRegPath, subKey);
                var distroSubkeys = Registry.CurrentUser.OpenSubKey(distroRegPath, true);
                Debug.WriteLine(distroSubkeys.GetValue("DistributionName"));
                distroSubkeys.SetValue("DistributionName", distribution.Name);
                Debug.WriteLine($"OK {subKey}");
                distroSubkeys.Close();
            }
        }
        lxsSubKeys.Close();
    }

    public void LaunchDistribution(Distribution? distribution)
    {
        try
        {

            var lxssRegPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");
            var lxsSubKeys = Registry.CurrentUser.OpenSubKey(lxssRegPath);

            foreach (var subKey in lxsSubKeys.GetSubKeyNames())
            {
                if (subKey == $"{{{distribution?.Id.ToString()}}}")
                {
                    var distroRegPath = Path.Combine(lxssRegPath, subKey);
                    var distroSubkeys = Registry.CurrentUser.OpenSubKey(distroRegPath, true);
                    Debug.WriteLine(distroSubkeys.GetValue("DistributionName"));
                    distroSubkeys.Close();
                }
            }
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl ~ -d {distribution?.Name}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(true)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            Debug.WriteLine($"[INFO] Process ID : {process.Id} and NAME : {process.ProcessName} started");
            distribution?.RunningProcesses.Add(process);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Process start failed for distro {distribution.Name}");
        }

    }

    public void StopDistribution(Distribution? distribution)
    {
        if (distribution?.RunningProcesses == null)
        {
            Debug.WriteLine($"[ERROR] Try to execute StopDistribution method but " +
                            $"they are no processes running for {distribution.Name}");
        }
        else
        {
            foreach (var process in distribution.RunningProcesses)
            {

                process.CloseMainWindow();
                process.WaitForExit(30000);

                if (process.HasExited)
                {
                    Debug.WriteLine($"[INFO] Process ID : {process.Id} and " +
                                    $"NAME : {process.ProcessName} is closed");
                }
            }
        }
    }

    // TODO: Check why opening distro file system invoke sometimes an error. 

    public void OpenDistributionFileSystem(Distribution? distribution)
    {
        string distroPath = Path.Combine(WSL_UNC_PATH, $"{distribution.Name}");

        var processBuilder = new ProcessBuilderHelper("explorer.exe")
            .SetArguments(distroPath)
            .Build();
        processBuilder.Start();
    }

}