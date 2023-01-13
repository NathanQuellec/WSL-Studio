using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionService
{
    void InitDistributionsList();
    IEnumerable<Distribution> GetAllDistributions();
    Distribution GetDistribution(int id);
    void AddDistribution(Distribution? distribution);
    void RemoveDistribution(Distribution? distribution);
    void RenameDistribution(Distribution? distribution);
    void LaunchDistribution(Distribution? distribution);
    void StopDistribution(Distribution? distribution);
}
