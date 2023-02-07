using System.Threading;
using System.Timers;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using WSLStudio.Contracts.Services;
using WSLStudio.Messages;
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
        var appPage = appFrame.Content as Page;
        if (appPage == null)
        {
            throw new Exception("Could not find App Page.");
        }
        var infoBarContainer = appPage.Content as Grid;
        if (infoBarContainer == null)
        {
            throw new Exception("Could not find InfoBar Container.");
        }
        var infoBar = infoBarContainer.FindName(infoBarName) as InfoBar;
        if (infoBar == null)
        {
            throw new Exception("Could not find InfoBar.");
        }

        return infoBar;
    }

    // Send a message to the view to close the InfoBar
    private void StopTimerInfoBar(object sender, ElapsedEventArgs e, InfoBar infoBar)
    {
        var timer = sender as Timer;
        WeakReferenceMessenger.Default.Send(new CloseInfoBarMessage(infoBar));
        timer?.Stop();
    }

    public void OpenInfoBar(InfoBar infoBar, double time)
    {
        var timer = new Timer(time);
        timer.Elapsed += (sender, e) => StopTimerInfoBar(sender, e, infoBar);
        infoBar.IsOpen = true;
        timer.Start();
    }

    public void OpenInfoBar(InfoBar infoBar)
    {
        infoBar.IsOpen = true;
    }

    public void CloseInfoBar(InfoBar infoBar)
    {
        infoBar.IsOpen = false;
    }
}