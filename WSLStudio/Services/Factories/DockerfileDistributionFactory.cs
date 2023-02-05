using Docker.DotNet.Models;
using Docker.DotNet;
using ICSharpCode.SharpZipLib.Tar;
using System.Diagnostics;
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Helpers;
using WSLStudio.Models;

namespace WSLStudio.Services.Factories;

public class DockerfileDistributionFactory : IDistributionFactory
{
    private const string DOCKER_NAMED_PIPE = "npipe://./pipe/docker_engine";

    private string _distroName;
    private string _tarLocation;
    private string _appPath;
    private string _imageTag;
    private string _containerId;

    private readonly DockerClient _dockerClient;

    public DockerfileDistributionFactory()
    {
        this._dockerClient = new DockerClientConfiguration(new Uri(uriString: DOCKER_NAMED_PIPE)).CreateClient();
    }

    public async Task<Distribution?> CreateDistribution(string distroName, double memoryLimit, int processorLimit, string resourceOrigin)
    {


        this._imageTag = $"wsl-studio-{distroName.ToLower()}";
        this._distroName = distroName;

        var distroTarFile = $"{distroName}.tar.gz";
        var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);


        this._appPath = Path.Combine(roamingPath, "WslStudio");

        if (!Directory.Exists(this._appPath))
        {
            Directory.CreateDirectory(this._appPath);
        }

        var tarFolder = Path.Combine(this._appPath, distroName);

        if (!Directory.Exists(tarFolder))
        {
            Directory.CreateDirectory(tarFolder);
        }

        this._tarLocation = Path.Combine(tarFolder, distroTarFile);

        try
        {
            await this.BuildDockerImage(resourceOrigin);
            await this.CreateDockerContainer();
            await this.ExportDockerContainer();
            await this.ImportDistribution();

            return new Distribution()
            {
                Name = distroName,
                MemoryLimit = memoryLimit,
                ProcessorLimit = processorLimit,
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

    private static Stream CreateTarballForDockerfileDirectory(string directory)
    {
        var tarball = new MemoryStream();
        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

        using var archive = new TarOutputStream(tarball)
        {
            //Prevent the TarOutputStream from closing the underlying memory stream when done
            IsStreamOwner = false
        };

        foreach (var file in files)
        {
            //Replacing slashes as KyleGobel suggested and removing leading /
            string tarName = file.Substring(directory.Length).Replace('\\', '/').TrimStart('/');

            //Let's create the entry header
            var entry = TarEntry.CreateTarEntry(tarName);
            using var fileStream = File.OpenRead(file);
            entry.Size = fileStream.Length;
            archive.PutNextEntry(entry);

            //Now write the bytes of data
            byte[] localBuffer = new byte[32 * 1024];
            while (true)
            {
                int numRead = fileStream.Read(localBuffer, 0, localBuffer.Length);
                if (numRead <= 0)
                    break;

                archive.Write(localBuffer, 0, numRead);
            }

            //Nothing more to do with this entry
            archive.CloseEntry();
        }
        archive.Close();

        //Reset the stream and return it, so it can be used by the caller
        tarball.Position = 0;
        return tarball;
    }


    private async Task BuildDockerImage(string workingDirectory)
    {
        try
        {

            await using var tarball = CreateTarballForDockerfileDirectory(workingDirectory);

            var imageBuildParameters = new ImageBuildParameters
            {
                Tags = new List<string> { this._imageTag },

            };

            var progress = new Progress<JSONMessage>();

            await this._dockerClient.Images.BuildImageFromDockerfileAsync(imageBuildParameters, tarball, null, null, progress);
        }
        catch (DockerApiException ex)
        {
            Console.WriteLine("[ERROR] Failed to build image, reason: " + ex.Message);
            throw;
        }

        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to build image, reason: " + ex.Message);
            throw;
        }
    }

    private async Task CreateDockerContainer()
    {
        try
        {

            var container = await this._dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = this._imageTag,
                Name = this._imageTag,
            });

            this._containerId = container.ID;

        }
        catch (DockerApiException ex)
        {
            Console.WriteLine("[ERROR] Failed to create container, reason: " + ex.Message);
            throw;
        }

        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to create container, reason: " + ex.Message);
            throw;
        }
    }

    private async Task ExportDockerContainer()
    {
        try
        {

            await using var exportStream = await this._dockerClient.Containers.ExportContainerAsync(this._imageTag);

            await using var fileStream = new FileStream(this._tarLocation, FileMode.Create);
            await exportStream.CopyToAsync(fileStream);

        }
        catch (DockerApiException ex)
        {
            Console.WriteLine("[ERROR] Failed to export container, reason: " + ex.Message);
            await this.RemoveImageAndContainer();
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to export container, reason: " + ex.Message);
            await this.RemoveImageAndContainer();
            throw;
        }

    }

    private async Task ImportDistribution()
    {
        try
        {

            var installDir = Path.Combine(this._appPath, this._distroName, "installDir");
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl --import {this._distroName} {installDir} {this._tarLocation}")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();

            process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to import distribution, reason: " + ex.Message);
            await this.RemoveImageAndContainer();
            throw;
        }
    }

    private async Task RemoveImageAndContainer()
    {
        try
        {
            await this._dockerClient.Images.DeleteImageAsync(this._imageTag, new ImageDeleteParameters()
            {
                Force = true,
            });

            await this._dockerClient.Containers.RemoveContainerAsync(this._containerId, new ContainerRemoveParameters()
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