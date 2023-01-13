using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Timers;
using Windows.System;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WSLStudio.Messages;
using WSLStudio.Services;
using Microsoft.UI.Xaml.Input;
using Timer = System.Timers.Timer;

namespace WSLStudio.ViewModels;

public class DistrosListDetailsViewModel : ObservableObject
{

    private static StackPanel? _stackPanel;
    private static InfoBar? _infoBar;
    private static Timer? _timer;

    private readonly IDistributionService _distributionService;
    private readonly IDialogBuilderService _dialogBuilderService;

    public DistrosListDetailsViewModel ( IDistributionService distributionService,
                                         IDialogBuilderService dialogBuilderService )
    {
        this._distributionService = distributionService;
        this._dialogBuilderService = dialogBuilderService;

        RemoveDistroCommand = new RelayCommand<Distribution>(RemoveDistributionViewModel);
        RenameDistroCommand = new AsyncRelayCommand<Distribution>(RenameDistributionDialog);
        LaunchDistroCommand = new RelayCommand<Distribution>(LaunchDistributionViewModel);
        StopDistroCommand = new RelayCommand<Distribution>(StopDistributionViewModel);
        OpenDistroFileSystemCommand = new RelayCommand<Distribution>(OpenDistributionFileSystemViewModel);

        this._distributionService.InitDistributionsList();
        this.PopulateDistributionsCollection();
        _dialogBuilderService = dialogBuilderService;
    }

    public RelayCommand<Distribution> RemoveDistroCommand { get; set; }

    public AsyncRelayCommand<Distribution> RenameDistroCommand { get; set; }

    public RelayCommand<Distribution> LaunchDistroCommand { get; set; }

    public RelayCommand<Distribution> StopDistroCommand { get; set; }

    public RelayCommand<Distribution> OpenDistroFileSystemCommand { get; set; }

    public ObservableCollection<Distribution> Distros { get; set; } = new();

    // Send a message to the view to close the InfoBar
    private static void CloseInfoBar(object sender, ElapsedEventArgs e)
    {
        lock (_infoBar)
        {
            WeakReferenceMessenger.Default.Send(new CloseInfoBarMessage());
            _timer.Stop();
        }
    }

    // Open an InfoBar that closes after 2 seconds
    private static void OpenInfoBar(string infoBarName)
    {
        if (_infoBar == null)
        {
            var appFrame = App.MainWindow.Content as Frame;
            var appPage = appFrame.Content as Page;
            var appDockPanel = appPage.Content as DockPanel;
            _infoBar = appDockPanel.FindName(infoBarName) as InfoBar;

            _timer = new Timer(2000); // 2000 milliseconds = 2 seconds
            _timer.Elapsed += CloseInfoBar;
        }

        lock (_infoBar)
        {
            _infoBar.IsOpen = true;
            _timer.Start();
        }
    }

    private void RemoveDistributionViewModel(Distribution? distribution)
    {
        Debug.WriteLine($"[INFO] Command called : Removing ${distribution} ...");

        if (distribution == null)
        {
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the XAML source");
        }
        else
        {
            this._distributionService.RemoveDistribution(distribution);
            this.Distros.Remove(distribution);
            OpenInfoBar("RemoveDistroSuccess");
        }
    }

    // Check if the new distribution has valid characters 
    private void ValidateDistributionName(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var dialogContent = sender.Content as StackPanel;

        var newDistroNameInput = dialogContent?.Children.First() as TextBox;
        var newDistroName = newDistroNameInput?.Text;

        var renameDistroErrorInfoBar = dialogContent?.Children.Last() as InfoBar;

        if (renameDistroErrorInfoBar != null)
        {
            renameDistroErrorInfoBar.IsOpen = true;

            var regexItem = new Regex("^[a-zA-Z0-9-_ ]*$");


            if (string.IsNullOrWhiteSpace(newDistroName))
            {
                args.Cancel = true;
                renameDistroErrorInfoBar.Message = "You cannot set an empty distribution name.";
            }

            else if (newDistroName.Any(char.IsWhiteSpace))
            {
                args.Cancel = true;
                renameDistroErrorInfoBar.Message = "You cannot set a new distribution name with white spaces.";
            }

            else if (newDistroName.Length is <= 2 or > 30)
            {
                args.Cancel = true;
                renameDistroErrorInfoBar.Message = "You cannot set a new distribution name" +
                                                   " with a length shorter than 2 characters or longer than 30 characters.";
            }

            else if (!regexItem.IsMatch(newDistroName))
            {
                args.Cancel = true;
                renameDistroErrorInfoBar.Message = "You cannot set a new distribution name with special characters.";
            }
        }

        this._dialogBuilderService.SetContent(_stackPanel);
    }

    private async Task RenameDistributionDialog(Distribution? distribution)
    {
        Debug.WriteLine($"[INFO] Command called : Opening ContentDialog to rename ${distribution.Name} ...");

        var newDistroName = new TextBox()
        {
            Margin = new Thickness(0 ,20, 0, 15),
            Height = 32,
        };

        var renameDistroErrorInfoBar = new InfoBar()
        {
            Severity = InfoBarSeverity.Error,
            Title = "Invalid Distribution Name",
            IsOpen = false,
            IsClosable = false,
            Visibility = Visibility.Visible,
        };

        _stackPanel = new StackPanel()
        {
            Children =
            {
                newDistroName,
                renameDistroErrorInfoBar,
            },
        };

        var contentDialog = this._dialogBuilderService.SetTitle($"Rename \"{distribution.Name}\" :")
            .SetContent(_stackPanel)
            .SetPrimaryButtonText("Rename")
            .SetCloseButtonText("Cancel")
            .SetDefaultButton(ContentDialogButton.Primary)
            .SetXamlRoot(App.MainWindow.Content.XamlRoot)
            .SetPrimaryButtonClick(ValidateDistributionName)
            .Build();

        var buttonClicked = await contentDialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            RenameDistributionViewModel(distribution, newDistroName.Text);
        }
    }

    private void RenameDistributionViewModel(Distribution distribution, string newDistroName)
    {
        Debug.WriteLine($"[INFO] Renaming {distribution.Name} for {newDistroName}");

        if (distribution == null)
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the XAML source");

        else
        {
            int index = this.Distros.ToList().FindIndex(distro => distro.Name == distribution.Name);
            if (index != -1)
            {
                this.Distros.ElementAt(index).Name = newDistroName;
            }

            this._distributionService.RenameDistribution(distribution);
        }
    }

    private void LaunchDistributionViewModel(Distribution? distribution)
    {
        Debug.WriteLine($"[INFO] Command called : ${distribution} distribution is launching ...");

        if (distribution == null)
        {
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the XAML source");
        }
        else
        {
            this._distributionService.LaunchDistribution(distribution);
            // Publish message  (allows us to show the stop button when the start button is clicked)
            WeakReferenceMessenger.Default.Send(new ShowDistroStopButtonMessage(distribution));
        }
    }

    private void StopDistributionViewModel(Distribution? distribution)
    {
        Debug.WriteLine($"[INFO] Command called : ${distribution} distribution is stopping ...");

        if (distribution == null)
        {
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the XAML source");
        }
        else
        {
            this._distributionService.StopDistribution(distribution);
            WeakReferenceMessenger.Default.Send(new HideDistroStopButtonMessage(distribution));
        }
    }

    private void OpenDistributionFileSystemViewModel(Distribution? distribution)
    {
        Debug.WriteLine($"[INFO] Command called : ${distribution} file system is opening ...");

        if (distribution == null)
        {
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the XAML source");
        }
        else
        {
            this._distributionService.OpenDistributionFileSystem(distribution);
        }
    }

    private void PopulateDistributionsCollection()
    {
        try
        {
            this.Distros.Clear();
            foreach (var distro in this._distributionService.GetAllDistributions())
            {
                this.Distros.Add(distro);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

}
