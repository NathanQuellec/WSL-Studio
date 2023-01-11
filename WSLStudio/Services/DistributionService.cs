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
using WSLStudio.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.Win32;

namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private readonly IList<Distribution> _distros = new List<Distribution>();
    private readonly WslService _wslService = new();
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
            foreach (var distro in apiDistroList)
            {
                this.AddDistribution(distro);
            }
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

    public Distribution GetDistribution(int id)
    {
        return _distros[id];
    }

    public void AddDistribution(Distribution? distribution)
    {
        if (distribution != null)
        {
            _distros.Add(distribution);
            Debug.WriteLine($"[INFO] Distribution {distribution.Name} added");
        }
        else
        {
            throw new ArgumentNullException();
        }
    }

    public void RemoveDistribution(Distribution? distribution)
    {
        ProcessBuilderHelper processBuilderHelper = new();

        var process = processBuilderHelper.SetFileName("cmd.exe")
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

    public void RenameDistribution(Distribution? distribution, string newDistroName)
    {
        Debug.WriteLine("Update distro");
        var lxssPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");
        RegistryKey? key = Registry.CurrentUser.OpenSubKey(lxssPath);

        foreach (var subKey in key.GetSubKeyNames())
        {
            if (subKey == distribution?.Id.ToString())
            {
                Debug.WriteLine("OK");
            }
        }
        key.Close();

    }

    public void LaunchDistribution(Distribution? distribution)
    {
        try
        {
            ProcessBuilderHelper processBuilderHelper = new();
            var process = processBuilderHelper.SetFileName("cmd.exe")
                .SetArguments($"/c wsl -d {distribution?.Name}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(true)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            Debug.WriteLine($"[INFO] Process ID : {process.Id} and NAME : {process.ProcessName} started");
            distribution.RunningProcesses.Add(process);
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
}