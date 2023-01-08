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

namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private readonly IList<Distribution> _distros = new List<Distribution>();
    private readonly ProcessBuilderHelper _processBuilderHelper = new();
    private readonly WslService _wslService = new();
    private readonly WslApi _wslApi = new();

    public DistributionService()
    {
        if (_wslService.CheckWsl())
            this.InitDistributionsList();
    }

    public  void InitDistributionsList()
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
        //TEST ACTIONS 2
        return _distros[id];
    }

    public void AddDistribution(Distribution distro)
    {
        _distros.Add(distro);
        Debug.WriteLine($"Distribution {distro.Name} added");
    }

    public void DeleteDistribution(int id)
    {
        Debug.WriteLine("Delete distro");
    }

    public void UpdateDistribution(Distribution distro)
    {
        Debug.WriteLine("Update distro");
    }

    public void LaunchDistribution(Distribution distribution)
    {
        var process = _processBuilderHelper.SetFileName("cmd.exe")
            .SetArguments($"/c wsl -d {distribution.Name}")
            .SetRedirectStandardOutput(false)
            .SetUseShellExecute(true)
            .SetCreateNoWindow(true)
            .Build();
        process.Start();
    }
}