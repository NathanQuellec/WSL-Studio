using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionService
{
    void InitDistributionsList();
    IEnumerable<Distribution> GetAllDistributions();
    Task<Distribution?> CreateDistribution(string distroName, double memoryLimit, int processorLimit, string resourceOrigin);
    void RemoveDistribution(Distribution distribution);
    bool RenameDistribution(Distribution distribution, string newDistroName);
    void LaunchDistribution(Distribution distribution);
    void StopDistribution(Distribution distribution);
    void OpenDistributionFileSystem(Distribution distribution);
}
