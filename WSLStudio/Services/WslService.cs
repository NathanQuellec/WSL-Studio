using System.Diagnostics;
using System.Management;
using Community.Wsl.Sdk;
using Microsoft.Win32;
using WSLStudio.Contracts.Services;
using WSLStudio.Helpers;
using WSLStudio.Models;

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

    public bool CheckHypervisor()
    {
        var processBuilder = new ProcessBuilderHelper("powershell.exe")
            .SetArguments(
                "/c (Get-WmiObject -Class \"Win32_ComputerSystem\" -ComputerName \"localhost\").HypervisorPresent")
            .SetUseShellExecute(false)
            .SetRedirectStandardOutput(true)
            .SetRedirectStandardError(true)
            .SetCreateNoWindow(true)
            .Build();
        processBuilder.Start();
        var output = processBuilder.StandardOutput.ReadToEnd();
        var virtualizationEnabled = bool.Parse(output);
        
        return virtualizationEnabled;
    }
}