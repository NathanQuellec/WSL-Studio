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

public class DistributionService : IDistributionService
{
    private readonly IList<Distribution> _distros = new List<Distribution>();

    public DistributionService()
    {
        this.InitDistributionsList();
    }

    public void InitDistributionsList()
    {
        var process = new ProcessBuilderService()
            .SetFileName("cmd.exe")
            .SetArguments("/c wsl --list")
            .SetRedirectStandardOutput(true)
            .SetUseShellExecute(false)
            .SetCreateNoWindow(true)
            .Build();

        process.Start();

        var commandResults = process.StandardOutput.ReadToEnd();
        commandResults = commandResults.Replace("\0", string.Empty).Replace("\r", string.Empty);
        var distrosResults = commandResults.Split('\n');

        // remove "Default" in the prompt result 
        distrosResults[1] = distrosResults[1].Split(" ")[0];
        Debug.WriteLine("-----------LIST OF WSL DISTROS-----------");
        for (var i = 1; i < distrosResults.Length; i++)
        {
            // Exclude empty line(s) and Docker special-purpose internal Linux distros 
            if ( distrosResults[i].Trim().Length > 0 && 
                distrosResults[i] != "docker-desktop" && 
                distrosResults[i] != "docker-desktop-data" )
            {
                this.AddDistribution( new Distribution { Name = distrosResults[i].Trim() } );
            }
        }
    }

    public IList<Distribution> GetAllDistributions()
    {
        return _distros;
    }

    public Distribution GetDistribution(int id)
    {
        //TEST ACTIONS
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
