// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.Mvvm.Messaging;
using WSLStudio.Messages;
using WSLStudio.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WSLStudio.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DistrosListDetailsView : Page
{
    private Button _distroStopButton = new();

    public DistrosListDetailsViewModel ViewModel { get; } =  App.GetService<DistrosListDetailsViewModel>();

    public DistrosListDetailsView()
    {
        this.InitializeComponent();

        WeakReferenceMessenger.Default.Register<ShowDistroStopButtonMessage>(this, (recipient, message) =>
        {
            var distro = message.distribution;
            FindDistroStopButton(this, distro.Name);
            if (_distroStopButton != null)
            {
                _distroStopButton.Visibility = Visibility.Visible;
            }
        });

        WeakReferenceMessenger.Default.Register<HideDistroStopButtonMessage>(this, (recipient, message) =>
        {
            var distro = message.distribution;
            FindDistroStopButton(this, distro.Name);
            if (_distroStopButton != null)
            {
                _distroStopButton.Visibility = Visibility.Collapsed;
            }
        });

        //Close InfoBar after timer set in DistroListDetailsViewModel.cs:RemoveSuccessInfoBar()
        WeakReferenceMessenger.Default.Register<CloseInfoBarMessage>(this, (recipient, message) =>
        {
            var dispatcher = DispatcherQueue.TryEnqueue(() => RemoveDistroInfoSuccess.IsOpen = false);
        });
    }

    /*
     * We need to go through the Visual Tree recursively to find the Tag that matches the Distro Name received,
     * as we cannot set a dynamic x:Name property for the Stop button.
     */
    public void FindDistroStopButton(DependencyObject parent, string searchDistroName)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var currentChild = VisualTreeHelper.GetChild(parent, i);
            if (currentChild != null && currentChild is Button)
            {
                var btn = (Button)currentChild;
                if ((string)btn.Tag == $"Stop_{searchDistroName}")
                {
                    _distroStopButton = btn;
                }
            }
            FindDistroStopButton(currentChild, searchDistroName);
        }
    }
}
