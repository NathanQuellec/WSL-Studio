using Docker.DotNet;
using Docker.DotNet.Models;
using WSLStudio.Contracts.Services;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class DockerHubDistributionFactory : DistributionFactory
{
    public async override Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin, string targetFolder)
    {
        var containerName = $"wsl-studio-{distroName.ToLower()}";
        var imageName = resourceOrigin;
        var distroTarFile = $"{distroName}.tar.gz";

        var tarLocation = Path.Combine(targetFolder, distroTarFile);
        var installDir = Path.Combine(targetFolder, "installDir");

        var docker = new DockerHelper();

        var imageTag = "latest";

        // check if user specify a tag in the image name input
        if (resourceOrigin.Contains(':'))
        {
            imageTag = resourceOrigin.Split(':').Last();
        }

        try
        {
            await docker.PullImageFromDockerHub(imageName, imageTag);
            var container = await docker.CreateDockerContainer(imageName, containerName);
            await docker.ExportDockerContainer(containerName, tarLocation);
            await ImportDistribution(distroName, installDir, tarLocation);
            RemoveDistributionArchive(tarLocation);
            await docker.RemoveDockerContainer(container!.ID);
            await docker.RemoveDockerImage(imageName);

            Console.WriteLine("[INFO] Distribution creation from Docker Hub succeed.");

            return new Distribution()
            {
                Name = distroName,
            };
        }
        catch (DockerApiException ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}