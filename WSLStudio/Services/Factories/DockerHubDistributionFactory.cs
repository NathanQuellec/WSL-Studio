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
        var imageName = resourceOrigin;
        var imageTag = "latest"; // default tag

        var distroTarFile = $"{distroName}.tar.gz";

        var installDir = Path.Combine(targetFolder, "installDir");


        // check if user specify a tag in the image name input
        if (resourceOrigin.Contains(':'))
        {
            var imageElements = resourceOrigin.Split(':');
            imageName = imageElements.First();
            imageTag = imageElements.Last();
        }

        try
        {
            /*await docker.PullImageFromDockerHub(imageName, imageTag);
            var container = await docker.CreateDockerContainer(imageName, containerName);
            await docker.ExportDockerContainer(containerName, tarLocation);
            await ImportDistribution(distroName, installDir, tarLocation);
            RemoveDistributionArchive(tarLocation);
            await docker.RemoveDockerContainer(container!.ID);
            await docker.RemoveDockerImage(imageName);*/
            var imageToken = await DockerHelper.GetAuthToken(imageName);
            var imageManifest = await DockerHelper.GetImageManifest(imageToken, imageName, imageTag);
            var imageLayers = await DockerHelper.GetLayers(imageToken, imageManifest, imageName);

            var tarPathList = new List<string>();
            foreach (var layer in imageLayers)
            {
                var tarFilePath = await ArchiveHelper.DecompressArchive(layer);
                tarPathList.Add(tarFilePath);
            }

            var newArchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WslStudio", "distro.tar");
            File.Delete(newArchPath);
            await ArchiveHelper.MergeArchive(tarPathList, newArchPath);

            await ImportDistribution(distroName, installDir, newArchPath);

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