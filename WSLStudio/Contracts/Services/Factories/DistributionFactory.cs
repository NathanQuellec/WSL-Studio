using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Contracts.Services.Factories;

public abstract class DistributionFactory
{
    public abstract Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin, string targetFolder);

    public static Task ImportDistribution(string distroName, string installDir, string tarLocation)
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