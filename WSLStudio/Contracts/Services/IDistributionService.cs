using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionService
{
    void InitDistributionsList();
    IEnumerable<Distribution> GetAllDistributions();
    Distribution GetDistribution(int id);
    void AddDistribution(Distribution? distro);
    void DeleteDistribution(int id);
    void UpdateDistribution(Distribution? distribution);
    void LaunchDistribution(Distribution? distribution);
    void StopDistribution(Distribution distribution);
}
