using System.Diagnostics;
using System.Management;
using Community.Wsl.Sdk;
using WSLStudio.Contracts.Services;

namespace WSLStudio.Services;

public class WslService : IWslService
{
    private readonly WslApi _wslApi = new();

    public bool CheckWsl()
    {
        if (!_wslApi.IsWslSupported() || !_wslApi.IsInstalled)
            return false;

        return true;
    }

    public bool CheckProcessorVirtualization()
    {
        var managClass = new ManagementClass("win32_processor");
        var managInstances = managClass.GetInstances();

        foreach (var managObj in managInstances)
        {
            foreach (var prop in managObj.Properties)
            {
                if (prop.Name == "VirtualizationFirmwareEnabled" && prop.Value is false )
                {
                    Debug.WriteLine("[ERROR] Cannot run WSL - Property Name: {0} as Value: {1}", prop.Name, prop.Value);
                    return false;
                }
            }
        }
        return true;
    }
}