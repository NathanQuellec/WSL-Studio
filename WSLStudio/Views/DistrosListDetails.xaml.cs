// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI.Controls;
using WSLStudio.Messages;
using WSLStudio.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WSLStudio.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DistrosListDetails : Page, IRecipient<ShowStopButtonMessage>
{
    public DistrosListDetailsViewModel ViewModel {
        get;
    }

    public DistrosListDetails()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<DistrosListDetailsViewModel>();
        WeakReferenceMessenger.Default.Register<ShowStopButtonMessage>(this);
    }


    public void Receive(ShowStopButtonMessage message)
    {
        //Button stopButton = this.FindName("StopButton") as Button;

        //stopButton.Visibility = Visibility.Collapsed;
    }
}
