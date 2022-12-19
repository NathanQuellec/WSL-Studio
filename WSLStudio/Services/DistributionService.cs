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

public class DataDistrosService : IDataDistrosService
{
    private readonly IList<Distribution> _distros;

    public DataDistrosService()
    {
        _distros = new List<Distribution>();
        WslCommandProcess wslCommandProcess = new WslCommandProcess();
        ProcessStartInfo psi = wslCommandProcess.Config("cmd.exe", "/c wsl --list");
        var proc = Process.Start(psi);
        wslCommandProcess.WslCommandPromptResults(proc);
    }

    public IList<Distribution> GetAllDistributions()
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
        Debug.WriteLine($"Distribution {distro.Name}");
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
