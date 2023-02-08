using Docker.DotNet;
using WSLStudio.Contracts.Services;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class ArchiveDistributionFactory : IDistributionFactory
{
    private readonly IWslService _wslService = new WslService();

    public async Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin, string targetFolder)
    {
        var installDir = Path.Combine(targetFolder, "installDir");

        try
        {
           await this._wslService.ImportDistribution(distroName, installDir, resourceOrigin);

           Console.WriteLine("[INFO] Distribution creation from Archive file succeed.");

            return new Distribution()
            {
                Name = distroName,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }
}