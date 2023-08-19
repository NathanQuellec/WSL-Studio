﻿using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using WSLStudio.Contracts.Services;
using WSLStudio.Helpers;
using WSLStudio.Messages;
using WSLStudio.Models;
using WSLStudio.Views.ContentDialog;
using Brush = Microsoft.UI.Xaml.Media.Brush;

namespace WSLStudio.ViewModels;

public class DistrosListDetailsVM : ObservableObject
{

    private readonly IDistributionService _distributionService;
    private readonly IInfoBarService _infoBarService;

    private bool _isDistroCreationProcessing;

    #region RelayCommand

    public AsyncRelayCommand<Distribution> RemoveDistroCommand { get; set; }

    public AsyncRelayCommand<Distribution> RenameDistroCommand { get; set; }

    public RelayCommand<Distribution> LaunchDistroCommand { get; set; }

    public RelayCommand<Distribution> StopDistroCommand { get; set; }

    public RelayCommand<Distribution> OpenDistroWithFileExplorerCommand { get; set; }

    public RelayCommand<Distribution> OpenDistroWithVsCodeCommand { get; set; }

    public RelayCommand<Distribution> OpenDistroWithWinTermCommand { get; set; }

    public AsyncRelayCommand CreateDistroCommand { get; set; }

    public AsyncRelayCommand<Distribution> CreateDistroSnapshotCommand { get; set; }

    #endregion

    public ObservableCollection<Distribution> Distros { get; set; } = new();

    private string _snapshotName;
    private string _snapshotDescr;

    public DistrosListDetailsVM(IDistributionService distributionService, IInfoBarService infoBarService)
    {
        this._distributionService = distributionService;
        this._infoBarService = infoBarService;

        this._isDistroCreationProcessing = false;

        RemoveDistroCommand = new AsyncRelayCommand<Distribution>(RemoveDistributionDialog);
        RenameDistroCommand = new AsyncRelayCommand<Distribution>(RenameDistributionDialog);
        LaunchDistroCommand = new RelayCommand<Distribution>(LaunchDistributionViewModel);
        StopDistroCommand = new RelayCommand<Distribution>(StopDistributionViewModel);
        OpenDistroWithFileExplorerCommand = new RelayCommand<Distribution>(OpenDistributionWithFileExplorerViewModel);
        OpenDistroWithVsCodeCommand = new RelayCommand<Distribution>(OpenDistributionWithVsCodeViewModel);
        OpenDistroWithWinTermCommand = new RelayCommand<Distribution>(OpenDistroWithWinTermViewModel);
        CreateDistroCommand = new AsyncRelayCommand(CreateDistributionDialog, () => !this._isDistroCreationProcessing);
        CreateDistroSnapshotCommand = new AsyncRelayCommand<Distribution>(CreateDistroSnapshotDialog);

        this._distributionService.InitDistributionsList();
        this.PopulateDistributionsCollection();

    }

    private void PopulateDistributionsCollection()
    {
        try
        {
            Console.WriteLine($"[INFO] Populate distributions collection");
            this.Distros.Clear();
            foreach (var distro in this._distributionService.GetAllDistributions())
            {
                this.Distros.Add(distro);
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

        var dialogService = App.GetService<IDialogBuilderService>();


        var dialog = dialogService.SetTitle($"Are you sure to remove \"{distribution.Name}\" ?")
            .SetPrimaryButtonText("Remove")
            .SetCloseButtonText("Cancel")
            .SetDefaultButton(ContentDialogButton.Primary)
            .SetXamlRoot(App.MainWindow.Content.XamlRoot)
            .Build();

        var buttonClicked = await dialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            RemoveDistributionViewModel(distribution);
        }
    }

    private void RemoveDistributionViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Removing {distribution.Name} ...");

        this._distributionService.RemoveDistribution(distribution);
        this.Distros.Remove(distribution);

        if (!Distros.Contains(distribution))
        {
            var removeDistroInfoBar = this._infoBarService.FindInfoBar("RemoveDistroInfoSuccess");
            this._infoBarService.OpenInfoBar(removeDistroInfoBar, 2000);
        }
    }

    private async Task RenameDistributionDialog(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening ContentDialog to rename {distribution.Name} ...");

        var dialogService = App.GetService<IDialogBuilderService>();

        var newDistroNameInput = new TextBox()
        {
            Name = "DistroNameInput",
            Margin = new Thickness(0, 20, 0, 15),
            Height = 32,
            MaxLength = 30,
        };

        var renameDistroErrorInfoBar = new InfoBar()
        {
            Name = "DistroNameErrorInfoBar",
            Severity = InfoBarSeverity.Error,
            Title = "Invalid : Distribution Name",
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

    private void ValidateDistributionName(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var distroNameInput = sender.FindChild("DistroNameInput") as TextBox;
        distroNameInput.ClearValue(Control.BorderBrushProperty);
        var errorInfoBar = sender.FindChild("DistroNameErrorInfoBar") as InfoBar;
        errorInfoBar.IsOpen = false;

        var distroNamesList = Distros.Select(distro => distro.Name).ToList();
        var regex = new Regex("^[a-zA-Z0-9-_ ]*$");
        var minLength = 2;

        try
        {
            var inputValidationHelper = new InputValidationHelper();
            inputValidationHelper
                .NotNullOrWhiteSpace(distroNameInput.Text)
                .IncludeWhiteSpaceChar(distroNameInput.Text)
                .MinimumLength(distroNameInput.Text, minLength)
                .InvalidCharacters(distroNameInput.Text, regex, "special characters")
                .DataAlreadyExist(distroNameInput.Text, distroNamesList);
        }
        catch (ArgumentException e)
        {
            args.Cancel = true;
            errorInfoBar.Message = e.Message;
            errorInfoBar.IsOpen = true;
            distroNameInput.BorderBrush = new SolidColorBrush(Colors.DarkRed);
        }
    }

    private void RenameDistributionViewModel(Distribution distribution, string newDistroName)
    {
        Console.WriteLine($"[INFO] Renaming {distribution.Name} for {newDistroName}");

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
        Console.WriteLine($"[INFO] Command called : ${distribution!.Name} distribution is launching ...");

        this._distributionService.LaunchDistribution(distribution);
        // Publish message  (allows us to show the stop button when the start button is clicked)
        WeakReferenceMessenger.Default.Send(new ShowDistroStopButtonMessage(distribution));
    }

    private void StopDistributionViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : {distribution!.Name} distribution is stopping ...");

        this._distributionService.StopDistribution(distribution);
        WeakReferenceMessenger.Default.Send(new HideDistroStopButtonMessage(distribution));
    }

    private void OpenDistributionWithFileExplorerViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : {distribution.Name} file system is opening ...");

        this._distributionService.OpenDistributionFileSystem(distribution);
    }

    private void OpenDistributionWithVsCodeViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening {distribution.Name} with VS Code ...");
        this._distributionService.OpenDistributionWithVsCode(distribution);
    }

    private void OpenDistroWithWinTermViewModel(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening {distribution.Name} with Windows Terminal ...");
        this._distributionService.OpenDistroWithWinTerm(distribution);
    }

    // return a tuple composed of the distro name, the resource origin (file/folder path or docker hub link)
    // and the creation mode chose by the user
    // TODO : Refactor
    private Tuple<string, string, string> GetDistributionCreationInfos(ContentDialog dialog)
    {

        var dialogContent = dialog.Content as StackPanel;
        var contentContainer = dialogContent!.Children.First() as UserControl;
        var form = contentContainer!.Content as StackPanel;

        var resourceOrigin = "";

        var creationModeComboBox = form.FindName("DistroCreationMode") as ComboBox;
        var creationMode = creationModeComboBox!.SelectedItem.ToString();
        TextBox? inputTextBox;
        switch (creationMode)
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

        var distroNameInput = form.FindName("DistroNameInput") as TextBox;
        var distroName = distroNameInput!.Text;

        return Tuple.Create(distroName, resourceOrigin, creationMode);
    }

    private async Task CreateDistributionDialog()
    {
        Console.WriteLine($"[INFO] Command called : Opening ContentDialog for distribution creation");

        var dialogService = App.GetService<IDialogBuilderService>();

        // contentdialog content set in CreateDistroView.xaml
        var createDistroDialog = new CreateDistroView();

        var dialog = dialogService.SetTitle("Create Distribution :")
            .AddContent(createDistroDialog)
            .SetPrimaryButtonText("Create")
            .SetCloseButtonText("Cancel")
            .SetDefaultButton(ContentDialogButton.Primary)
            .SetPrimaryButtonClick(ValidateCreateDistribution)
            .SetXamlRoot(App.MainWindow.Content.XamlRoot)
            .Build();

        var buttonClicked = await dialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {
            var (distroName, resourceOrigin, creationMode) = this.GetDistributionCreationInfos(dialog);

            await CreateDistributionViewModel(creationMode, distroName, resourceOrigin);
        }
    }

    private void ValidateCreateDistribution(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ValidateDistributionName(sender, args);
        ValidateCreationMode(sender, args);
    }

    private void ValidateCreationMode(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var creationMode = sender.FindChild("DistroCreationMode") as ComboBox;
        creationMode.ClearValue(Control.BorderBrushProperty);
        var creationModeErrorInfoBar = sender.FindChild("CreationModeErrorInfoBar") as InfoBar;
        creationModeErrorInfoBar.IsOpen = false;

        try
        {
            var inputValidationHelper = new InputValidationHelper();
            inputValidationHelper.SelectorNotNull(creationMode.SelectedItem);
        }
        catch (ArgumentException e)
        {
            args.Cancel = true;
            creationModeErrorInfoBar.IsOpen = true;
            creationMode.BorderBrush = new SolidColorBrush(Colors.DarkRed);
        }
    }

    private async Task CreateDistributionViewModel(string creationMode, string distroName, string resourceOrigin)
    {
        this._isDistroCreationProcessing = true;
        var createNewDistroInfoProgress = this._infoBarService.FindInfoBar("CreateNewDistroInfoProgress");
        this._infoBarService.OpenInfoBar(createNewDistroInfoProgress);
        var newDistro = await this._distributionService.CreateDistribution(creationMode, distroName, resourceOrigin);

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


    private async Task CreateDistroSnapshotDialog(Distribution distribution)
    {
        Console.WriteLine($"[INFO] Command called : Opening ContentDialog for snapshot creation");

        var dialogService = App.GetService<IDialogBuilderService>();

        // contentdialog content set in CreateDistroSnapshotView.xaml
        var addSnapshotDialog = new CreateDistroSnapshotView();

        var dialog = dialogService.SetTitle("Create Snapshot :")
            .AddContent(addSnapshotDialog)
            .SetPrimaryButtonText("Create")
            .SetCloseButtonText("Cancel")
            .SetDefaultButton(ContentDialogButton.Primary)
            .SetPrimaryButtonClick(ValidateSnapshot)
            .SetXamlRoot(App.MainWindow.Content.XamlRoot)
            .Build();
        var buttonClicked = await dialog.ShowAsync();

        if (buttonClicked == ContentDialogResult.Primary)
        {

            //var (distroName, resourceOrigin, creationMode) = this.GetDistributionCreationInfos(dialog);

            //await CreateDistributionViewModel(creationMode, distroName, resourceOrigin);
            await CreateDistroSnapshotViewModel(distribution, this._snapshotName, this._snapshotDescr);
        }

    }

    private async void ValidateSnapshot(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var snapshotNameTextBox = sender.FindChild("SnapshotNameInput") as TextBox;
        var snapshotDescrTextBox = sender.FindChild("SnapshotDescrInput") as TextBox;
        this._snapshotName = snapshotNameTextBox.Text;
        this._snapshotDescr = snapshotDescrTextBox.Text;


    }

    private async Task CreateDistroSnapshotViewModel(Distribution distribution, string snapshotName,
        string snapshotDescr)
    {
      await this._distributionService.CreateDistroSnapshot(distribution, snapshotName, snapshotDescr);

    }
}
