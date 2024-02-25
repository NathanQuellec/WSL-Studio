using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ICSharpCode.SharpZipLib.GZip;
using System.Text;
using Serilog;
using WSLStudio.Contracts.Services;
using WSLStudio.Models;
using WSLStudio.Helpers;

namespace WSLStudio.Services;

public class SnapshotService : ISnapshotService
{

    public ObservableCollection<Snapshot> GetDistributionSnapshots(string distroPath)
    {
        Log.Information($"Populate list of snapshots from {distroPath} snapshot records file");

        var snapshotsList = new ObservableCollection<Snapshot>();
        var snapshotsInfosPath = Path.Combine(distroPath, "snapshots", "SnapshotsInfos");

        try
        {
            var snapshotsInfosLines = File.ReadAllLines(snapshotsInfosPath);
            for (var i = 1; i < snapshotsInfosLines.Length; i++)
            {
                var snapshotsInfos = snapshotsInfosLines[i].Split(';');
                snapshotsList.Insert(0, new Snapshot()
                {
                    Id = Guid.Parse(snapshotsInfos[0]),
                    Name = snapshotsInfos[1],
                    Description = snapshotsInfos[2],
                    CreationDate = snapshotsInfos[3],
                    Size = snapshotsInfos[4],
                    DistroSize = snapshotsInfos[5],
                    Path = snapshotsInfos[6],
                });
            }

            return snapshotsList;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to populate list of snapshots from snapshot records file - Caused by exception {ex}");
            return new ObservableCollection<Snapshot>();
        }
    }

    public async Task<bool> CreateDistroSnapshot(Distribution distribution, string snapshotName, string snapshotDescr)
    {
        Log.Information($"Creating snapshot {snapshotName} from distribution {distribution.Name} ...");

        var currentDateTime = DateTime.Now.ToString("dd MMMMM yyyy HH:mm:ss");
        var snapshotFolder = FilesHelper.CreateDirectory(distribution.Path, "snapshots");

        var snapshotId = Guid.NewGuid();

        try
        {
            var snapshotPath = Path.Combine(snapshotFolder, $"{snapshotId}_{snapshotName}.tar");
            await WslHelper.ExportDistribution(distribution.Name, snapshotPath);
            decimal sizeOfSnap = await CompressSnapshot(snapshotPath);
            snapshotPath += ".gz"; // adding .gz extension file after successfully completed the compression
            var snapshot = new Snapshot()
            {
                Id = snapshotId,
                Name = snapshotName,
                Description = snapshotDescr,
                CreationDate = currentDateTime,
                Size = sizeOfSnap.ToString(CultureInfo.InvariantCulture),
                DistroSize = distribution.Size,
                Path = snapshotPath,
            };

            distribution.Snapshots.Insert(0, snapshot);
            await SaveDistroSnapshotInfos(snapshot, snapshotFolder);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create snapshot {snapshotName} from distribution {distribution.Name} - Caused by exception {ex}");
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
            throw;
        }
    }

    private static async Task SaveDistroSnapshotInfos(Snapshot snapshot, string snapshotFolder)
    {
        Log.Information($"Saving snapshot {snapshot.Name} information in {snapshotFolder} folder");
        try
        {
            var snapshotInfosFile = Path.Combine(snapshotFolder, "SnapshotsInfos");
            var snapshotInfosHeader = new StringBuilder();
            var snapshotInfos = new StringBuilder();
            var properties = snapshot.GetType().GetProperties();

            if (!File.Exists(snapshotInfosFile))
            {
                snapshotInfosHeader.Append(string.Join(';', properties.Select(prop => prop.Name)));
                snapshotInfosHeader.Append('\n');
                await File.AppendAllTextAsync(snapshotInfosFile, snapshotInfosHeader.ToString());
            }

            snapshotInfos.Append(string.Join(';', properties.Select(prop => prop.GetValue(snapshot))));
            snapshotInfos.Append('\n');
            await File.AppendAllTextAsync(snapshotInfosFile, snapshotInfos.ToString());
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save snapshot information - Caused by exception : {ex}");
        }
    }

    public void DeleteSnapshotFile(Snapshot snapshot)
    {
        Log.Information("Deleting snapshot file ...");
        if (File.Exists(snapshot.Path))
        {
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
                var snapshotsInfosFile = Path.Combine(snapshotsFolder, "SnapshotsInfos");
                var recordsToKeep = (await File.ReadAllLinesAsync(snapshotsInfosFile))
                    .Where(line => line.Split(';')[0] != snapshot.Id.ToString());
                await File.WriteAllLinesAsync(snapshotsInfosFile, recordsToKeep);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete snapshot information records - Caused by exception : {ex}");
        }
    }
}