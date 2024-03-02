using System.Threading;
using System.Timers;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Controls;
using WSLStudio.Contracts.Services;
using WSLStudio.Messages;
using WSLStudio.Views.UserControls;
using Timer = System.Timers.Timer;

namespace WSLStudio.Services;

public class InfoBarService : IInfoBarService
{
    public InfoBar FindInfoBar(string infoBarName)
    {
        var appFrame = App.MainWindow.Content as Frame;
        if (appFrame == null)
        {
            throw new Exception("Could not find App Frame.");
        }
        var infoBar = appFrame.FindChild(infoBarName) as InfoBar;
        if (infoBar == null)
        {
            throw new Exception("Could not find InfoBar.");
        }

        return infoBar;
    }

    // Send a message to the view to close the InfoBar
    private static void StopTimerInfoBar(object sender, ElapsedEventArgs e, InfoBar infoBar)
    {
        var timer = sender as Timer;
        WeakReferenceMessenger.Default.Send(new CloseInfoBarMessage(infoBar));
        timer?.Stop();
    }

    public void OpenInfoBar(InfoBar infoBar)
    {
        infoBar.IsOpen = true;
    }

    public void OpenInfoBar(InfoBar infoBar, double time)
    {
        var timer = new Timer(time);
        timer.Elapsed += (sender, e) => StopTimerInfoBar(sender, e, infoBar);
        infoBar.IsOpen = true;
        timer.Start();
    }

    public void OpenInfoBar(InfoBar infoBar, string message, double time)
    {
        var timer = new Timer(time);
        timer.Elapsed += (sender, e) => StopTimerInfoBar(sender, e, infoBar);
        infoBar.Message = message;
        infoBar.IsOpen = true;
        timer.Start();
    }

    public void CloseInfoBar(InfoBar infoBar)
    {
        infoBar.IsOpen = false;
    }
}