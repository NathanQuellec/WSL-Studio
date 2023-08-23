using System.Globalization;
using ICSharpCode.SharpZipLib.GZip;
using System.Text;
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

    public List<Snapshot> GetDistributionSnapshots(string distroPath)
    {
        var snapshotsList = new List<Snapshot>();
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
                });
            }

            return snapshotsList;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return new List<Snapshot>();
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
            decimal sizeOfSnap = await CompressSnapshot(snapshotPath, snapshotFolder);
            var snapshot = new Snapshot()
            {
                Id = snapshotId,
                Name = snapshotName,
                Description = snapshotDescr,
                CreationDate = currentDateTime,
                Size = sizeOfSnap.ToString(CultureInfo.InvariantCulture),
                DistroSize = distribution.Size,
            };

            distribution.Snapshots.Insert(0, snapshot);
            await SaveDistroSnapshotInfos(snapshotFolder, snapshot);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    private async Task<decimal> CompressSnapshot(string snapshotPath, string destPath)
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

    private async Task SaveDistroSnapshotInfos(string snapshotFolder, Snapshot snapshot)
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
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}