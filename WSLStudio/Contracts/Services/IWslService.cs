using Windows.Services.Maps;
using WSLStudio.Helpers;

namespace WSLStudio.Contracts.Services;

public interface IWslService
{
    bool CheckWsl();
    bool CheckHypervisor();

    Task ImportDistribution(string distroName, string installDir, string tarLocation);
}