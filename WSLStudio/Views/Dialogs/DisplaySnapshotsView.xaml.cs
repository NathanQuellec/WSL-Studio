using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using WSLStudio.Helpers;
using WSLStudio.Models;
using WSLStudio.ViewModels;
using Path = System.IO.Path;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WSLStudio.Views.Dialogs;
public sealed partial class DisplaySnapshotsView : ContentDialog
{

    public DisplaySnapshotsVM ViewModel
    {
        get; set;
    }

    public DisplaySnapshotsView()
    {
        this.InitializeComponent();
        this.ViewModel = App.GetService<DisplaySnapshotsVM>();
    }

    private void OpenSnapshotsFolder(object sender, RoutedEventArgs args)
    {
        Log.Information("Opening snapshots folder ...");

        var distribution = this.DataContext as Distribution;
        var snapshotsFolderPath = Path.Combine(distribution!.Path, "snapshots");

        try
        {
            var process = new ProcessBuilder("explorer.exe")
                .SetArguments(snapshotsFolderPath)
                .Build();
            process.Start();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open snapshots folder - Caused by exception {ex}");
        }
    }

    private async void OpenDeleteSnapshotDialog(object sender, RoutedEventArgs args)
    {
        Log.Information("Opening delete snapshot dialog");

        this.Hide();
        var snapshot = (sender as Button)?.DataContext as Snapshot;

        if (App.IsDistributionProcessing)
        {
            App.ShowIsProcessingDialog();
            return;
        }

        var deleteSnapshotDialog = new ContentDialog()
        {
            Title = $"Are you sure to delete snapshot \"{snapshot!.Name}\" ?",
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DataContext = snapshot,
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };

        deleteSnapshotDialog.PrimaryButtonCommand = ViewModel.DeleteSnapshotCommand;
        deleteSnapshotDialog.PrimaryButtonCommandParameter = deleteSnapshotDialog.DataContext;

        try
        {
            var buttonClicked = await deleteSnapshotDialog.ShowAsync();
            if (buttonClicked == ContentDialogResult.Primary)
            {
                DeleteSnapshot(snapshot);
            }
            else
            {
                this.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open snapshot deletion dialog of {snapshot.Name} - Caused by exception {ex}");
        }
    }

    private void DeleteSnapshot(Snapshot snapshot)
    {
        Log.Information($"Deleting snapshot {snapshot.Name}");
        var distro = this.DataContext as Distribution;

        try
        {
            var snapshotToRemove = distro?.Snapshots.First(snap => snap.Id.Equals(snapshot.Id));
            distro?.Snapshots.Remove(snapshotToRemove!);

            this.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to delete snapshot {snapshot.Name} - Caused by exception {ex}");
        }
    }

    private async void OpenCreateDistroDialog(object sender, RoutedEventArgs args)
    {
        this.Hide();

        Log.Information("Opening dialog for distribution creation from snapshot");

        if (App.IsDistributionProcessing)
        {
            App.ShowIsProcessingDialog();
            return;
        }

        var button = sender as Button;
        var snapshot = button?.DataContext as Snapshot;

        var createDistroDialog = new CreateDistributionView
        {
            Title = $"Create distribution from snapshot \"{snapshot!.Name}\":",
            DataContext = snapshot,
            XamlRoot = App.MainWindow.Content.XamlRoot
        };
        createDistroDialog.PrimaryButtonClick += ViewModel.CreateDistroFromSnapshot;

        try
        {
            // Hide "Creation Mode" ComboBox control because we just want a distribution name
            var dialogStackPanel = createDistroDialog.Content as StackPanel;
            var creationMode = dialogStackPanel!.FindChild("DistroCreationMode") as ComboBox;
            creationMode!.Visibility = Visibility.Collapsed;

            var buttonClicked = await createDistroDialog.ShowAsync();

            if (buttonClicked != ContentDialogResult.Primary)
            {
                this.ShowAsync();
            }

        }
        catch (Exception ex)
        {
            Log.Error($"[View exception] Failed to open distribution creation dialog - Caused by exception : {ex}");
        }
    }
}
