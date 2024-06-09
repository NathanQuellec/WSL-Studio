using System.ComponentModel;
using Community.Wsl.Sdk;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;
using WSLStudio.Exceptions;
using WSLStudio.Messages;

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
        var process = new ProcessBuilder("powershell.exe")
            .SetArguments(
                "/c (Get-WmiObject -Class \"Win32_ComputerSystem\" -ComputerName \"localhost\").HypervisorPresent")
            .SetUseShellExecute(false)
            .SetRedirectStandardOutput(true)
            .SetRedirectStandardError(true)
            .SetCreateNoWindow(true)
            .Build();
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var virtualizationEnabled = bool.Parse(output);

        return virtualizationEnabled;
    }
    /**
     * Check if WSL is installed from the microsoft store
     */

    public static async Task<bool> CheckWslMicrosoftStore()
    {
        var process = new ProcessBuilder("powershell.exe")
            .SetArguments(
                "/c  winget ls  -q 'Windows Subsystem for Linux'")
            .SetUseShellExecute(false)
            .SetRedirectStandardOutput(true)
            .SetRedirectStandardError(true)
            .SetCreateNoWindow(true)
            .Build();
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();

        return output.Contains("Linux");
    }

    public static async void InstallWslFromMicrosoftStore()
    {
        var process = new ProcessBuilder("powershell.exe")
            .SetArguments(
                "/c  winget install 'Windows Subsystem for Linux'")
            .SetUseShellExecute(true)
            .SetVerb("runas")
            .Build();
        process.Start();
        await process.WaitForExitAsync();
    }

    /**
     * Used to create snapshots by exporting the file system to an archive file
     */
    public static async Task ExportDistribution(string distroName, string destPath)
    {
        var process = new ProcessBuilder("cmd.exe")
            .SetArguments(
                $"/c wsl --export {distroName} {destPath}")
            .SetRedirectStandardOutput(true)
            .SetUseShellExecute(false)
            .SetCreateNoWindow(true)
            .Build();
        process.Start();

        await process.WaitForExitAsync();
    }

    public static async Task ImportDistribution(string distroName, string installDir, string tarLocation)
    {
        
        Log.Information("Importing distribution ...");
        WeakReferenceMessenger.Default.Send(new DistroProgressBarMessage("Importing your distribution ..."));
        try
        {
            var process = new ProcessBuilder("cmd.exe")
                .SetArguments($"/c md {installDir} & wsl --import {distroName} {installDir} {tarLocation}")
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
    /**
     * Import distribution from vhdx based snapshot 
     */
    public static async Task ImportInPlaceDistribution(string distroName, string installDir, string vhdxFilePath)
    {
        
        Log.Information("Importing distribution ...");
        WeakReferenceMessenger.Default.Send(new DistroProgressBarMessage("Importing your distribution ..."));
        try
        {
            var process = new ProcessBuilder("cmd.exe")
                .SetArguments($"/c md {installDir} & wsl --import-in-place {distroName} {vhdxFilePath}")
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
            var process = new ProcessBuilder("cmd.exe")
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
            var process = new ProcessBuilder("cmd.exe")
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

    public static async Task TerminateDistribution(string distroName)
    {
        Log.Information($"Terminating distribution {distroName} ...");

        try
        {
            var process = new ProcessBuilder("cmd.exe")
                .SetArguments($"/c wsl --terminate {distroName}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start process to terminate distribution - Caused by exception {ex}");
        }
    }

    public static async Task ShutdownWsl()
    {
        Log.Information($"Shutdown WSL ...");

        try
        {
            var process = new ProcessBuilder("cmd.exe")
                .SetArguments($"/c wsl --shutdown")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            await process.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to shutdown WSL - Caused by exception {ex}");
        }
    }
}
