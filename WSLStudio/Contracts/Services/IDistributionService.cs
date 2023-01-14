using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionService
{
    void InitDistributionsList();
    IEnumerable<Distribution> GetAllDistributions();
    void CreateDistribution();
    void RemoveDistribution(Distribution? distribution);
    void RenameDistribution(Distribution? distribution);
    void LaunchDistribution(Distribution? distribution);
    void StopDistribution(Distribution? distribution);
    void OpenDistributionFileSystem(Distribution? distribution);
}
