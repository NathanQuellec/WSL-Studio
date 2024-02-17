using Serilog;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Contracts.Services.Factories;

public abstract class DistributionFactory
{
    public abstract Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin, string targetFolder);

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
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to import distribution - Caused by exception {ex}");
        } 
    }

    public static void RemoveDistributionArchive(string tarLocation)
    {
        if (File.Exists(tarLocation))
        {
            File.Delete(tarLocation);
            Log.Information("Temporary archive has been successfully deleted");
        }
    }
}