using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using WSLStudio.Contracts.Services;
using WSLStudio.Models;

namespace WSLStudio.ViewModels;

public class DisplaySnapshotsVM : ObservableObject
{
    private readonly ISnapshotService _snapshotService;

    private readonly DistrosListDetailsVM _distrosViewModel;

    public RelayCommand<Snapshot> DeleteSnapshotCommand { get; set; }

    public DisplaySnapshotsVM(ISnapshotService snapshotService)
    {
        _snapshotService = snapshotService;
        _distrosViewModel = App.GetService<DistrosListDetailsVM>();

        DeleteSnapshotCommand = new RelayCommand<Snapshot>(DeleteSnapshotViewModel);
    }

    public void DeleteSnapshotViewModel(Snapshot snapshot)
    {
        Log.Information("Deleting snapshot record");
        _snapshotService.DeleteSnapshotInfosRecord(snapshot);

        Log.Information("Deleting snapshot file");
        File.Delete(snapshot.Path);
    }

    public async void CreateDistroFromSnapshot(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            App.IsDistributionProcessing = true;

            var distroNameInput = (sender.Content as StackPanel)?.FindChild("DistroNameInput") as TextBox;
            var snapshot = sender.DataContext as Snapshot;

            _distrosViewModel.ValidateDistributionName(sender, args);
            await _distrosViewModel.CreateDistributionViewModel(distroNameInput!.Text, snapshot!.Type, snapshot!.Path);

            App.IsDistributionProcessing = false;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create distribution from snapshot - Caused by {ex}");
            App.IsDistributionProcessing = false;
            args.Cancel = true;
        }
    }
}