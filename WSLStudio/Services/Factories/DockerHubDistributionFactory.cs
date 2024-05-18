using Serilog;
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
        var installDir = Path.Combine(targetFolder, "installDir");

        // check if we used any docker official images and add library prefix 
        if (!resourceOrigin.Contains('/'))
        {
            imageName = string.Concat("library/", imageName);
        }

        // check if user specify a tag in the image name input
        if (resourceOrigin.Contains(':'))
        {
            var imageElements = resourceOrigin.Split(':');
            imageName = imageElements.First();
            imageTag = imageElements.Last();
        }

        try
        {
            var imageToken = await DockerHelper.GetAuthToken(imageName);
            var imageManifest = await DockerHelper.GetImageManifest(imageToken, imageName, imageTag);

            if (imageManifest?.GetLayers() == null)
            {
                throw new Exception("Unable to find this image on DockerHub");
            }

            var imageLayers = await DockerHelper.GetLayers(imageToken, imageManifest, imageName);

            var tarPathList = new List<string>();
            foreach (var layer in imageLayers)
            {
                var tarFilePath = await ArchiveHelper.DecompressArchive(layer);
                tarPathList.Add(tarFilePath);
            }

            var newArchPath = Path.Combine(App.TmpDirPath, "distro.tar");
            await ArchiveHelper.MergeArchive(tarPathList, newArchPath);

            await WslHelper.ImportDistribution(distroName, installDir, newArchPath);
            FilesHelper.RemoveDirContent(App.TmpDirPath);

            Log.Information("Distribution creation from DockerHub succeed.");

            return new DistributionBuilder()
               .WithName(distroName)
               .Build();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create distribution from DockerHub - Caused by exception : {ex}");
            FilesHelper.RemoveDirContent(App.TmpDirPath);
            throw;
        }
    }
}