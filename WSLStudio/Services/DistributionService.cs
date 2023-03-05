﻿using System;
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
using WSLStudio.Contracts.Services.Factories;
using WSLStudio.Services.Factories;
using CommunityToolkit.WinUI.Helpers;

namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private const string WSL_UNC_PATH = @"\\wsl.localhost";
    private const string APP_FOLDER = "WslStudio";

    private static readonly object _lock = new object();

    private readonly IList<Distribution> _distros;

    private readonly WslApi _wslApi;

    public DistributionService()
    {
        _distros = new List<Distribution>();
        _wslApi = new WslApi();
    }

    public async Task InitDistributionsList()
    {
        try
        {
            var lxssRegPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");
            var lxssSubKeys = Registry.CurrentUser.OpenSubKey(lxssRegPath);

            foreach (var subKey in lxssSubKeys.GetSubKeyNames())
            {
                // we iterate only on distros registry keys
                if (!subKey.StartsWith('{') || !subKey.EndsWith('}'))
                {
                    continue;
                }

                var distroRegPath = Path.Combine(lxssRegPath, subKey);
                var distroSubkeys = Registry.CurrentUser.OpenSubKey(distroRegPath);

                var distroName = (string)distroSubkeys.GetValue("DistributionName");

                // Filter Docker special-purpose internal Linux distros 
                if ((distroName != "docker-desktop") && (distroName != "docker-desktop-data"))
                {
                    var distroPath = (string)distroSubkeys.GetValue("BasePath");
                    var wslVersion = (int)distroSubkeys.GetValue("Version");

                    var distro = new Distribution()
                    {
                        Id = Guid.Parse(subKey),
                        Name = distroName,
                        Path = distroPath,
                        WslVersion = wslVersion,
                    };

                    this._distros.Add(distro);
                    Console.WriteLine(distroSubkeys.GetValue("DistributionName"));
                }

                distroSubkeys.Close();
            }

            lxssSubKeys.Close();

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public async Task SetDistributionsInfos()
    {
        foreach (var distro in _distros)
        {
            distro.OsName = await GetDistroOsName(distro.Name);
            distro.OsVersion =  await GetDistroOsVersion(distro.Name);
            distro.Size =  GetDistroSize(distro.Path);
        }
    }

    private static async Task<string> GetDistroOsName(string distroName)
    {
        
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl -d {distroName} grep \"^NAME\" /etc/os-release")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();

            var output = process.StandardOutput.ReadToEndAsync().GetAwaiter().GetResult();
            await process.WaitForExitAsync();

            var osName = output.Remove(0, 5) // Remove "NAME=" substring from output
                .Replace('\"', ' ')
                .Trim();
            Console.WriteLine($"OS Name for {distroName} is {osName}");

            return osName;
    }

    private static async Task<string> GetDistroOsVersion(string distroName)
    {
        
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl -d {distroName} grep \"^VERSION_ID\" /etc/os-release")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();

            var output = process.StandardOutput.ReadToEndAsync().GetAwaiter().GetResult();
            
            await process.WaitForExitAsync();

            var osVersion = output.Remove(0, 11) // Remove "VERSION_ID=" substring from output
                .Replace('\"', ' ')
                .Trim();
            Console.WriteLine($"OS Version for {distroName} is {osVersion}");

            return osVersion;

    }

    private static string GetDistroSize(string distroPath)
    {
        lock (_lock)
        {
            var diskLocation = Path.Combine(distroPath, "ext4.vhdx");
            var diskFile = new FileInfo(diskLocation);
            var sizeInGB = (double)diskFile.Length / 1024 / 1024 / 1024;
            return Math.Round(sizeInGB, 2).ToString();
        }
    }

    public IEnumerable<Distribution> GetAllDistributions()
    {
        return _distros;
    }

    private static Task<string> CreateDistributionFolder(string distroName)
    {
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

        return Task.FromResult(distroFolder);
    }

    public async Task<Distribution?> CreateDistribution(string creationMode, string distroName, string resourceOrigin)
    {

        var distroFolder = await CreateDistributionFolder(distroName);

        DistributionFactory factory = creationMode switch
        {
            "Dockerfile" => new DockerfileDistributionFactory(),
            "Archive" => new ArchiveDistributionFactory(),
            "Docker Hub" => new DockerHubDistributionFactory(),
            _ => throw new NotImplementedException(),
        };

        var newDistro = await factory.CreateDistribution(distroName, resourceOrigin, distroFolder);

        if (newDistro == null)
        {
            return null;
        }

        // set the id of our model with the id of the new distro generated by wsl 
        foreach (var distro in _wslApi.GetDistributionList())
        {
            if (distro.DistroName == newDistro.Name)
            {
                newDistro.Id = distro.DistroId;
            }
        }
        this._distros.Add(newDistro);

        return newDistro;
    }

    public void RemoveDistribution(Distribution distribution)
    {
        var process = new ProcessBuilderHelper("cmd.exe")
            .SetArguments($"/c wsl --unregister {distribution?.Name}")
            .SetCreateNoWindow(true)
            .Build();
        process.Start();

        if (distribution != null)
        {
            _distros.Remove(distribution);
            Console.WriteLine($"[INFO] Distribution {distribution?.Name} deleted");
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
    public bool RenameDistribution(Distribution distribution, string newDistroName)
    {
        Console.WriteLine($"[INFO] Editing Registry for {distribution.Name} with key : {distribution.Id}");
        var lxssRegPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");
        var lxsSubKeys = Registry.CurrentUser.OpenSubKey(lxssRegPath);

        foreach (var subKey in lxsSubKeys.GetSubKeyNames())
        {
            if (subKey != $"{{{distribution.Id.ToString()}}}")
            {
                continue;
            }

            var distroRegPath = Path.Combine(lxssRegPath, subKey);
            var distroSubkeys = Registry.CurrentUser.OpenSubKey(distroRegPath, true);
            Console.WriteLine(distroSubkeys.GetValue("DistributionName"));
            distroSubkeys.SetValue("DistributionName", newDistroName);
            Console.WriteLine($"OK {subKey}");
            distroSubkeys.Close();
            lxsSubKeys.Close();
            //this.RenameDistributionFolder(distribution.Name, newDistroName);
            return true;
        }
        lxsSubKeys.Close();
        return false;
    }

    // TODO : Rename distro folder
    /*public void RenameDistributionFolder(string distroName, string newDistroName)
    {
        var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var distroPath = Path.Combine(roamingPath, APP_FOLDER, distroName);
        var newDistroPath = Path.Combine(roamingPath, APP_FOLDER, newDistroName);

        try
        {
            if (Directory.Exists(distroPath))
            {
                Directory.Move(distroPath, newDistroPath);
                Console.WriteLine("Directory renamed successfully.");
            }
            else
            {
                Console.WriteLine("Source directory does not exist.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error renaming directory: " + e.Message);
        }
    }*/

    public void LaunchDistribution(Distribution distribution)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl ~ -d {distribution?.Name}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(true)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            Console.WriteLine($"[INFO] Process ID : {process.Id} and NAME : {process.ProcessName} started");
            distribution?.RunningProcesses.Add(process);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Process start failed for distro {distribution.Name}, reason : {ex}");
        }

    }

    public void StopDistribution(Distribution distribution)
    {
        if (distribution?.RunningProcesses == null)
        {
            Console.WriteLine($"[ERROR] Try to execute StopDistribution method but " +
                            $"they are no processes running for {distribution!.Name}");
        }
        else
        {
            foreach (var process in distribution.RunningProcesses)
            {

                process.CloseMainWindow();
                process.WaitForExit(30000);

                if (process.HasExited)
                {
                    Console.WriteLine($"[INFO] Process ID : {process.Id} and " +
                                    $"NAME : {process.ProcessName} is closed");
                }
            }
        }
    }

    // TODO: Check why opening distro file system invoke sometimes an error. 

    public void OpenDistributionFileSystem(Distribution distribution)
    {
        var distroPath = Path.Combine(WSL_UNC_PATH, $"{distribution.Name}");

        var processBuilder = new ProcessBuilderHelper("explorer.exe")
            .SetArguments(distroPath)
            .Build();
        processBuilder.Start();
    }

    public void OpenDistributionWithVsCode(Distribution distribution)
    {
        var process = new ProcessBuilderHelper("cmd.exe")
            .SetArguments($"/c wsl ~ -d {distribution?.Name} code .")
            .Build();
        process.Start();
    }

    public void OpenDistroWithWt(Distribution distribution)
    {
        var process = new ProcessBuilderHelper("cmd.exe")
            .SetArguments($"/c wt wsl ~ -d {distribution?.Name}")
            .SetRedirectStandardOutput(false)
            .SetUseShellExecute(false)
            .SetCreateNoWindow(true)
            .Build();
        process.Start();
    }
}