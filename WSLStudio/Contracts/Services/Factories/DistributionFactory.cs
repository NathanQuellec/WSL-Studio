using WSLStudio.Models;

namespace WSLStudio.Contracts.Services.Factories;

public abstract class DistributionFactory
{
    public abstract Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin, string targetFolder);

}