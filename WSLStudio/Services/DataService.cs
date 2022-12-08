using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Diagnostics;
namespace WSLStudio.Services;

public class DataService : IDataService
{
    private IList<Distribution> _distributions;

    public DataService()
    {
        _distributions = new List<Distribution>
        {
            new Distribution { Name = "Ubuntu" },
            new Distribution { Name = "Kali" },
            new Distribution { Name = "Debian" },
        };
    }

    public void DeleteDistribution(int id)
    {
        Debug.WriteLine("Delete distro");
    }
    public void AddDistribution(Distribution distribution)
    {
        Debug.WriteLine($"Distribution {distribution.Name}");
    }
    public IList<Distribution> GetAllDistributions()
    {
        return _distributions;
    }
    public Distribution GetDistribution(int id)
    {
        return _distributions[id];
    }
    public void UpdateDistribution(Distribution distribution)
    {
        Debug.WriteLine("Update distro");
    }
}
