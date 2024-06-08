using Serilog;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class VhdxDistributionFactory : AbstractDistributionFactory
{
    public async override Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin,
        string targetFolder)
    {
        Log.Information("Creating distribution from vhdx image file ...");
        var installDir = Path.Combine(targetFolder, "installDir");

        try
        {
            if (!File.Exists(resourceOrigin))
            {
                throw new FileNotFoundException();
            }

            Directory.CreateDirectory(installDir);
            var vhdxDestPath = Path.Combine(installDir, "ext4.vhdx");
            File.Copy(resourceOrigin, vhdxDestPath);
            await FilesHelper.ExtractGzFile(resourceOrigin, vhdxDestPath);

            await WslHelper.ImportInPlaceDistribution(distroName, installDir, vhdxDestPath);

            Log.Information("Distribution creation from vhdx image file succeed.");

            return new DistributionBuilder()
                .WithName(distroName)
                .Build();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create distribution from vhdx image file - Caused by exception : {ex}");
            Directory.Delete(installDir, true );
            throw;
        }
    }
}