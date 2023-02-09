﻿using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IDistributionService
{
    void InitDistributionsList();
    IEnumerable<Distribution> GetAllDistributions();
    Task<Distribution?> CreateDistribution(string creationMode, string distroName, string resourceOrigin);
    void RemoveDistribution(Distribution distribution);
    bool RenameDistribution(Distribution distribution, string newDistroName);
    void LaunchDistribution(Distribution distribution);
    void StopDistribution(Distribution distribution);
    void OpenDistributionFileSystem(Distribution distribution);
    void OpenDistributionWithVsCode(Distribution distribution);
}
