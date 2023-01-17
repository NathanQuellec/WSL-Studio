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

    private readonly DockerClient _dockerClient;

    public DockerfileDistributionFactory()
    { 
        this._dockerClient = new DockerClientConfiguration(new Uri(uriString: DOCKER_NAMED_PIPE)).CreateClient();
    }

    public async Task<Distribution> CreateDistribution(string distroName, double memoryLimit, int processorLimit, string resourceOrigin)
    {
        this._distroName = distroName;
        this._imageTag = $"wsl-studio-{distroName.ToLower()}";

       
        var distroTarFile = $"{distroName}.tar.gz";

        var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        

        this._appPath = Path.Combine(roamingPath, "WslStudio");

        if (!Directory.Exists(this._appPath))
        {
            Directory.CreateDirectory(this._appPath);
        }

        this._tarLocation = Path.Combine(this._appPath, distroTarFile);

        await BuildDockerImage(resourceOrigin);
        await CreateDockerContainer();
        await ExportDockerContainer();
        await ImportDistribution();

        return new Distribution()
        {
            Name = distroName,
            MemoryLimit = memoryLimit,
            ProcessorLimit = processorLimit,
        };
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
            //C# 8.0 using syntax
            var progress = new Progress<JSONMessage>();

            await this._dockerClient.Images.BuildImageFromDockerfileAsync(imageBuildParameters, tarball, null, null, progress);

            /*var filter = new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "reference", 
                        new Dictionary<string, bool>
                        {
                            {
                                this._imageTag, 
                                true
                            }
                        }
                    }
                }
            };
            var images = await this._dockerClient.Images.ListImagesAsync(filter);

            while (true)
            {
                if(images.ToString().Equals(this._imageTag))
                {
                    break;
                }
                images = await this._dockerClient.Images.ListImagesAsync(filter);
            }*/
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[ERROR] BuildDockerImage failed");
        }
    }

    private async Task CreateDockerContainer()
    {
        try
        {

           await this._dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = this._imageTag,
                Name = this._imageTag,
            });

        }
        catch (Exception ex)
        {
            Debug.WriteLine("[ERROR]CreateDockerContainer failed");
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
            Console.WriteLine("Error: Failed to export container, reason: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: Failed to export container, reason: " + ex.Message);
        }

    }

    private async Task ImportDistribution()
    {
        try
        {

            var installDir = Path.Combine(this._appPath, "installDir");
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
            Console.WriteLine("Error: Failed to import distribution, reason: " + ex.Message);
        }

    }
}