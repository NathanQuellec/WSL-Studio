using System.Collections.ObjectModel;
using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface ISnapshotService
{
    ObservableCollection<Snapshot> GetDistributionSnapshots(string distroPath);
    Task<bool> CreateDistroSnapshot(Distribution distribution, string snapshotName, string snapshotDescr);
    void DeleteSnapshotFile(Snapshot snapshot);
    void DeleteSnapshotInfosRecord(Snapshot snapshot);
}