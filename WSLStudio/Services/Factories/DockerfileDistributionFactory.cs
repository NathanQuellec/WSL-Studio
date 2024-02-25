using Docker.DotNet.Models;
using Docker.DotNet;
using ICSharpCode.SharpZipLib.Tar;
using System.Diagnostics;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using Serilog;

namespace WSLStudio.Services.Factories;

public class DockerfileDistributionFactory : DistributionFactory
{
    public async override Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin, string targetFolder)
    {
        Log.Information("Creating distribution from Dockerfile ...");

        var containerName = $"wsl-studio-{distroName.ToLower()}";
        var imageName = containerName;
        var distroTarFile = $"{distroName}.tar.gz";

        var tarLocation = Path.Combine(targetFolder, distroTarFile);
        var installDir = Path.Combine(targetFolder, "installDir");

        var docker = new DockerHelper();

        try
        {

            await docker.BuildDockerImage(resourceOrigin, imageName);
            var container = await docker.CreateDockerContainer(imageName, containerName);
            await docker.ExportDockerContainer(containerName, tarLocation);
            await WslHelper.ImportDistribution(distroName, installDir, tarLocation);
            File.Delete(tarLocation);
            await docker.RemoveDockerContainer(container!.ID);
            await docker.RemoveDockerImage(imageName);

            Log.Information("Distribution creation from Dockerfile succeed.");

            return new Distribution()
            {
                Name = distroName,
                
            };
        }
        catch (DockerApiException ex)
        {
            Log.Error($"Failed to connect to Docker API - Caused by exception : {ex}");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create distribution from Dockerfile - Caused by exception : {ex}");
            throw;
        }
    }
}