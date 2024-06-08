using System.Collections.ObjectModel;
using WSLStudio.Models;

namespace WSLStudio.Contracts.Services;

public interface ISnapshotService
{
    ObservableCollection<Snapshot> GetDistributionSnapshots(string distroPath);
    Task<bool> CreateSnapshot(Distribution distribution, string snapshotName, string snapshotDescr, bool isFastSnapshot);
    void DeleteSnapshotInfosRecord(Snapshot snapshot);
}