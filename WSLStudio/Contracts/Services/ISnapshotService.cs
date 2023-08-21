using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface ISnapshotService
{
    Task<bool> CreateDistroSnapshot(Distribution distribution, string snapshotName, string snapshotDescr);
    List<Snapshot> GetDistributionSnapshots(string distroPath);
}