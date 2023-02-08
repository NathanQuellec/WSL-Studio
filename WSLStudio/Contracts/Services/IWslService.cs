using Windows.Services.Maps;
using WSLStudio.Helpers;

namespace WSLStudio.Contracts.Services;

public interface IWslService
{
    bool CheckWsl();
    bool CheckHypervisor();
}