using Docker.DotNet.Models;
using Docker.DotNet;
using ICSharpCode.SharpZipLib.Tar;
using System.Diagnostics;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;

namespace WSLStudio.Services.Factories;

public class DockerfileDistributionFactory : DistributionFactory
{
    public async override Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin, string targetFolder)
    {
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
            await ImportDistribution(distroName, installDir, tarLocation);
            await docker.RemoveDockerContainer(container!.ID);
            await docker.RemoveDockerImage(imageName);

            Console.WriteLine("[INFO] Distribution creation from Dockerfile succeed.");

            return new Distribution()
            {
                Name = distroName,
                
            };
        }
        catch (DockerApiException ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }
}