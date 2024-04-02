using Microsoft.UI.Xaml.Controls;

namespace WSLStudio.Messages;

public class CloseInfoBarMessage
{
    public InfoBar InfoBar
    {
        get;
    }

    public CloseInfoBarMessage(InfoBar infoBar)
    {
        this.InfoBar = infoBar;
    }
}