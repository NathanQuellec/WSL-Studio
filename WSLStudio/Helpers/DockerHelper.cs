using System.Net.Http.Headers;
using System.Net.Http.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using WSLStudio.Models.Docker;
using WSLStudio.Models.Docker.Manifests;

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
        Log.Information("Building Docker image ...");
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
        catch (Exception ex)
        {
            Log.Error($"Docker image build failed - Caused by exception : {ex}");
            throw new Exception("Failed to build Docker image, please check that Docker Desktop is running on your host");
        }
    }

    public async Task PullImageFromDockerHub(string imageName, string imageTag)
    {
        Log.Information("Pulling Docker image from DockerHub ...");
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
        catch (Exception ex)
        {
            Log.Error($"Failed to pull Docker image - Caused by exception : {ex}");
            throw new Exception("Failed to pull Docker image");
        }
    }

    public async Task<CreateContainerResponse?> CreateDockerContainer(string imageName, string containerName)
    {
        Log.Information("Creating Docker container ...");
        try
        {

            return await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = imageName,
                Name = containerName,
            });

        }
        catch (Exception ex)
        {
            await RemoveDockerImage(imageName);
            Log.Error($"Failed to create Docker container - Caused by exception : {ex}");
            throw new Exception("Failed to create Docker container from this image");
        }
    }

    public async Task ExportDockerContainer(string containerName, string targetPath)
    {
        Log.Information("Exporting Docker container ...");
        try
        {

            await using var exportStream = await _dockerClient.Containers.ExportContainerAsync(containerName);

            await using var fileStream = new FileStream(targetPath, FileMode.Create);
            await exportStream.CopyToAsync(fileStream);

        }
        catch (Exception ex)
        {
            Log.Error($"Failed to export Docker container - Caused by exception : {ex}");
            throw new Exception("Failed to export Docker Container for distribution creation");
        }

    }

    public async Task RemoveDockerImage(string imageName)
    {
        Log.Information("Deleting Docker image ...");
        try
        {
            await _dockerClient.Images.DeleteImageAsync(imageName, new ImageDeleteParameters()
            {
                Force = true,
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete Docker image - Caused by exception : {ex}");
        }
    }

    public async Task RemoveDockerContainer(string containerId)
    {
        Log.Information("Deleting Docker container ...");

        try
        {
            await _dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters()
            {
                Force = true,
            });
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete Docker container - Caused by exception : {ex}");
        }
    }

    public static async Task<AuthToken?> GetAuthToken(string imageName)
    {
        Log.Information("Fetching Docker image authtoken ...");

        var uriString =
            $@"https://auth.docker.io/token?service=registry.docker.io&scope=repository:{imageName}:pull";

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
            Log.Error($"Failed to fetch Docker image authtoken - Caused by exception : {ex}");
            throw new Exception("Failed to fetch image authentication token");
        }

    }

    public static async Task<IImageManifest?> GetImageManifest(AuthToken authToken, string imageName, string imageTag)
    {
        Log.Information("Fetching Docker image manifest ...");

        var manifestRootUri = $@"{DOCKER_REGISTRY}/{imageName}/manifests";
        var manifestUri = new Uri(manifestRootUri + $"/{imageTag}");
        using var httpClient = new HttpClient();

        // docker manifest spec
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.distribution.manifest.v2+json"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.container.image.v1+json"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.image.rootfs.diff.tar.gzip"));

        // oci manifest spec
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.oci.image.index.v1+json"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.oci.image.manifest.v1+json"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.oci.image.config.v1+json"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.oci.image.layer.v1.tar+gzip"));

        try
        {
            using var manifestResponse = await httpClient.GetAsync(manifestUri);
            using var content = manifestResponse.Content;

            IImageManifest? imageManifest;
            switch (content.Headers.ContentType?.ToString())
            {
                // if manifest is a fat manifest (a list of others manifest)
                case "application/vnd.oci.image.index.v1+json":
                {
                    Log.Information("Fetching fat manifest");
                    var fatManifest = content.ReadFromJsonAsync<ImageFatManifest>().Result;
                    var selectedManifest = manifestRootUri + $"/{fatManifest?.GetManifestByArchitecture("amd64")}";
                    var selectedManifestUri = new Uri(selectedManifest);

                    Log.Information("Fetching manifest with amd64 architecture");
                    using var selectedManifestResponse = await httpClient.GetAsync(selectedManifestUri);
                    using var newContent = selectedManifestResponse.Content;
                    imageManifest = newContent.ReadFromJsonAsync<DockerImageManifest>().Result;
                    break;
                }
                case "application/vnd.docker.distribution.manifest.v2+json" or
                     "application/vnd.oci.image.manifest.v1+json":
                {
                    Log.Information("Fetching standard manifest");
                    imageManifest = content.ReadFromJsonAsync<DockerImageManifest>().Result;
                    break;
                }
                default:
                    imageManifest = null;
                    break;
            }

            return imageManifest;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to fetch Docker image manifest - Caused by exception : {ex}");
            throw new Exception("Failed to fetch image manifest");
        }
    }

    public static async Task<List<string>?> GetLayers(AuthToken authToken, IImageManifest imageManifest, string imageName)
    {
        Log.Information("Fetching Docker image layers ...");

        try
        {
            var layers = imageManifest.GetLayers();
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.Token);
            httpClient.Timeout = TimeSpan.FromSeconds(300);

            var layersPath = new List<string>();

            foreach (var layer in layers)
            {
                var destPath = Path.Combine(App.TmpDirPath, $"{layer.Split(':')[1]}.tar.gz");
                layersPath.Add(destPath);

                var uriString = $@"{DOCKER_REGISTRY}/{imageName}/blobs/{layer}";

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
            Log.Error($"Failed to fetch Docker image layers - Caused by exception : {ex}");
            throw new Exception("Failed to fetch image layers");
        }
    }


}