using WSLStudio.Models;

namespace WSLStudio.Contracts.Services.Factories;

public interface IDistributionFactory
{
    Task<Distribution?> CreateDistribution(string distroName, double memoryLimit, int processorLimit, string resourceOrigin);
}