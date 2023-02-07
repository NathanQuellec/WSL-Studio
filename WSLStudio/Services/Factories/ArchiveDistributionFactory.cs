using Docker.DotNet;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class ArchiveDistributionFactory : IDistributionFactory
{
    private const string APP_FOLDER = "WslStudio";

    public Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin)
    {
        var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var appPath = Path.Combine(roamingPath, APP_FOLDER);

        if (!Directory.Exists(appPath))
        {
            Directory.CreateDirectory(appPath);
        }

        var distroFolder = Path.Combine(appPath, distroName);

        if (!Directory.Exists(distroFolder))
        {
            Directory.CreateDirectory(distroFolder);
        }

        try
        {
            ImportDistribution(distroName, appPath, resourceOrigin);

            return Task.FromResult(new Distribution()
            {
                Name = distroName,
            })!;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return Task.FromResult<Distribution?>(null);
        }
    }

    private static void ImportDistribution(string distroName, string appPath, string archivePath)
    {
        try
        {

            var installDir = Path.Combine(appPath, distroName, "installDir");
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl --import {distroName} {installDir} {archivePath}")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();

            process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to import distribution, reason: " + ex.Message);
            throw;
        }
    }

}