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

namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private readonly IList<Distribution> _distros = new List<Distribution>();
    private readonly WslApi _wslApi = new WslApi();

    public DistributionService()
    {
        this.InitDistributionsList();
    }

    public void InitDistributionsList()
    {
        var apiDistroList = _wslApi.GetDistributionList()
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

    public IEnumerable<Distribution> GetAllDistributions()
    {
        return _distros;
    }

    public Distribution GetDistribution(int id)
    {
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
}