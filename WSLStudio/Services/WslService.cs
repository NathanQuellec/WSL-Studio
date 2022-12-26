using System.Diagnostics;
using WSLStudio.Contracts.Services;
using WSLStudio.Helpers;
using Community.Wsl.Sdk;

namespace WSLStudio.Services;

public class WslService : IWslService
{
    private readonly WslApi _wslApi = new WslApi();

    public bool CheckWsl()
    {
        if (!_wslApi.IsWslSupported() || !_wslApi.IsInstalled)
            return false;

        return true
    }
}
// ERROR BUILD 