using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using WSLStudio.Models.Docker;
using System;
using System.Reflection.Emit;
using TarArchive = SharpCompress.Archives.Tar.TarArchive;
using TarArchiveEntry = SharpCompress.Archives.Tar.TarArchiveEntry;

using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Common.Tar;
using SharpCompress.Common.Rar;

namespace WSLStudio.Helpers;

public class DockerHelper
{

    private readonly DockerClient _dockerClient;

    private const string DOCKER_NAMED_PIPE = "npipe://./pipe/docker_engine";
    private const string DOCKER_REGISTRY = "https://registry.hub.docker.com/v2";
   // private static readonly string DockerAuthToken = "auth.docker.io";

    public DockerHelper()
    {
        _dockerClient = new DockerClientConfiguration(new Uri(uriString: DOCKER_NAMED_PIPE)).CreateClient();
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
            string tarName = file.Substring(directory.Length).Replace('\\', '/').TrimStart('/');

            //Let's create the entry header
            var entry = ICSharpCode.SharpZipLib.Tar.TarEntry.CreateTarEntry(tarName);
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


    public async Task BuildDockerImage(string workingDirectory, string imageName)
    {
        try
        {

            await using var tarball = CreateTarballForDockerfileDirectory(workingDirectory);

            var imageBuildParameters = new ImageBuildParameters
            {
                Tags = new List<string> { imageName },

            };

            var progress = new Progress<JSONMessage>();

            await _dockerClient.Images.BuildImageFromDockerfileAsync(imageBuildParameters, tarball, null, null, progress);
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

    public async Task PullImageFromDockerHub(string imageName, string imageTag)
    {
        try
        {

            var imageCreateParameters = new ImagesCreateParameters()
            {
                FromImage = imageName,
                Tag = imageTag
            };

            var progress = new Progress<JSONMessage>();

            await _dockerClient.Images.CreateImageAsync(imageCreateParameters, null, progress);
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

    public async Task<CreateContainerResponse?> CreateDockerContainer(string imageName, string containerName)
    {
        try
        {

            return await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = imageName,
                Name = containerName,
            });

        }
        catch (DockerApiException ex)
        {
            await RemoveDockerImage(imageName);
            Console.WriteLine("[ERROR] Failed to create container, reason: " + ex.Message);
            throw;
        }

        catch (Exception ex)
        {
            await RemoveDockerImage(imageName);
            Console.WriteLine("[ERROR] Failed to create container, reason: " + ex.Message);
            throw;
        }
    }

    public async Task ExportDockerContainer(string containerName, string targetPath)
    {
        try
        {

            await using var exportStream = await _dockerClient.Containers.ExportContainerAsync(containerName);

            await using var fileStream = new FileStream(targetPath, FileMode.Create);
            await exportStream.CopyToAsync(fileStream);

        }
        catch (DockerApiException ex)
        {
            Console.WriteLine("[ERROR] Failed to export container, reason: " + ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to export container, reason: " + ex.Message);
            throw;
        }

    }

    public async Task RemoveDockerImage(string imageName)
    {
        try
        {
            await _dockerClient.Images.DeleteImageAsync(imageName, new ImageDeleteParameters()
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

    public async Task RemoveDockerContainer(string containerId)
    {
        try
        {
            await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters()
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

    public static async Task<AuthToken?> GetAuthToken(string image)
    {
        var uriString =
            $@"https://auth.docker.io/token?service=registry.docker.io&scope=repository:{image}:pull";

        var uri = new Uri(uriString);
        using var httpClient = new HttpClient();

        try
        {
            using var httpResponse = await httpClient.GetAsync(uri);
            using var content = httpResponse.Content;
            var authToken = content.ReadFromJsonAsync<AuthToken>().Result;

            return authToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to download image auth token : " + ex);
            return null;
        }
        
    }

    public static async Task<ImageManifest?> GetImageManifest(AuthToken authToken, string image, string tag)
    {
        var uriString = $@"{DOCKER_REGISTRY}/{image}/manifests/{tag}";
        var uri = new Uri(uriString);
        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.distribution.manifest.v2+json"));

        try
        {
            using var httpResponse = await httpClient.GetAsync(uri);
            using var content = httpResponse.Content;
            var imageManifest = content.ReadFromJsonAsync<ImageManifest>().Result;

            return imageManifest;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to download image manifest : " + ex);
            return null;
        }
    }

    public static async Task<List<string>?> GetLayers(AuthToken authToken, ImageManifest imageManifest, string image)
    {
        try
        {
            var layers = imageManifest.Layers;
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token);
            httpClient.Timeout = TimeSpan.FromSeconds(300);

            var layersPath = new List<string>();

            foreach (var layer in layers)
            {
                var destPath = Path.Combine(App.tmpDirPath,$"{layer.Digest.Split(':')[1]}.tar.gz");
                layersPath.Add(destPath);

                var uriString = $@"{DOCKER_REGISTRY}/{image}/blobs/{layer.Digest}";

                var uri = new Uri(uriString);
                using var httpResponse = await httpClient.GetAsync(uri);
                using var content = httpResponse.Content;
                await using var layerStream = await content.ReadAsStreamAsync();

                var layerFile = File.Create(destPath);
                await layerStream.CopyToAsync(layerFile);
                layerFile.Close();
            }

            return layersPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to download image layers : " + ex);
            return null;
        }
    }


}