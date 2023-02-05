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
using System.Xml.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using WSLStudio.Views;
using Microsoft.UI.Xaml.Media;
using WSLStudio.Helpers;

namespace WSLStudio.ViewModels;

public class DistrosListDetailsViewModel : ObservableObject
{
    private static InfoBar? _infoBar;
    private static Timer? _timer;

    private readonly IDistributionService _distributionService;

    public DistrosListDetailsViewModel ( IDistributionService distributionService )
    {
        this._distributionService = distributionService;

        RemoveDistroCommand = new RelayCommand<Distribution>(RemoveDistributionViewModel);
        RenameDistroCommand = new AsyncRelayCommand<Distribution>(RenameDistributionDialog);
        LaunchDistroCommand = new RelayCommand<Distribution>(LaunchDistributionViewModel);
        StopDistroCommand = new RelayCommand<Distribution>(StopDistributionViewModel);
        OpenDistroFileSystemCommand = new RelayCommand<Distribution>(OpenDistributionFileSystemViewModel);
        CreateDistroCommand = new AsyncRelayCommand(CreateDistributionDialog);

        this._distributionService.InitDistributionsList();
        this.PopulateDistributionsCollection();
    }

    public RelayCommand<Distribution> RemoveDistroCommand { get; set; }

    public AsyncRelayCommand<Distribution> RenameDistroCommand { get; set; }

    public RelayCommand<Distribution> LaunchDistroCommand { get; set; }

    public RelayCommand<Distribution> StopDistroCommand { get; set; }

    public RelayCommand<Distribution> OpenDistroFileSystemCommand { get; set; }

    public AsyncRelayCommand CreateDistroCommand { get; set; }

    public ObservableCollection<Distribution> Distros { get; set; } = new();


    private void PopulateDistributionsCollection()
    {
        try
        {
            Debug.WriteLine($"[INFO] Populate distributions collection");
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
            var appGrid = appPage.Content as Grid;
            _infoBar = appGrid.FindName(infoBarName) as InfoBar;

            _timer = new Timer(2000); // 2000 milliseconds = 2 seconds
            _timer.Elapsed += CloseInfoBar;
        }

        lock (_infoBar)
        {
            _infoBar.IsOpen = true;
            _timer.Start();
        }
    }

    private void RemoveDistributionViewModel(Distribution distribution)
    {
        Debug.WriteLine($"[INFO] Command called : Removing {distribution.Name} ...");

        this._distributionService.RemoveDistribution(distribution);
        this.Distros.Remove(distribution);

        if (!Distros.Contains(distribution))
        {
            OpenInfoBar("RemoveDistroInfoSuccess");
        }
    }

    // Check if the new distribution has valid characters 
    private void ValidateDistributionName(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var dialogContent = sender.Content as StackPanel;
        var contentForm = new StackPanel();

        if (dialogContent?.Children.First() is UserControl)
        {
            var contentContainer = dialogContent.Children.First() as UserControl;
            contentForm = contentContainer?.Content as StackPanel;
        }

        else
        {
            contentForm = dialogContent;
        }

        var newDistroNameInput = contentForm?.FindName("distroNameInput") as TextBox;
        var newDistroName = newDistroNameInput?.Text;

        var namesList = this.Distros.Select(distro => distro.Name).ToList();

        var renameDistroErrorInfoBar = contentForm?.FindName("DistroNameErrorInfoBar") as InfoBar;

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

            else if (namesList.Contains(newDistroName))
            {
                args.Cancel = true;
                renameDistroErrorInfoBar.Message = "You cannot set a new distribution name with an existing one.";
            }
        }

    }

    private async Task RenameDistributionDialog(Distribution distribution)
    {
        Debug.WriteLine($"[INFO] Command called : Opening ContentDialog to rename {distribution.Name} ...");

        var dialogService = App.GetService<IDialogBuilderService>();

        var newDistroNameInput = new TextBox()
        {
            Name = "distroNameInput",
            Margin = new Thickness(0 ,20, 0, 15),
            Height = 32,
        };

        var renameDistroErrorInfoBar = new InfoBar()
        {
            Name = "DistroNameErrorInfoBar",
            Severity = InfoBarSeverity.Error,
            Title = "Invalid Distribution Name",
            IsOpen = false,
            IsClosable = false,
        };


        var contentDialog = dialogService.SetTitle($"Rename \"{distribution.Name}\" :")
            .AddContent(newDistroNameInput)
            .AddContent(renameDistroErrorInfoBar)
            .SetPrimaryButtonText("Rename")
            .SetCloseButtonText("Cancel")
            .SetDefaultButton(ContentDialogButton.Primary)
            .SetXamlRoot(App.MainWindow.Content.XamlRoot)
            .SetPrimaryButtonClick(ValidateDistributionName)
            .Build();

        var buttonClicked = await contentDialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            RenameDistributionViewModel(distribution, newDistroNameInput.Text);
        }
    }

    private void RenameDistributionViewModel(Distribution distribution, string newDistroName)
    {
        Debug.WriteLine($"[INFO] Renaming {distribution.Name} for {newDistroName}");

        var index = this.Distros.ToList().FindIndex(distro => distro.Name == distribution.Name);
        if (index != -1)
        {
            this.Distros.ElementAt(index).Name = newDistroName;
        }

        this._distributionService.RenameDistribution(distribution);
        
    }

    private void LaunchDistributionViewModel(Distribution distribution)
    {
        Debug.WriteLine($"[INFO] Command called : ${distribution!.Name} distribution is launching ...");

        this._distributionService.LaunchDistribution(distribution);
        // Publish message  (allows us to show the stop button when the start button is clicked)
        WeakReferenceMessenger.Default.Send(new ShowDistroStopButtonMessage(distribution));
    }

    private void StopDistributionViewModel(Distribution distribution)
    {
        Debug.WriteLine($"[INFO] Command called : {distribution!.Name} distribution is stopping ...");

        this._distributionService.StopDistribution(distribution);
        WeakReferenceMessenger.Default.Send(new HideDistroStopButtonMessage(distribution));
    }

    private void OpenDistributionFileSystemViewModel(Distribution distribution)
    {
        Debug.WriteLine($"[INFO] Command called : {distribution.Name} file system is opening ...");

        this._distributionService.OpenDistributionFileSystem(distribution);
    }

    private async Task CreateDistributionDialog()
    {
        Debug.WriteLine($"[INFO] Command called : Opening ContentDialog for distribution creation");

        var dialogService = App.GetService<IDialogBuilderService>();

        // contentdialog content set in CreateDistroDialog.xaml
        var createDistroDialog = new CreateDistroDialog();

        var contentDialog = dialogService.SetTitle("Add distribution :")
            .AddContent(createDistroDialog)
            .SetPrimaryButtonText("Create")
            .SetCloseButtonText("Cancel")
            .SetDefaultButton(ContentDialogButton.Primary)
            .SetPrimaryButtonClick(CreateDistribution)
            .SetXamlRoot(App.MainWindow.Content.XamlRoot)
            .Build();

        var buttonClicked = await contentDialog.ShowAsync();

    }

    private async void CreateDistribution(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {

        this.ValidateDistributionName(sender, args);

        var resourceOrigin = "";

        var dialogContent = sender.Content as StackPanel;
        var contentContainer = dialogContent!.Children.First() as UserControl;
        var contentForm = contentContainer!.Content as StackPanel;

        var renameDistroErrorInfoBar = contentForm?.FindName("DistroNameErrorInfoBar") as InfoBar;

        if (contentForm != null)
        {
            var creationMode = contentForm.FindName("CreationMode") as ComboBox;

            if (creationMode?.SelectedItem == null)
            {
                args.Cancel = true;
                renameDistroErrorInfoBar.Message = "No creation mode has been selected.";
                return;
            }

            TextBox? inputTextBox;
            switch (creationMode?.SelectedItem.ToString())
            {
                case "Dockerfile":
                    inputTextBox = contentForm.FindName("DockerfileInput") as TextBox;
                    resourceOrigin = inputTextBox?.Text;
                    break;
                case "Docker Hub":
                    inputTextBox = contentForm.FindName("DockerHubInput") as TextBox;
                    resourceOrigin = inputTextBox?.Text;
                    break;
                case "Archive":
                    inputTextBox = contentForm.FindName("ArchiveInput") as TextBox;
                    resourceOrigin = inputTextBox?.Text;
                    break;
            }

            if (resourceOrigin != null)
            {
                var distroNameInput = contentForm?.FindName("distroNameInput") as TextBox;
                var distroName = distroNameInput.Text;
                await this.CreateDistributionViewModel(distroName, resourceOrigin);
            }
        }
    }

    private async Task CreateDistributionViewModel(string distroName, string resourceOrigin)
    {
        var memoryLimit = 4.0;
        var processorLimit = 2;
        //var resourceOrigin = "C:\\Users\\nathan\\Documents\\wsl-studioDEV\\";

        var newDistro = await this._distributionService.CreateDistribution(distroName, memoryLimit, processorLimit, resourceOrigin);
        this.Distros.Add(newDistro);

    }
}
