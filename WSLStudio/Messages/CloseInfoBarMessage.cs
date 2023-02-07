using Docker.DotNet.Models;
using Microsoft.UI.Xaml.Controls;
using WSLStudio.Models;

namespace WSLStudio.Messages;

public class CloseInfoBarMessage
{
    public InfoBar InfoBar { get; }

    public CloseInfoBarMessage(InfoBar infoBar)
    {
        this.InfoBar = infoBar;
    }
}