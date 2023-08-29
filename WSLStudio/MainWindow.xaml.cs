using WSLStudio.Helpers;

namespace WSLStudio;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        Content = null;
        ExtendsContentIntoTitleBar = true;
    }
}
