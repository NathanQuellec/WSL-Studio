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

    #region MyRegion

    public RelayCommand<Distribution> OpenSnapshotsFolderCommand { get; set; }

    public RelayCommand DeleteSnapshotCommand { get; set; }

    #endregion

    public DisplaySnapshotsView()
    {
        this.InitializeComponent();
        OpenSnapshotsFolderCommand = new RelayCommand<Distribution>(OpenSnapshotsFolder);
        DeleteSnapshotCommand = new RelayCommand(DeleteSnapshot);
    }

    private void  OpenSnapshotsFolder(Distribution distribution)
    {
        var snapshotsFolderPath = Path.Combine(distribution.Path, "snapshots");

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

    public void DeleteSnapshot()
    {
        Console.WriteLine("Delete snapshot");
    }
}
