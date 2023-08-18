using Windows.Services.Maps;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IWslService
{
    bool CheckWsl();
    bool CheckHypervisor();
    Task ExportDistribution(string distroName, string destPath);
}