using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionService
{
    void InitDistributionsList();
    IList<Distribution> GetAllDistributions();
    Distribution GetDistribution(int id);
    void AddDistribution(Distribution distro);
    void DeleteDistribution(int id);
    void UpdateDistribution(Distribution distribution);
    //void LaunchDistribution(Distribution distribution);
    //void StopDistribution(Distribution distribution);
}
