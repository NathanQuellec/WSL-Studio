using System.Diagnostics;
using System.Management;
using Community.Wsl.Sdk;
using WSLStudio.Contracts.Services;
using WSLStudio.Helpers;

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

    public Task ImportDistribution(string distroName, string installDir, string tarLocation)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl --import {distroName} {installDir} {tarLocation}")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();

            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to import distribution, reason: " + ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }
}