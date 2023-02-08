﻿using Docker.DotNet.Models;
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
    private const string APP_FOLDER = "WslStudio";

    private string _imageTag;
    private string _containerId;

    private readonly DockerClient _dockerClient;

    public DockerfileDistributionFactory()
    {
        this._imageTag = "";
        this._containerId = "";
        this._dockerClient = new DockerClientConfiguration(new Uri(uriString: DOCKER_NAMED_PIPE)).CreateClient();
    }

    public async Task<Distribution?> CreateDistribution(string distroName, string resourceOrigin)
    {


        this._imageTag = $"wsl-studio-{distroName.ToLower()}";

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
            await this.BuildDockerImage(resourceOrigin);
            await this.CreateDockerContainer();
            await this.ExportDockerContainer(tarLocation);
            await this.ImportDistribution(distroName, appPath, tarLocation);
            await this.RemoveDockerImage();
            await this.RemoveDockerContainer();

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
            await this.RemoveDockerImage();
            throw;
        }

        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to create container, reason: " + ex.Message);
            await this.RemoveDockerImage();
            throw;
        }
    }

    private async Task ExportDockerContainer(string tarLocation)
    {
        try
        {

            await using var exportStream = await this._dockerClient.Containers.ExportContainerAsync(this._imageTag);

            await using var fileStream = new FileStream(tarLocation, FileMode.Create);
            await exportStream.CopyToAsync(fileStream);

        }
        catch (DockerApiException ex)
        {
            Console.WriteLine("[ERROR] Failed to export container, reason: " + ex.Message);
            await this.RemoveDockerImage();
            await this.RemoveDockerContainer();
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to export container, reason: " + ex.Message);
            await this.RemoveDockerImage();
            await this.RemoveDockerContainer();
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

    private async Task RemoveDockerContainer()
    {
        try
        {
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