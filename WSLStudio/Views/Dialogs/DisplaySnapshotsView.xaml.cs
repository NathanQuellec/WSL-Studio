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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WSLStudio.Views.Dialogs;
public sealed partial class DisplaySnapshotsView : ContentDialog
{
    public DisplaySnapshotsView()
    {
        this.InitializeComponent();
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

    public void DeleteSnapshot(object sender, RoutedEventArgs args)
    {
        var distro = this.DataContext as Distribution;
        var snapshotId = (sender as Button)!.Tag.ToString();
        try
        {
            var snapshotToRemove = distro?.Snapshots.First(snapshot => snapshot.Id.ToString() == snapshotId);
            distro?.Snapshots.Remove(snapshotToRemove!);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
