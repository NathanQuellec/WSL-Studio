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

namespace WSLStudio.Services;

public class DataService : IDataService
{
    private IList<Distribution> _distributions;

    public DataService()
    {
        _distributions = new List<Distribution>();
    }

    public IList<Distribution> GetAllDistributions()
    {
        return _distributions;
    }

    public Distribution GetDistribution(int id)
    {
        return _distributions[id];
    }
    
    public void AddDistribution(Distribution distribution)
    {
        _distributions.Add(distribution);
        Debug.WriteLine($"Distribution {distribution.Name}");
    }

    public void DeleteDistribution(int id)
    {
        Debug.WriteLine("Delete distro");
    }
    
    public void UpdateDistribution(Distribution distribution)
    {
        Debug.WriteLine("Update distro");
    }
}
