﻿using System.Collections.ObjectModel;
using System.Net.Mime;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WSLStudio.Contracts.Services;
using WSLStudio.Helpers;
using WSLStudio.Messages;
using WSLStudio.Models;
using WSLStudio.Views;
using WSLStudio.Views.Dialogs;

namespace WSLStudio.ViewModels;

public class DistrosListDetailsVM : ObservableObject
{

    private readonly IDistributionService _distributionService;
    private readonly ISnapshotService _snapshotService;
    private readonly IInfoBarService _infoBarService;

    #region RelayCommand

    public AsyncRelayCommand<Distribution> RemoveDistroCommand { get; set; }

    public AsyncRelayCommand<Distribution> RenameDistroCommand { get; set; }

    public RelayCommand<Distribution> LaunchDistroCommand { get; set; }

    public RelayCommand<Distribution> StopDistroCommand { get; set; }

    public RelayCommand<Distribution> OpenDistroWithFileExplorerCommand { get; set; }

    public RelayCommand<Distribution> OpenDistroWithVsCodeCommand { get; set; }

    public RelayCommand<Distribution> OpenDistroWithWinTermCommand { get; set; }

    public AsyncRelayCommand CreateDistroCommand { get; set; }

    public AsyncRelayCommand<Distribution> CreateSnapshotCommand { get; set; }

    public AsyncRelayCommand<Distribution> DisplaySnapshotsListCommand { get; set; }

    #endregion

    public ObservableCollection<Distribution> Distros { get; set; } = new();

    public DistrosListDetailsVM(IDistributionService distributionService, ISnapshotService snapshotService, IInfoBarService infoBarService)
    {
        _distributionService = distributionService;
        _snapshotService = snapshotService;
        _infoBarService = infoBarService;

        RemoveDistroCommand = new AsyncRelayCommand<Distribution>(RemoveDistributionDialog);
        RenameDistroCommand = new AsyncRelayCommand<Distribution>(RenameDistributionDialog);
        LaunchDistroCommand = new RelayCommand<Distribution>(LaunchDistributionViewModel);
        StopDistroCommand = new RelayCommand<Distribution>(StopDistributionViewModel);
        OpenDistroWithFileExplorerCommand = new RelayCommand<Distribution>(OpenDistributionWithFileExplorerViewModel);
        OpenDistroWithVsCodeCommand = new RelayCommand<Distribution>(OpenDistributionWithVsCodeViewModel);
        OpenDistroWithWinTermCommand = new RelayCommand<Distribution>(OpenDistroWithWinTermViewModel);
        CreateDistroCommand = new AsyncRelayCommand(CreateDistributionDialog);
        CreateSnapshotCommand = new AsyncRelayCommand<Distribution>(CreateSnapshotDialog);
        DisplaySnapshotsListCommand = new AsyncRelayCommand<Distribution>(DisplaySnapshotsList);

        _distributionService.InitDistributionsList();
        PopulateDistributionsCollection();
    }

    private void PopulateDistributionsCollection()
    {
        try
        {
            Console.WriteLine($"[INFO] Populate distributions collection");
            Distros.Clear();
            foreach (var distro in _distributionService.GetAllDistributions())
            {
                Distros.Add(distro);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task RemoveDistributionDialog(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening ContentDialog to remove {distribution.Name} ...");


        var dialog = new ContentDialog()
        {
            Title = $"Are you sure to remove \"{distribution.Name}\" ?",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        var buttonClicked = await dialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            RemoveDistributionViewModel(distribution);
        }
    }

    private void RemoveDistributionViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Removing {distribution.Name} ...");

        _distributionService.RemoveDistribution(distribution);
        Distros.Remove(distribution);

        if (!Distros.Contains(distribution))
        {
            var removeDistroInfoBar = _infoBarService.FindInfoBar("RemoveDistroInfoSuccess");
            _infoBarService.OpenInfoBar(removeDistroInfoBar, 2000);
        }
    }

    private async Task RenameDistributionDialog(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening ContentDialog to rename {distribution.Name} ...");

        try
        {
            var dialog = new RenameDistributionView()
            {
                Title = $"Rename \"{distribution.Name}\" :",
                XamlRoot = App.MainWindow.Content.XamlRoot,
            };

            dialog.PrimaryButtonClick += ValidateRenameDistribution;
            var buttonClicked = await dialog.ShowAsync();

            if (buttonClicked == ContentDialogResult.Primary)
            {
                var newDistroNameInput = (dialog.Content as StackPanel)!.FindChild("DistroNameInput") as TextBox;
                RenameDistributionViewModel(distribution, newDistroNameInput!.Text);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    private void ValidateRenameDistribution(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            ValidateDistributionName(sender, args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    internal void ValidateDistributionName(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var distroNameInput = sender.FindChild("DistroNameInput") as TextBox;
        distroNameInput!.ClearValue(Control.BorderBrushProperty);
        var errorInfoBar = sender.FindChild("DistroNameErrorInfoBar") as InfoBar;
        errorInfoBar!.IsOpen = false;

        var distroNamesList = Distros.Select(distro => distro.Name).ToList();
        var regex = new Regex("^[a-zA-Z0-9-_ ]*$");
        const int minLength = 2;

        try
        {
            var textInputValidationHelper = new TextInputValidationHelper(distroNameInput.Text);
            textInputValidationHelper
                .NotNullOrWhiteSpace()
                .IncludeWhiteSpaceChar()
                .MinimumLength(minLength)
                .InvalidCharacters(regex, "special characters")
                .DataAlreadyExist(distroNamesList);
        }
        catch (ArgumentException e)
        {
            args.Cancel = true;
            errorInfoBar.Message = e.Message;
            errorInfoBar.IsOpen = true;
            distroNameInput.BorderBrush = new SolidColorBrush(Colors.DarkRed);
            throw;
        }
    }

    private async Task RenameDistributionViewModel(Distribution distribution, string newDistroName)
    {
        Console.WriteLine($"[INFO] Renaming {distribution.Name} for {newDistroName}");

        try
        {
            var isDistroRenamed = await _distributionService.RenameDistribution(distribution, newDistroName);
            if (!isDistroRenamed)
            {
                return;
            }

            var index = Distros.ToList().FindIndex(distro => distro.Name == distribution.Name);
            if (index != -1)
            {
                Distros.ElementAt(index).Name = newDistroName;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void LaunchDistributionViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : ${distribution!.Name} distribution is launching ...");

        _distributionService.LaunchDistribution(distribution);

        // Publish message  (allows us to show the stop button when the start button is clicked)
        WeakReferenceMessenger.Default.Send(new ShowDistroStopButtonMessage(distribution));
    }

    private void StopDistributionViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : {distribution!.Name} distribution is stopping ...");

        _distributionService.StopDistribution(distribution);

        WeakReferenceMessenger.Default.Send(new HideDistroStopButtonMessage(distribution));
    }

    private void OpenDistributionWithFileExplorerViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : {distribution.Name} file system is opening ...");

        _distributionService.OpenDistributionFileSystem(distribution);
    }

    private void OpenDistributionWithVsCodeViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening {distribution.Name} with VS Code ...");

        _distributionService.OpenDistributionWithVsCode(distribution);

    }

    private void OpenDistroWithWinTermViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening {distribution.Name} with Windows Terminal ...");

        _distributionService.OpenDistroWithWinTerm(distribution);

        //WeakReferenceMessenger.Default.Send(new ShowDistroStopButtonMessage(distribution));
    }

    // return a tuple composed of the distro name, the resource origin (file/folder path or docker hub link)
    // and the creation mode chose by the user
    // TODO : Refactor
    private Tuple<string, string, string> GetCreateDistroFormInfos(ContentDialog dialog)
    {
        var distroNameInput = dialog.FindChild("DistroNameInput") as TextBox;
        var distroName = distroNameInput!.Text;

        var creationModeComboBox = dialog.FindChild("DistroCreationMode") as ComboBox;
        var creationMode = creationModeComboBox!.SelectedItem.ToString();

        TextBox? resourceOriginTextBox;
        var resourceOrigin = "";

        switch (creationMode)
        {
            case "Dockerfile":
                resourceOriginTextBox = dialog.FindChild("DockerfileInput") as TextBox;
                resourceOrigin = resourceOriginTextBox.Text;
                break;
            case "Docker Hub":
                resourceOriginTextBox = dialog.FindChild("DockerHubInput") as TextBox;
                resourceOrigin = resourceOriginTextBox.Text;
                break;
            case "Archive":
                resourceOriginTextBox = dialog.FindChild("ArchiveInput") as TextBox;
                resourceOrigin = resourceOriginTextBox.Text;
                break;
        }

        return Tuple.Create(distroName, creationMode, resourceOrigin);
    }

    private async Task CreateDistributionDialog()
    {
        Console.WriteLine($"[INFO] Command called : Opening ContentDialog for distribution creation");

        var createDistroDialog = new CreateDistributionView
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
        };

        createDistroDialog.PrimaryButtonClick += ValidateCreateDistribution;

        if (App.IsDistributionProcessing)
        {
            App.ShowSnapshotProcessingDialog();
            return;
        }

        var buttonClicked = await createDistroDialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            var (distroName, creationMode, resourceOrigin) = GetCreateDistroFormInfos(createDistroDialog);

            await CreateDistributionViewModel(distroName, creationMode, resourceOrigin);
        }
    }

    private void ValidateCreateDistribution(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            ValidateDistributionName(sender, args);
            ValidateCreationMode(sender, args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static void ValidateCreationMode(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            var creationMode = sender.FindChild("DistroCreationMode") as ComboBox;
            creationMode!.ClearValue(Control.BorderBrushProperty);
            var creationModeErrorInfoBar = sender.FindChild("CreationModeErrorInfoBar") as InfoBar;
            creationModeErrorInfoBar!.IsOpen = false;

            if (creationMode.SelectedItem == null)
            {
                args.Cancel = true;
                creationModeErrorInfoBar.IsOpen = true;
                creationMode.BorderBrush = new SolidColorBrush(Colors.DarkRed);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        
    }

    internal async Task CreateDistributionViewModel(string distroName, string creationMode, string resourceOrigin)
    {
        App.IsDistributionProcessing = true;
        var createDistroInfoProgress = _infoBarService.FindInfoBar("CreateDistroInfoProgress");
        _infoBarService.OpenInfoBar(createDistroInfoProgress);

        try
        {
            var newDistro = await _distributionService.CreateDistribution(distroName, creationMode, resourceOrigin);
            _infoBarService.CloseInfoBar(createDistroInfoProgress);
            var createDistroInfoSuccess = _infoBarService.FindInfoBar("CreateDistroInfoSuccess");
            _infoBarService.OpenInfoBar(createDistroInfoSuccess, 2000);
            Distros.Add(newDistro);
            App.IsDistributionProcessing = false;

        }
      
        catch (Exception e)
        {
            Console.WriteLine($"Service failed to create distribution {distroName}: " + e.Message);
            _infoBarService.CloseInfoBar(createDistroInfoProgress);
            var createDistroInfoError = _infoBarService.FindInfoBar("CreateDistroInfoError");
            _infoBarService.OpenInfoBar(createDistroInfoError, 5000);
            // TODO : Remove folder
            App.IsDistributionProcessing = false;
        }
    }

    private async Task DisplaySnapshotsList(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening ContentDialog to display snapshots");

        try
        {
            var displaySnapshots = new DisplaySnapshotsView()
            {
                XamlRoot = App.MainWindow.Content.XamlRoot,
                DataContext = distribution,
            };

            displaySnapshots.ShowAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task CreateSnapshotDialog(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening ContentDialog for snapshot creation");

        var createSnapshotDialog = new CreateSnapshotView
        {
            XamlRoot = App.MainWindow.Content.XamlRoot
        };

        createSnapshotDialog.PrimaryButtonClick += ValidateSnapshotName;

        var buttonClicked = await createSnapshotDialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            WeakReferenceMessenger.Default.Send(new HideDistroStopButtonMessage(distribution));
            var snapshotName = (createSnapshotDialog.FindChild("SnapshotNameInput") as TextBox)!.Text;
            var snapshotDescr = (createSnapshotDialog.FindChild("SnapshotDescrInput") as TextBox)!.Text
                .Replace(';', ' ')
                .Replace('\n',' ')
                .Replace('\r', ' '); ; // replace ';' characters to avoid error in SnapshotService::GetDistributionSnapshots
            await CreateSnapshotViewModel(distribution, snapshotName, snapshotDescr);
        }
    }

    // TODO : Refactor with ValidateDistroName
    private static void ValidateSnapshotName(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var snapshotNameInput = sender.FindChild("SnapshotNameInput") as TextBox;
        snapshotNameInput!.ClearValue(Control.BorderBrushProperty);

        var errorInfoBar = sender.FindChild("SnapshotNameErrorInfoBar") as InfoBar;
        errorInfoBar!.IsOpen = false;

        var regex = new Regex("^[a-zA-Z0-9-_ ]*$");
        const int minLength = 2;

        try
        {
            var textInputValidationHelper = new TextInputValidationHelper(snapshotNameInput.Text);
            textInputValidationHelper
                .NotNullOrWhiteSpace()
                .IncludeWhiteSpaceChar()
                .MinimumLength(minLength)
                .InvalidCharacters(regex, "special characters");
        }
        catch (ArgumentException e)
        {
            args.Cancel = true;
            errorInfoBar.Message = e.Message;
            errorInfoBar.IsOpen = true;
            snapshotNameInput.BorderBrush = new SolidColorBrush(Colors.DarkRed);
        }
    }

    //TODO : Refactor with CreateDistributionViewModel to avoid boilerplate code
    private async Task CreateSnapshotViewModel(Distribution distribution, string snapshotName,
        string snapshotDescr)
    {
        var createSnapshotInfoProgress = _infoBarService.FindInfoBar("CreateSnapshotInfoProgress");
        _infoBarService.OpenInfoBar(createSnapshotInfoProgress);
        var isSnapshotCreated = await _snapshotService.CreateDistroSnapshot(distribution, snapshotName, snapshotDescr);

        if (isSnapshotCreated)
        {
            _infoBarService.CloseInfoBar(createSnapshotInfoProgress);
            var createSnapshotInfoSuccess = _infoBarService.FindInfoBar("CreateSnapshotInfoSuccess");
            _infoBarService.OpenInfoBar(createSnapshotInfoSuccess, 2000);
        }
        else
        {
            _infoBarService.CloseInfoBar(createSnapshotInfoProgress);
            var createSnapshotInfoError = _infoBarService.FindInfoBar("CreateSnapshotInfoError");
            _infoBarService.OpenInfoBar(createSnapshotInfoError, 5000);
        }
    }
}
