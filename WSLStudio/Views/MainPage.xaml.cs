using Microsoft.UI.Xaml.Controls;

using WSLStudio.ViewModels;

namespace WSLStudio.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }
}
