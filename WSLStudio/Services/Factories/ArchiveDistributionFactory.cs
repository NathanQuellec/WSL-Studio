using Serilog;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class ArchiveDistributionFactory : DistributionFactory
{
    public async override Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin,
        string targetFolder)
    {
        Log.Information("Creating distribution from archive file ...");
        var installDir = Path.Combine(targetFolder, "installDir");

        try
        {
            if (!File.Exists(resourceOrigin))
            {
                throw new FileNotFoundException();
            }

            await WslHelper.ImportDistribution(distroName, installDir, resourceOrigin);

            Log.Information("Distribution creation from archive file succeed.");

            return new DistributionBuilder()
                .WithName(distroName)
                .Build();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create distribution from archive file - Caused by exception : {ex}");
            throw;
        }
    }
}