﻿using CommunityToolkit.Mvvm.ComponentModel;
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

    private readonly IDistributionService _distributionService;
    private readonly IInfoBarService _infoBarService;

    private bool _isDistroCreationProcessing;

    public DistrosListDetailsViewModel ( IDistributionService distributionService, IInfoBarService infoBarService)
    {
        this._distributionService = distributionService;
        this._infoBarService = infoBarService;

        this._isDistroCreationProcessing = false;

        RemoveDistroCommand = new AsyncRelayCommand<Distribution>(RemoveDistributionDialog);
        RenameDistroCommand = new AsyncRelayCommand<Distribution>(RenameDistributionDialog);
        LaunchDistroCommand = new RelayCommand<Distribution>(LaunchDistributionViewModel);
        StopDistroCommand = new RelayCommand<Distribution>(StopDistributionViewModel);
        OpenDistroFileSystemCommand = new RelayCommand<Distribution>(OpenDistributionFileSystemViewModel);
        CreateDistroCommand = new AsyncRelayCommand(CreateDistributionDialog, () => !this._isDistroCreationProcessing);

        this._distributionService.InitDistributionsList();
        this.PopulateDistributionsCollection();
    }

    public AsyncRelayCommand<Distribution> RemoveDistroCommand { get; set; }

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

    private async Task RemoveDistributionDialog(Distribution distribution)
    {
        Debug.WriteLine($"[INFO] Command called : Opening ContentDialog to remove {distribution.Name} ...");

        var dialogService = App.GetService<IDialogBuilderService>();


        var dialog = dialogService.SetTitle($"Are you sure to remove \"{distribution.Name}\" ?")
            .SetPrimaryButtonText("Remove")
            .SetCloseButtonText("Cancel")
            .SetDefaultButton(ContentDialogButton.Primary)
            .SetXamlRoot(App.MainWindow.Content.XamlRoot)
            .SetPrimaryButtonClick(ValidateDistributionName)
            .Build();

        var buttonClicked = await dialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            RemoveDistributionViewModel(distribution);
        }
    }

    private void RemoveDistributionViewModel(Distribution distribution)
    {
        Debug.WriteLine($"[INFO] Command called : Removing {distribution.Name} ...");

        this._distributionService.RemoveDistribution(distribution);
        this.Distros.Remove(distribution);

        if (!Distros.Contains(distribution))
        {
            var removeDistroInfoBar = this._infoBarService.FindInfoBar("RemoveDistroInfoSuccess");
            this._infoBarService.OpenInfoBar(removeDistroInfoBar, 2000);
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

        if (renameDistroErrorInfoBar == null)
        {
            return;
        }

        var regexItem = new Regex("^[a-zA-Z0-9-_ ]*$");

        if (string.IsNullOrWhiteSpace(newDistroName))
        {
            args.Cancel = true;
            renameDistroErrorInfoBar.Message = "You cannot set an empty distribution name.";
            renameDistroErrorInfoBar.IsOpen = true;
        }

        else if (newDistroName.Any(char.IsWhiteSpace))
        {
            args.Cancel = true;
            renameDistroErrorInfoBar.Message = "You cannot set a new distribution name with white spaces.";
            renameDistroErrorInfoBar.IsOpen = true;
        }

        else if (newDistroName.Length is <= 2 or > 30)
        {
            args.Cancel = true;
            renameDistroErrorInfoBar.Message = "You cannot set a new distribution name" +
                                               " with a length shorter than 2 characters or longer than 30 characters.";
            renameDistroErrorInfoBar.IsOpen = true;
        }

        else if (!regexItem.IsMatch(newDistroName))
        {
            args.Cancel = true;
            renameDistroErrorInfoBar.Message = "You cannot set a new distribution name with special characters.";
            renameDistroErrorInfoBar.IsOpen = true;
        }

        else if (namesList.Contains(newDistroName))
        {
            args.Cancel = true;
            renameDistroErrorInfoBar.Message = "You cannot set a new distribution name with an existing one.";
            renameDistroErrorInfoBar.IsOpen = true;
        }
        else
        {
            renameDistroErrorInfoBar.IsOpen = false;
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


        var dialog = dialogService.SetTitle($"Rename \"{distribution.Name}\" :")
            .AddContent(newDistroNameInput)
            .AddContent(renameDistroErrorInfoBar)
            .SetPrimaryButtonText("Rename")
            .SetCloseButtonText("Cancel")
            .SetDefaultButton(ContentDialogButton.Primary)
            .SetXamlRoot(App.MainWindow.Content.XamlRoot)
            .SetPrimaryButtonClick(ValidateDistributionName)
            .Build();

        var buttonClicked = await dialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            RenameDistributionViewModel(distribution, newDistroNameInput.Text);
        }
    }

    private void RenameDistributionViewModel(Distribution distribution, string newDistroName)
    {
        Debug.WriteLine($"[INFO] Renaming {distribution.Name} for {newDistroName}");

        var isDistroRenamed = this._distributionService.RenameDistribution(distribution, newDistroName);
        if (!isDistroRenamed)
        {
            return;
        }

        var index = this.Distros.ToList().FindIndex(distro => distro.Name == distribution.Name);
        if (index != -1)
        {
            this.Distros.ElementAt(index).Name = newDistroName;
        }
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

        var dialog = dialogService.SetTitle("Create distribution :")
            .AddContent(createDistroDialog)
            .SetPrimaryButtonText("Create")
            .SetCloseButtonText("Cancel")
            .SetDefaultButton(ContentDialogButton.Primary)
            .SetPrimaryButtonClick(ValidateCreationMode)
            .SetXamlRoot(App.MainWindow.Content.XamlRoot)
            .Build();

        var buttonClicked = await dialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            var (distroName, resourceOrigin) = this.GetDistributionCreationInfos(dialog);

            await CreateDistributionViewModel(distroName, resourceOrigin);
        }
    }

    private void ValidateCreationMode(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        this.ValidateDistributionName(sender, args);

        var dialogContent = sender.Content as StackPanel;
        var contentContainer = dialogContent!.Children.First() as UserControl;
        var form = contentContainer!.Content as StackPanel;

        var creationModeErrorInfoBar = form!.FindName("CreationModeErrorInfoBar") as InfoBar;

        var creationMode = form.FindName("CreationMode") as ComboBox;

        if (creationMode?.SelectedItem != null)
        {
            creationModeErrorInfoBar!.IsOpen = false;
            return;
        }

        args.Cancel = true;
        creationModeErrorInfoBar!.IsOpen = true;
    }

    // return a tuple composed of the distro name and the resource origin (file/folder path or docker hub link)
    private Tuple<string, string> GetDistributionCreationInfos(ContentDialog dialog)
    {

        var dialogContent = dialog.Content as StackPanel;
        var contentContainer = dialogContent!.Children.First() as UserControl;
        var form = contentContainer!.Content as StackPanel;

        var resourceOrigin = "";

        var creationMode = form.FindName("CreationMode") as ComboBox;
        TextBox? inputTextBox;
        switch (creationMode?.SelectedItem.ToString())
        {
            case "Dockerfile":
                inputTextBox = form.FindName("DockerfileInput") as TextBox;
                resourceOrigin = inputTextBox!.Text;
                break;
            case "Docker Hub":
                inputTextBox = form.FindName("DockerHubInput") as TextBox;
                resourceOrigin = inputTextBox!.Text;
                break;
            case "Archive":
                inputTextBox = form.FindName("ArchiveInput") as TextBox;
                resourceOrigin = inputTextBox!.Text;
                break;
        }

        var distroNameInput = form?.FindName("distroNameInput") as TextBox;
        var distroName = distroNameInput!.Text;

        return Tuple.Create(distroName, resourceOrigin);
    }

    private async Task CreateDistributionViewModel(string distroName, string resourceOrigin)
    {
        const double memoryLimit = 4.0;
        const int processorLimit = 2;

        this._isDistroCreationProcessing = true;

        var createNewDistroInfoProgress = this._infoBarService.FindInfoBar("CreateNewDistroInfoProgress");
        this._infoBarService.OpenInfoBar(createNewDistroInfoProgress);

        var newDistro = await this._distributionService.CreateDistribution(distroName, memoryLimit, processorLimit, resourceOrigin);
        if (newDistro != null)
        {
            this._isDistroCreationProcessing = false;

            this._infoBarService.CloseInfoBar(createNewDistroInfoProgress);

            var createNewDistroInfoSuccess = this._infoBarService.FindInfoBar("CreateNewDistroInfoSuccess");
            this._infoBarService.OpenInfoBar(createNewDistroInfoSuccess, 2000);

            this.Distros.Add(newDistro);
        }
        else
        {
            this._isDistroCreationProcessing = false;

            this._infoBarService.CloseInfoBar(createNewDistroInfoProgress);

            var createNewDistroInfoError = this._infoBarService.FindInfoBar("CreateNewDistroInfoError");
            this._infoBarService.OpenInfoBar(createNewDistroInfoError, 5000);
        }
    }
}
