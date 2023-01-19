// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WSLStudio.Views
{
    public sealed partial class CreateDistroDialog : UserControl
    {

        public CreateDistroDialog()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private void CreateDistro_SelectionMode(object sender, SelectionChangedEventArgs e)
        {


            string creationMode = e.AddedItems[0].ToString();

        }

        public async void OpenExplorer(object sender, RoutedEventArgs args)
        {

            var hwnd = App.MainWindow.GetWindowHandle();
            FolderPicker folderPicker = new();
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");


            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                string folderName = folder.Name;
                string folderPath = folder.Path;
            }
        }
    }
}
