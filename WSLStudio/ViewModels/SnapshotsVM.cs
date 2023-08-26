using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WSLStudio.Contracts.Services;
using WSLStudio.Models;

namespace WSLStudio.ViewModels;

public class SnapshotsVM : ObservableObject
{
    private readonly ISnapshotService _snapshotService;

    public RelayCommand<Snapshot> DeleteSnapshotCommand { get; set; }

    public SnapshotsVM(ISnapshotService snapshotService)
    {
        _snapshotService = snapshotService;

        DeleteSnapshotCommand = new RelayCommand<Snapshot>(DeleteSnapshotViewModel);
    }

    public void DeleteSnapshotViewModel(Snapshot snapshot)
    {
        _snapshotService.DeleteSnapshotFile(snapshot);
        _snapshotService.DeleteSnapshotInfosRecord(snapshot);
    }
}