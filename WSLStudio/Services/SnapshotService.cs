using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ICSharpCode.SharpZipLib.GZip;
using System.Text;
using Newtonsoft.Json.Bson;
using WSLStudio.Contracts.Services;
using WSLStudio.Models;

namespace WSLStudio.Services;

public class SnapshotService : ISnapshotService
{
    private readonly IWslService _wslService;

    public SnapshotService(IWslService wslService)
    {
        _wslService = wslService;
    }

    public ObservableCollection<Snapshot> GetDistributionSnapshots(string distroPath)
    {
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
            Console.WriteLine(ex.Message);
            return new ObservableCollection<Snapshot>();
        }
    }

    public async Task<bool> CreateDistroSnapshot(Distribution distribution, string snapshotName, string snapshotDescr)
    {

        var currentDateTime = DateTime.Now.ToString("dd MMMMM yyyy HH:mm:ss");
        var snapshotFolder = Path.Combine(distribution.Path, "snapshots");
        if (!Directory.Exists(snapshotFolder))
        {
            Directory.CreateDirectory(snapshotFolder);
        }

        var snapshotId = Guid.NewGuid();
        var snapshotPath = Path.Combine(snapshotFolder, $"{snapshotId}_{snapshotName}.tar");

        try
        {
            await this._wslService.ExportDistribution(distribution.Name, snapshotPath);
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
            await SaveDistroSnapshotInfos(snapshotFolder, snapshot);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    private static async Task<decimal> CompressSnapshot(string snapshotPath)
    {
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
            Console.WriteLine("Snapshot compression failed : " + ex.Message);
            throw;
        }
    }

    private static async Task SaveDistroSnapshotInfos(string snapshotFolder, Snapshot snapshot)
    {
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
            Console.WriteLine(ex.Message);
        }
    }

    public void DeleteSnapshotFile(Snapshot snapshot)
    {
        if (File.Exists(snapshot.Path))
        {
            File.Delete(snapshot.Path);
        }
    }

    public async void DeleteSnapshotInfosRecord(Snapshot snapshot)
    {
        try
        {
            var snapshotsFolder = Directory.GetParent(snapshot.Path).Name;
            var snapshotsInfosFile = Path.Combine(snapshotsFolder, "SnapshotsInfos");
            var recordsToKeep = (await File.ReadAllLinesAsync(snapshotsInfosFile))
                .Where(line => line.Split(';')[0] != snapshot.Id.ToString());
            await File.WriteAllLinesAsync(snapshot.Path, recordsToKeep);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}