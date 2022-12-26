using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Community.Wsl.Sdk;
using Microsoft.UI.Xaml.Controls;

namespace WSLStudio.Helpers;
internal static class WslHelper
{
    private static readonly WslApi _wslApi = new WslApi();

    public static bool CheckWsl()
    {
        if (!_wslApi.IsWslSupported() || !_wslApi.IsInstalled)
            return false;

        return true;
    }

    public static async Task ShowNoWslDialog()
    {
        ContentDialog noWslDialog = new ContentDialog();
        noWslDialog.Title = "Impossible to detect WSL";
        noWslDialog.Content = "Check if WSL is supported or installed on your system";
        noWslDialog.CloseButtonText = "Ok";
        noWslDialog.XamlRoot = App.MainWindow.Content.XamlRoot;
        await noWslDialog.ShowAsync();
        App.MainWindow.Close();
    }
}
