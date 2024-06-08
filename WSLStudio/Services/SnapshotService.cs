using System.Collections.ObjectModel;
using System.Globalization;
using ICSharpCode.SharpZipLib.GZip;
using Serilog;
using WSLStudio.Contracts.Services;
using WSLStudio.Exceptions;
using WSLStudio.Helpers;
using WSLStudio.Models;
using WSLStudio.Contracts.Services.Storage;
using WSLStudio.Enums;
using WSLStudio.Services.Storage;

namespace WSLStudio.Services;

public class SnapshotService : ISnapshotService
{
    private readonly IFileStorageService _fileStorageService;

    public SnapshotService()
    {
        _fileStorageService = new JsonFileStorageService();
    }

    private static async Task CreateFastSnapshot(string distroPath, string snapshotPath)
    {
        var distroImagePath = Path.Combine(distroPath, "ext4.vhdx");

        await WslHelper.ShutdownWsl();
        await using var distroImage = File.OpenRead(distroImagePath);
        await using var snapshotFile = File.OpenWrite(snapshotPath);
        await distroImage.CopyToAsync(snapshotFile);

        snapshotFile.Close();
        await distroImage.DisposeAsync();
    }

    // TODO : Refactor
    public async Task<bool> CreateSnapshot(Distribution distribution, string snapshotName, string snapshotDescr, bool isFastSnapshot)
    {
        Log.Information($"Creating snapshot {snapshotName} from distribution {distribution.Name} ...");

        var currentDateTime = DateTime.Now.ToString("dd MMMMM yyyy HH:mm:ss");
        var snapshotFolder = FilesHelper.CreateDirectory(distribution.Path, "snapshots");
        var snapshotId = Guid.NewGuid();
        var snapshotPath = Path.Combine(snapshotFolder, $"{snapshotId}_{snapshotName}");

        try
        {

            var snapshotType = SnapshotType.Vhdx;
            if (isFastSnapshot)
            {
                snapshotPath += ".vhdx";
                await CreateFastSnapshot(distribution.Path, snapshotPath);
            }
            else
            {
                snapshotPath += ".tar";
                snapshotType = SnapshotType.Archive;
                await WslHelper.ExportDistribution(distribution.Name, snapshotPath);
            }

            decimal sizeOfSnap = await CompressSnapshot(snapshotPath);
            snapshotPath += ".gz"; // adding .gz extension file after successfully completed the compression

            var snapshot = new Snapshot()
            {
                Id = snapshotId,
                Name = snapshotName,
                Description = snapshotDescr,
                Type = snapshotType.ToString(),
                CreationDate = currentDateTime,
                Size = sizeOfSnap.ToString(CultureInfo.InvariantCulture),
                DistroSize = distribution.Size,
                Path = snapshotPath,
            };

            distribution.Snapshots.Insert(0, snapshot);

            var currentTotalSnapSize = decimal.Parse(distribution.SnapshotsTotalSize, CultureInfo.InvariantCulture);
            distribution.SnapshotsTotalSize = (currentTotalSnapSize + sizeOfSnap)
                .ToString(CultureInfo.InvariantCulture);

            await SaveDistroSnapshotInfos(snapshot, snapshotFolder);

            return true;
        }
        catch (FileCompressionException ex)
        {
            Log.Error($"Failed to compress snapshot {snapshotName} from distribution {distribution.Name} - Caused by exception {ex}");
            Log.Information("Deleting snapshot file ...");
            File.Delete(snapshotPath);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create snapshot {snapshotName} from distribution {distribution.Name} - Caused by exception {ex}");
            Log.Information("Deleting snapshot file ...");
            File.Delete(snapshotPath);
            return false;
        }
    }

    private static async Task<decimal> CompressSnapshot(string snapshotPath)
    {
        Log.Information($"Compressing snapshot from .tar to .tar.gz in location {snapshotPath}");
        try
        {
            var compressedFilePath = snapshotPath + ".gz";

            await using Stream s = new GZipOutputStream(File.Create(compressedFilePath));
            await using var fs = File.OpenRead(snapshotPath);
            await fs.CopyToAsync(s, 4096, CancellationToken.None);
            fs.Close();

            File.Delete(snapshotPath);
            var sizeInGB = (decimal)s.Length / 1024 / 1024 / 1024;

            return Math.Round(sizeInGB, 2);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to compress snapshot in {snapshotPath} - Caused by exception : {ex}");
            throw new FileCompressionException();
        }
    }

    public ObservableCollection<Snapshot> GetDistributionSnapshots(string distroPath)
    {
        Log.Information($"Populate list of snapshots from {distroPath} snapshot records file");
        var snapshotsInfosPath = Path.Combine(distroPath, "snapshots", "SnapshotsInfos.json");

        try
        {
            var snapshotList = _fileStorageService.Load<Snapshot>(snapshotsInfosPath);
            return snapshotList;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to populate list of snapshots from snapshot records file - Caused by exception {ex}");
            return new ObservableCollection<Snapshot>();
        }
    }

    private async Task SaveDistroSnapshotInfos(Snapshot snapshot, string snapshotFolder)
    {
        Log.Information($"Saving snapshot {snapshot.Name} information in {snapshotFolder} folder");
        try
        {
            var snapshotInfosFile = Path.Combine(snapshotFolder, "SnapshotsInfos.json");
            await _fileStorageService.Save<Snapshot>(snapshotInfosFile, snapshot);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save snapshot information - Caused by exception : {ex}");
            Log.Information("Deleting snapshot file ...");
            File.Delete(snapshot.Path);
        }
    }

    public async void DeleteSnapshotInfosRecord(Snapshot snapshot)
    {
        Log.Information($"Delete snapshot {snapshot.Name} information records");
        try
        {
            var snapshotsFolder = Directory.GetParent(snapshot.Path)?.FullName;

            if (snapshotsFolder != null)
            {
                var snapshotsInfosFile = Path.Combine(snapshotsFolder, "SnapshotsInfos.json");
                await _fileStorageService.Delete<Snapshot>(snapshotsInfosFile, snapshot);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete snapshot information records - Caused by exception : {ex}");
        }
    }
}