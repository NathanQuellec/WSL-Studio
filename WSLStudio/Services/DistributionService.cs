using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Diagnostics;
using System.Collections.ObjectModel;
using ColorCode.Compilation.Languages;
using Community.Wsl.Sdk;
using Docker.DotNet;
using Docker.DotNet.Models;
using WSLStudio.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using ICSharpCode.SharpZipLib.Tar;

namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private const string WSL_UNC_PATH = @"\\wsl.localhost";
    private const string DOCKER_PIPE_URI = @"npipe://./pipe/docker_engine";

    private readonly IList<Distribution> _distros = new List<Distribution>();
    private readonly WslApi _wslApi = new();

    public void InitDistributionsList()
    {
        try
        {
            var apiDistroList = _wslApi?.GetDistributionList()
                // Filter Docker special-purpose internal Linux distros 
                .Where(distro => (distro.DistroName != "docker-desktop") &&
                                 (distro.DistroName != "docker-desktop-data"))
                .Select(distro => new Distribution()
                {
                    Id = distro.DistroId,
                    Path = distro.BasePath,
                    IsDefault = distro.IsDefault,
                    WslVersion = distro.WslVersion,
                    Name = distro.DistroName,
                });

            if (apiDistroList == null)
                return;

            foreach (var distro in apiDistroList) 
                this._distros.Add(distro);
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine("INFO: No WSL distributions found in the system");
        }
    }

    public IEnumerable<Distribution> GetAllDistributions()
    {
        return _distros;
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


    public async Task CreateDistribution()
    {
        //var isDockerPipeExist = Directory.GetFiles("\\\\.\\pipe\\", "^docker_engine$").Length == 1;
        try
        {
          var workingDirectory = "C:\\Users\\nathan\\Documents\\wsl-studioDEV\\";

            using var tarball = CreateTarballForDockerfileDirectory(workingDirectory);

            IList<string> tags = new List<string>()
            {
                "wsl-studio-2"
            };
            var imageBuildParameters = new ImageBuildParameters
            {
               Tags = tags
            };
            //C# 8.0 using syntax
            var progress = new Progress<JSONMessage>();

            using var dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
            await dockerClient.Images.BuildImageFromDockerfileAsync(imageBuildParameters, tarball, null, null, progress);

        }
        catch (Exception ex)
        {
            Debug.WriteLine("[ERROR] Docker engine named pipe not found");
        }
    }

    public void RemoveDistribution(Distribution? distribution)
    {
        var process =  new ProcessBuilderHelper("cmd.exe")
            .SetArguments($"/c wsl --unregister {distribution?.Name}")
            .SetRedirectStandardOutput(true)
            .SetUseShellExecute(true)
            .SetCreateNoWindow(false)
            .Build();
        process.Start();

        if (distribution != null)
        {
            _distros.Remove(distribution);
            Debug.WriteLine($"[INFO] Distribution {distribution?.Name} deleted");
        }
        else
        {
            throw new ArgumentNullException();
        }
    }

    /**
     * Rename distro name in the Windows Registry.
     * With MSIX packaging, this type of actions make changes in a virtual registry and do not edit the real one.
     * Because we want to modify the system's user registry, we use flexible virtualization in Package.appxmanifest file.
     */
    public void RenameDistribution(Distribution? distribution)
    {
        Debug.WriteLine(this._distros);
        Debug.WriteLine($"[INFO] Editing Registry for {distribution.Name} with key : {distribution.Id}");
        var lxssRegPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");
        var lxsSubKeys = Registry.CurrentUser.OpenSubKey(lxssRegPath);

        foreach (var subKey in lxsSubKeys.GetSubKeyNames())
        {
            if (subKey == $"{{{distribution?.Id.ToString()}}}")
            {
                var distroRegPath = Path.Combine(lxssRegPath, subKey);
                var distroSubkeys = Registry.CurrentUser.OpenSubKey(distroRegPath, true);
                Debug.WriteLine(distroSubkeys.GetValue("DistributionName"));
                distroSubkeys.SetValue("DistributionName", distribution.Name);
                Debug.WriteLine($"OK {subKey}");
                distroSubkeys.Close();
            }
        }
        lxsSubKeys.Close();
    }

    public void LaunchDistribution(Distribution? distribution)
    {
        try
        {

            var lxssRegPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");
            var lxsSubKeys = Registry.CurrentUser.OpenSubKey(lxssRegPath);

            foreach (var subKey in lxsSubKeys.GetSubKeyNames())
            {
                if (subKey == $"{{{distribution?.Id.ToString()}}}")
                {
                    var distroRegPath = Path.Combine(lxssRegPath, subKey);
                    var distroSubkeys = Registry.CurrentUser.OpenSubKey(distroRegPath, true);
                    Debug.WriteLine(distroSubkeys.GetValue("DistributionName"));
                    distroSubkeys.Close();
                }
            }
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl ~ -d {distribution?.Name}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(true)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            Debug.WriteLine($"[INFO] Process ID : {process.Id} and NAME : {process.ProcessName} started");
            distribution?.RunningProcesses.Add(process);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Process start failed for distro {distribution.Name}");
        }

    }

    public void StopDistribution(Distribution? distribution)
    {
        if (distribution?.RunningProcesses == null)
        {
            Debug.WriteLine($"[ERROR] Try to execute StopDistribution method but " +
                            $"they are no processes running for {distribution.Name}");
        }
        else
        {
            foreach (var process in distribution.RunningProcesses)
            {

                process.CloseMainWindow();
                process.WaitForExit(30000);

                if (process.HasExited)
                {
                    Debug.WriteLine($"[INFO] Process ID : {process.Id} and " +
                                    $"NAME : {process.ProcessName} is closed");
                }
            }
        }
    }

    // TODO: Check why opening distro file system invoke sometimes an error. 

    public void OpenDistributionFileSystem(Distribution? distribution)
    {
        string distroPath = Path.Combine(WSL_UNC_PATH, $"{distribution.Name}");

        var processBuilder = new ProcessBuilderHelper("explorer.exe")
            .SetArguments(distroPath)
            .Build();
        processBuilder.Start();
    }

}