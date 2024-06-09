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

        Log.Information("[PUB/SUB] Message received to update progress bar advancement status");

        WeakReferenceMessenger.Default.Register<DistroProgressBarMessage>(this, (recipient, message) =>
        {
            Log.Information("");
            CreateDistroInfoProgress.Title = message.ProgressInfo;
        });

        WeakReferenceMessenger.Default.Register<SnapshotProgressBarMessage>(this, (recipient, message) =>
        {
            Log.Information("");
            CreateSnapshotInfoProgress.Title = message.ProgressInfo;
        });
    }
}
