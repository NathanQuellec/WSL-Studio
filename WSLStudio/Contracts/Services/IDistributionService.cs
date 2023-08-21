﻿using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionService
{
    Task InitDistributionsList();
    IEnumerable<Distribution> GetAllDistributions();
    Task<Distribution?> CreateDistribution(string distroName, string creationMode, string resourceOrigin);
    void RemoveDistribution(Distribution distribution);
    bool RenameDistribution(Distribution distribution, string newDistroName);
    void LaunchDistribution(Distribution distribution);
    void StopDistribution(Distribution distribution);
    void OpenDistributionFileSystem(Distribution distribution);
    void OpenDistributionWithVsCode(Distribution distribution);
    void OpenDistroWithWinTerm(Distribution distribution);
}
