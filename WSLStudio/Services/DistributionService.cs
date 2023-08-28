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
using System.Globalization;
using System.Text.RegularExpressions;
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
using DiscUtils;
using DiscUtils.Dmg;
using DiscUtils.Iso9660;
using DiscUtils.Streams;
using DiscUtils.Vhdx;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip;
using Ionic.Zip;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.CST.RecursiveExtractor.Extractors;
using SharpCompress;
using WinRT;
using Disk = DiscUtils.Dmg.Disk;
using ZipEntry = ICSharpCode.SharpZipLib.Zip.ZipEntry;
using ZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;
using ZipOutputStream = ICSharpCode.SharpZipLib.Zip.ZipOutputStream;


namespace WSLStudio.Services;

public class DistributionService : IDistributionService
{
    private const string WSL_UNC_PATH = @"\\wsl.localhost";
    private const string APP_FOLDER = "WslStudio";

    private static readonly object _lock = new object();

    private readonly IList<Distribution> _distros;
    private readonly WslApi _wslApi;

    private readonly ISnapshotService _snapshotService;

    public DistributionService(ISnapshotService snapshotService)
    {
        _distros = new List<Distribution>();
        _wslApi = new WslApi();
        _snapshotService = snapshotService;
    }

    // TODO : Refactor InitDistributionsList using parallel task
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
                if (distroName != "docker-desktop" && distroName != "docker-desktop-data")
                {
                    var distroPath = (string)distroSubkeys.GetValue("BasePath");
                    var wslVersion = (int)distroSubkeys.GetValue("Version");

                    // launch distro in the background to get access to distro file system infos (os name,version,etc)
                    /*var isDistroRunning = await CheckRunningDistribution(distroName);
                    if (!isDistroRunning)
                    {
                      //  await BackgroundLaunchDistribution(distroName);
                      //  await WaitForRunningDistribution(distroName);
                    }*/
                   
                    var distro = new Distribution()
                    {
                        Id = Guid.Parse(subKey),
                        Name = distroName,
                        Path = distroPath,
                        WslVersion = wslVersion,
                      //  OsName = GetOsInfos(distroName, "NAME"),
                     //   OsVersion = GetOsInfos(distroName, "VERSION"),
                      //  Size = GetSize(distroPath),
                      //  Users = GetDistributionUsers(distroName),
                       // Snapshots = _snapshotService.GetDistributionSnapshots(distroPath),
                    };

                    GetTestInfos(distro);

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


    // TODO : Fix unknown os version field
    private static string GetOsInfos(string distroName, string field)
    {
        var osInfosFilePath = Path.Combine(WSL_UNC_PATH, distroName, "etc", "os-release");
        var osInfosPattern = $@"(\b{field}="")(.*?)""";

        try
        {
            var osInfosFile = new FileInfo(osInfosFilePath);

            if (osInfosFile.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                Console.WriteLine("/etc/os-release is a symbolic link to /usr/lib/os-release");
                osInfosFilePath = Path.Combine(WSL_UNC_PATH, distroName, "usr", "lib", "os-release");
            }

            Console.WriteLine("----------------GET OS INFOS----------------");
            using var streamReader = new StreamReader(osInfosFilePath);
            var osInfos = "";
            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                var osInfosRegex = Regex.Match(line, osInfosPattern);
                if (osInfosRegex.Success)
                {
                    osInfos = osInfosRegex.Groups[2].Value;
                }

            }
            streamReader.Close();

            return (string.IsNullOrEmpty(osInfos) ? "Unknown" : osInfos);
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("os-release file doesn't exist : " + e.Message);
            return "Unknown";
        }
        catch (IOException e)
        {
            Console.WriteLine("Cannot open or read os-release file : " + e.Message);
            return "Unknown";
        }
        catch (Exception e)
        {
            Console.WriteLine("Cannot get os infos from os-release file : " + e.Message);
            return "Unknown";
        }
    }

    private static void GetTestInfos(Distribution distro)
    {
        var distroImage =  Path.Combine($"{distro.Path}", "ext4.vhdx");
        using var file = new DiskImageFile(distroImage, FileAccess.Read);

        byte[] fileType = new byte[64*1024];
        byte[] header1 = new byte[64 * 1024];
        byte[] header2 = new byte[64 * 1024];
        byte[] region1 = new byte[64 * 1024];
        byte[] region2 = new byte[64 * 1024];
        byte[] reserved = new byte[680 * 1024];

        var shift = fileType.Length;

        var content = file.OpenContent(null, Ownership.None);


        byte[] data = new byte[shift+region1.Length];
        var read = content.Read(data, shift, header1.Length);

        var encode = UnicodeEncoding.Unicode.GetString(data,0,4096);
        
        var l = data.Length;
        
    }

    private static string GetSize(string distroPath)
    {
        lock (_lock)
        {
            var diskLocation = Path.Combine(distroPath, "ext4.vhdx");
            var diskFile = new FileInfo(diskLocation);
            var sizeInGB = (decimal)diskFile.Length / 1024 / 1024 / 1024;
            return Math.Round(sizeInGB, 2).ToString(CultureInfo.InvariantCulture);
        }
    }

    private static List<string> GetDistributionUsers(string distroName)
    {
        var passwdFilePath = Path.Combine(WSL_UNC_PATH, distroName, "etc", "passwd");
        var userShellPattern = @"/bin/(.*?)sh$";
        var usersList = new List<string>();

        try
        {

            Console.WriteLine("----------------GET USERS LIST----------------");
            using var streamReader = new StreamReader(passwdFilePath);

            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                var userShellRegex = Regex.Match(line, userShellPattern);

                // get first column of passwd file when matching regex (i.e. get user field)
                if (userShellRegex.Success)
                {
                    usersList.Add(line.Split(':')[0]);
                }
            }
            streamReader.Close();
            return usersList;
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("/etc/passwd file doesn't exist : " + e.Message);
            usersList.Add("Unknown");
            return usersList;
        }
        catch (IOException e)
        {
            Console.WriteLine("Cannot open or read /etc/passwd file : " + e.Message);
            usersList.Add("Unknown");
            return usersList;
        }
        catch (Exception e)
        {
            Console.WriteLine("Cannot get list of users from /etc/passwd file : " + e.Message);
            usersList.Add("Unknown");
            return usersList;
        }
    }

    

    public IEnumerable<Distribution> GetAllDistributions()
    {
        return _distros;
    }

    private static string CreateDistributionFolder(string distroName)
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

        return distroFolder;
    }

    public async Task<Distribution?> CreateDistribution(string distroName, string creationMode, string resourceOrigin)
    {
        try
        {

            var distroFolder = CreateDistributionFolder(distroName);

            DistributionFactory factory = creationMode switch
            {
                "Dockerfile" => new DockerfileDistributionFactory(),
                "Archive" => new ArchiveDistributionFactory(),
                "Docker Hub" => new DockerHubDistributionFactory(),
                _ => throw new NullReferenceException(),
            };

            var newDistro = await factory.CreateDistribution(distroName, resourceOrigin, distroFolder);
            var distro = _wslApi
                .GetDistributionList()
                .FirstOrDefault(distro => distro.DistroName == newDistro.Name);


            newDistro.Id = distro.DistroId;
            newDistro.Path = distro.BasePath;
            newDistro.WslVersion = distro.WslVersion;
            newDistro.OsName = GetOsInfos(distroName, "NAME");
            newDistro.OsVersion = GetOsInfos(distroName, "VERSION");
            newDistro.Size = GetSize(distro.BasePath);
            newDistro.Users = GetDistributionUsers(distroName);

            this._distros.Add(newDistro);

            return newDistro;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
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

    private static async Task<bool> CheckRunningDistribution(string distroName)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments("/c wsl --list --running --quiet")
                .SetRedirectStandardOutput(true)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();

            var output = process.StandardOutput.ReadToEndAsync().GetAwaiter().GetResult();
            await process.WaitForExitAsync();
            var sanitizedOutput = output.Replace("\0", "")
                .Replace("\r", "");  // remove special character
            var runningDistros = sanitizedOutput.Split("\n");

            return runningDistros.Contains(distroName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Process start failed for distro {distroName}, reason : {ex}");
            return false;
        }
    }

    private async Task WaitForRunningDistribution(string distroName)
    {
        var isDistroRunning = await CheckRunningDistribution(distroName);
        if (!isDistroRunning)
        {
            await WaitForRunningDistribution(distroName);
        }
    }

    /** Workaround to solve file system access error (Issue : https://github.com/microsoft/wsl/issues/5307)
        Because a distribution need to be running to use its file system, 
        we quickly start and stop the corresponding distribution to avoid an error  
    **/
    private Task BackgroundLaunchDistribution(string distroName)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wsl -d {distroName}")
                .SetCreateNoWindow(true)
                .SetUseShellExecute(false)
                .Build();
            process.Start();

            return Task.CompletedTask;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Process start failed for distro {distroName}, reason : {ex}");
            return Task.FromException(ex);
        }
    }

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

    public async void StopDistribution(Distribution distribution)
    {
        if (distribution.RunningProcesses.Count == 0)
        {
            Console.WriteLine($"[ERROR] Try to execute StopDistribution method but " +
                            $"they are no processes running for {distribution!.Name}");
        }
        else
        {
            foreach (var process in distribution.RunningProcesses)
            {

                process.CloseMainWindow();
                await process.WaitForExitAsync();

                if (process.HasExited)
                {
                    Console.WriteLine($"[INFO] Process ID : {process.Id} and " +
                                      $"NAME : {process.ProcessName} is closed");
                }
                else
                {
                    process.Kill();
                }
            }
            distribution.RunningProcesses.Clear();
        }
    }

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

    public void OpenDistroWithWinTerm(Distribution distribution)
    {
        try
        {
            var process = new ProcessBuilderHelper("cmd.exe")
                .SetArguments($"/c wt wsl ~ -d {distribution?.Name}")
                .SetRedirectStandardOutput(false)
                .SetUseShellExecute(false)
                .SetCreateNoWindow(true)
                .Build();
            process.Start();
            Console.WriteLine($"[INFO] Process ID : {process.Id} and NAME : {process.ProcessName} started");
           // distribution?.RunningProcesses.Add(process);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Process start failed for distro {distribution.Name}, reason : {ex}");
        }
    }
}