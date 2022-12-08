using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDataService
{
    IList<Distribution> GetAllDistributions();
    Distribution GetDistribution(int id);
    void AddDistribution(Distribution distribution);
    void DeleteDistribution(int id);
    void UpdateDistribution(Distribution distribution);
}
