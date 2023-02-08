using Docker.DotNet;
using Docker.DotNet.Models;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class DockerHubDistributionFactory : IDistributionFactory
{

    private readonly DockerClient _dockerClient;

    private const string DOCKER_NAMED_PIPE = "npipe://./pipe/docker_engine";
    private const string APP_FOLDER = "WslStudio";

    private string _imageTag;
    private string _containerTag;
    private string _containerId;

    public DockerHubDistributionFactory()
    {
  
        this._dockerClient = new DockerClientConfiguration(new Uri(uriString: DOCKER_NAMED_PIPE)).CreateClient();
    }

    public async Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin)
    {

        this._imageTag = resourceOrigin;
        this._containerTag = $"wsl-studio-{distroName.ToLower()}";

        var distroTarFile = $"{distroName}.tar.gz";
        var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appPath = Path.Combine(roamingPath, APP_FOLDER);

        if (!Directory.Exists(appPath))
        {
            Directory.CreateDirectory(appPath);
        }

        var distroFolder = Path.Combine(appPath, distroName);

        if (!Directory.Exists(distroFolder))
        {
            Directory.CreateDirectory(distroFolder);
        }

        var tarLocation = Path.Combine(distroFolder, distroTarFile);

        try
        {
            Console.WriteLine("[INFO] Distribution creation from Docker Hub succeed.");

            await PullImageFromDockerHub(resourceOrigin);
            await CreateDockerContainer();
            await ExportDockerContainer(tarLocation);
            await ImportDistribution(distroName, appPath, tarLocation);

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

    private async Task PullImageFromDockerHub(string image)
    {
        try
        {

            var imageCreateParameters = new ImagesCreateParameters()
            {
                FromImage = _imageTag,
                Tag = "latest"
            };

            var progress = new Progress<JSONMessage>();

            await this._dockerClient.Images.CreateImageAsync(imageCreateParameters, null, progress);
        }
        catch (DockerApiException ex)
        {
            Console.WriteLine("[ERROR] Failed to pull image, reason: " + ex.Message);
            throw;
        }

        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to pull image, reason: " + ex.Message);
            throw;
        }
    }

    private async Task CreateDockerContainer()
    {
        try
        {

            var container = await this._dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = _imageTag,
                Name = _containerTag
            });

            _containerId = container.ID;

        }
        catch (DockerApiException ex)
        {
            Console.WriteLine("[ERROR] Failed to create container, reason: " + ex.Message);
            await RemoveDockerImage();
            throw;
        }

        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to create container, reason: " + ex.Message);
            await RemoveDockerImage();
            throw;
        }
    }

    private async Task ExportDockerContainer(string tarLocation)
    {
        try
        {

            await using var exportStream = await _dockerClient.Containers.ExportContainerAsync(this._containerTag);

            await using var fileStream = new FileStream(tarLocation, FileMode.Create);
            await exportStream.CopyToAsync(fileStream);

        }
        catch (DockerApiException ex)
        {
            Console.WriteLine("[ERROR] Failed to export container, reason: " + ex.Message);
            await RemoveDockerImage();
            await RemoveDockerContainer();
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to export container, reason: " + ex.Message);
            await RemoveDockerImage();
            await RemoveDockerContainer();
            throw;
        }

    }

    private async Task ImportDistribution(string distroName, string appPath, string tarLocation)
    {
        try
        {

            var installDir = Path.Combine(appPath, distroName, "installDir");
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl --import {distroName} {installDir} {tarLocation}")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();

            process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to import distribution, reason: " + ex.Message);
            await this.RemoveDockerImage();
            await this.RemoveDockerContainer();
            throw;
        }
    }

    private async Task RemoveDockerImage()
    {
        try
        {
            await _dockerClient.Images.DeleteImageAsync(_imageTag, new ImageDeleteParameters()
            {
                Force = true,
            });

            await _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters()
            {
                Force = true,
            });
        }
        catch (DockerApiException ex)
        {
            Console.WriteLine(ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task RemoveDockerContainer()
    {
        try
        {
            await this._dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters()
            {
                Force = true,
            });
        }
        catch (DockerApiException ex)
        {
            Console.WriteLine(ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}