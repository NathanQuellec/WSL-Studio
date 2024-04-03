using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using WSLStudio.Messages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WSLStudio.Views.UserControls;
public sealed partial class WslProcessInfoBar : UserControl
{
    public WslProcessInfoBar()
    {
        this.InitializeComponent();

        WeakReferenceMessenger.Default.Register<ProgressBarMessage>(this, (recipient, message) =>
        {
            Log.Information("");
            CreateDistroInfoProgress.Message = message.ProgressInfo;
        });
    }
}
