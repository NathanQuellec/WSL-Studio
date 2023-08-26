using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Input;
using WSLStudio.Helpers;
using WSLStudio.Models;
using Path = System.IO.Path;
using WSLStudio.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WSLStudio.Views.Dialogs;
public sealed partial class DisplaySnapshotsView : ContentDialog
{

    public DisplaySnapshotsVM ViewModel { get; set; }

    public DisplaySnapshotsView()
    {
        this.InitializeComponent();
        this.ViewModel = App.GetService<DisplaySnapshotsVM>();
    }

    private void  OpenSnapshotsFolder(object sender, RoutedEventArgs args)
    {
        var distribution = this.DataContext as Distribution;
        var snapshotsFolderPath = Path.Combine(distribution!.Path, "snapshots");

        try
        {
            var process = new ProcessBuilderHelper("explorer.exe")
                .SetArguments(snapshotsFolderPath)
                .Build();
            process.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async void OpenDeleteSnapshotDialog(object sender, RoutedEventArgs args)
    {
        this.Hide();

        var deleteSnapshotDialog = new ContentDialog()
        {
            Title = "Are you sure to delete this snapshot ?",
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DataContext = (sender as Button)?.DataContext,
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
                DeleteSnapshot(sender, args);
            }
            else
            {
                this.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void DeleteSnapshot(object sender, RoutedEventArgs args)
    {
        var distro = this.DataContext as Distribution;
        var snapshotId = (sender as Button)!.Tag.ToString();
        try
        {
            var snapshotToRemove = distro?.Snapshots.First(snapshot => snapshot.Id.ToString() == snapshotId);
            distro?.Snapshots.Remove(snapshotToRemove!);

            this.ShowAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async void OpenCreateDistroDialog(object sender, RoutedEventArgs args)
    {
        this.Hide();

        var button = sender as Button;
        var snapshot = button.DataContext as Snapshot;

        var stackPanel = new StackPanel();

        var distroNameInput = new TextBox()
        {
            Name = "DistroNameInput",
            Header = "Distribution Name",
            Margin = new Thickness(0,8,0,0),
        };

        stackPanel.Children.Add(distroNameInput);

        var createDistroDialog = new ContentDialog()
        {
            Title = "Create Distribution From Snapshot :",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = stackPanel,
            DataContext = snapshot,
            XamlRoot = App.MainWindow.Content.XamlRoot,
        };


        createDistroDialog.PrimaryButtonClick += ViewModel.CreateDistroFromSnapshot;

        var buttonClicked = await createDistroDialog.ShowAsync();

        if (buttonClicked != ContentDialogResult.Primary)
        {
            this.ShowAsync();
        }
    }
}
