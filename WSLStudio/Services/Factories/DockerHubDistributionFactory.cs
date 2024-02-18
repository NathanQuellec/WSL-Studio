using Docker.DotNet;
using Docker.DotNet.Models;
using Serilog;
using WSLStudio.Contracts.Services;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class DockerHubDistributionFactory : DistributionFactory
{
    public async override Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin, string targetFolder)
    {
        Log.Information("Creating distribution from DockerHub ...");

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

            if (imageManifest.Layers == null)
            {
                throw new Exception("Didnt't find layers for this image on DockerHub");
            }

            var imageLayers = await DockerHelper.GetLayers(imageToken, imageManifest, imageName);

            var tarPathList = new List<string>();
            foreach (var layer in imageLayers)
            {
                var tarFilePath = await ArchiveHelper.DecompressArchive(layer);
                tarPathList.Add(tarFilePath);
            }

            var newArchPath = Path.Combine(App.TmpDirPath,"distro.tar");
            await ArchiveHelper.MergeArchive(tarPathList, newArchPath);

            await ImportDistribution(distroName, installDir, newArchPath);
            FilesHelper.RemoveDirContent(App.TmpDirPath);

            Log.Information("Distribution creation from DockerHub succeed.");

            return new Distribution()
            {
                Name = distroName,
            };
        }
        catch (DockerApiException ex)
        {
            Log.Error($"Failed to connect to Docker API - Caused by exception : {ex}");
            FilesHelper.RemoveDirContent(App.TmpDirPath);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create distribution from DockerHub - Caused by exception : {ex}");
            FilesHelper.RemoveDirContent(App.TmpDirPath);
            throw;
        }
    }
}