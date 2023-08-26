using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Contracts.Services;
using WSLStudio.Models;

namespace WSLStudio.ViewModels;

public class SnapshotsVM : ObservableObject
{
    private  readonly ISnapshotService _snapshotService;

    public SnapshotsVM(ISnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
    }

    public void DeleteSnapshotViewModel(Snapshot snapshot)
    {
        _snapshotService.DeleteSnapshotFile(snapshot);
        _snapshotService.DeleteSnapshotInfosRecord(snapshot);
    }
}