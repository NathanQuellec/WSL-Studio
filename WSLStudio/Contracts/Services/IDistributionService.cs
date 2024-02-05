using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionService
{
    void InitDistributionsList();
    IEnumerable<Distribution> GetAllDistributions();
    Task<Distribution?> CreateDistribution(string distroName, string creationMode, string resourceOrigin);
    Task RemoveDistribution(Distribution distribution);
    Task<bool> RenameDistribution(Distribution distribution, string newDistroName);
    void LaunchDistribution(Distribution distribution);
    void StopDistribution(Distribution distribution);
    void OpenDistributionFileSystem(Distribution distribution);
    void OpenDistributionWithVsCode(Distribution distribution);
    void OpenDistroWithWinTerm(Distribution distribution);
}
