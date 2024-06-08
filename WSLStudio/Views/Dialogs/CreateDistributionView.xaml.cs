// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WSLStudio.Views.Dialogs;

public sealed partial class CreateDistributionView : ContentDialog
{
    public CreateDistributionView()
    {
        this.InitializeComponent();

    }

    public async void PickDockerFileFolder(object sender, RoutedEventArgs args)
    {
        var hwnd = App.MainWindow.GetWindowHandle();
        FolderPicker folderPicker = new();

        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        folderPicker.FileTypeFilter.Add("*");


        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            DockerfileInput.Text = folder.Path;
        }
    }


    public async void PickArchiveFile(object sender, RoutedEventArgs args)
    {

        var hwnd = App.MainWindow.GetWindowHandle();
        FileOpenPicker filePicker = new();

        WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

        filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        filePicker.FileTypeFilter.Add(".tar");
        filePicker.FileTypeFilter.Add(".gz");

        var archiveFile = await filePicker.PickSingleFileAsync();
        if (archiveFile != null)
        {
            ArchiveInput.Text = archiveFile.Path;
        }
    }

    public async void PickVhdxFile(object sender, RoutedEventArgs args)
    {

        var hwnd = App.MainWindow.GetWindowHandle();
        FileOpenPicker filePicker = new();

        WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

        filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        filePicker.FileTypeFilter.Add(".vhdx");

        var vhdxFile = await filePicker.PickSingleFileAsync();
        if (vhdxFile != null)
        {
            VhdxInput.Text = vhdxFile.Path;
        }
    }
}
