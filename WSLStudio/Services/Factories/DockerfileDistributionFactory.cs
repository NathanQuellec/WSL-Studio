using Docker.DotNet;
using Serilog;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class DockerfileDistributionFactory : AbstractDistributionFactory
{
    public async override Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin, string targetFolder)
    {
        Log.Information("Creating distribution from Dockerfile ...");

        var containerName = $"wsl-studio-{distroName.ToLower()}";
        var imageName = containerName;
        var distroTarFile = $"{distroName}.tar.gz";

        var tarLocation = Path.Combine(App.TmpDirPath, distroTarFile);
        var installDir = Path.Combine(targetFolder, "installDir");

        var docker = new DockerHelper();

        try
        {

            if (!Directory.Exists(resourceOrigin))
            {
                throw new DirectoryNotFoundException();
            }

            await docker.BuildDockerImage(resourceOrigin, imageName);
            var container = await docker.CreateDockerContainer(imageName, containerName);
            await docker.ExportDockerContainer(containerName, tarLocation);
            await WslHelper.ImportDistribution(distroName, installDir, tarLocation);
            File.Delete(tarLocation);
            await docker.RemoveDockerContainer(container!.ID);
            await docker.RemoveDockerImage(imageName);

            Log.Information("Distribution creation from Dockerfile succeed.");

            return new DistributionBuilder()
                .WithName(distroName)
                .Build();
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