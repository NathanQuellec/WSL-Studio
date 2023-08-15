using Windows.Services.Maps;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface IWslService
{
    bool CheckWsl();
    bool CheckHypervisor();
    void ExportDistribution(string distroName, string destPath);
}