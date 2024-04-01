using Community.Wsl.Sdk;
using Serilog;
using WSLStudio.Exceptions;

namespace WSLStudio.Helpers;

public static class WslHelper
{
    private static readonly WslApi _wslApi = new();

    public static bool CheckWsl()
    {
        if (!_wslApi.IsWslSupported() || !_wslApi.IsInstalled)
            return false;

        return true;
    }

    public static bool CheckHypervisor()
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

    public static async Task ExportDistribution(string distroName, string destPath)
    {
        var processBuilder = new ProcessBuilderHelper("cmd.exe")
            .SetArguments(
                $"/c wsl --export {distroName} {destPath}")
            .SetRedirectStandardOutput(true)
            .SetUseShellExecute(false)
            .SetCreateNoWindow(true)
            .Build();
        processBuilder.Start();

        await processBuilder.WaitForExitAsync();
    }

    public static async Task ImportDistribution(string distroName, string installDir, string tarLocation)
    {
        Log.Information("Importing distribution ...");
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl --import {distroName} {installDir} {tarLocation}")
                .SetRedirectStandardOutput(true)
                .SetRedirectStandardError(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();

            process.Start();
            await process.WaitForExitAsync();

            var isDistroImported = await CheckExistingDistribution(distroName);

            if (!isDistroImported)
            {
                throw new ImportDistributionException("Failed to import distribution");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to import distribution - Caused by exception {ex}");
            throw;
        }
    }

    public static async Task<bool> CheckRunningDistribution(string distroName)
    {
        Log.Information($"Check running distribution for {distroName}");
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments("/c wsl --list --running --quiet")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();

            var output = process.StandardOutput.ReadToEndAsync().GetAwaiter().GetResult();
            await process.WaitForExitAsync();
            var sanitizedOutput = output.Replace("\0", "").Replace("\r", "");  // remove special character
            var runningDistros = sanitizedOutput.Split("\n");

            return runningDistros.Contains(distroName);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start process to check running distribution - Caused by exception : {ex}");
            return false;
        }
    }

    public static async Task<bool> CheckExistingDistribution(string distroName)
    {
        Log.Information($"Check existing distribution for {distroName}");
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments("/c wsl --list --quiet")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();

            var output = process.StandardOutput.ReadToEndAsync().GetAwaiter().GetResult();
            await process.WaitForExitAsync();
            var sanitizedOutput = output.Replace("\0", "").Replace("\r", "");  // remove special character
            var existingDistros = sanitizedOutput.Split("\n");

            return existingDistros.Contains(distroName);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start process to check existing distribution - Caused by exception : {ex}");
            return false;
        }
    }
}
