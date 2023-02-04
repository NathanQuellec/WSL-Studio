using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class ArchiveDistributionFactory : IDistributionFactory
{
    public async Task<Distribution> CreateDistribution(string distroName, double memoryLimit, int processorLimit, string resourceOrigin) => null;
}