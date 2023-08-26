using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WSLStudio.Contracts.Services;
using WSLStudio.Helpers;
using WSLStudio.Models;
using WSLStudio.Services.Factories;

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
        _snapshotService.DeleteSnapshotFile(snapshot);
        _snapshotService.DeleteSnapshotInfosRecord(snapshot);
    }

    public async void CreateDistroFromSnapshot(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var distroNameInput = (sender.Content as StackPanel)?.FindChild("DistroNameInput") as TextBox;
            var snapshot = sender.DataContext as Snapshot;
            _distrosViewModel.ValidateDistributionName(sender, args);
            await _distrosViewModel.CreateDistributionViewModel(distroNameInput!.Text, "Archive", snapshot!.Path);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            args.Cancel = true;
        }
    }
}