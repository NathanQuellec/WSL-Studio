using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Serilog;
using WSLStudio.Contracts.Services;
using WSLStudio.Contracts.Services.UserInterface;
using WSLStudio.Helpers;
using WSLStudio.Messages;
using WSLStudio.Models;
using WSLStudio.Views.Dialogs;

namespace WSLStudio.ViewModels;

public class DistrosListDetailsVM : ObservableObject
{

    private readonly IDistributionService _distributionService;
    private readonly ISnapshotService _snapshotService;
    private readonly IInfoBarService _infoBarService;

    #region RelayCommand

    public AsyncRelayCommand<Distribution> RemoveDistroCommand
    {
        get; set;
    }

    public AsyncRelayCommand<Distribution> RenameDistroCommand
    {
        get; set;
    }

    public RelayCommand<Distribution> LaunchDistroCommand
    {
        get; set;
    }

    public RelayCommand<Distribution> StopDistroCommand
    {
        get; set;
    }

    public RelayCommand<Distribution> OpenDistroWithFileExplorerCommand
    {
        get; set;
    }

    public RelayCommand<Distribution> OpenDistroWithVsCodeCommand
    {
        get; set;
    }

    public RelayCommand<Distribution> OpenDistroWithWinTermCommand
    {
        get; set;
    }

    public AsyncRelayCommand CreateDistroCommand
    {
        get; set;
    }

    public AsyncRelayCommand<Distribution> CreateSnapshotCommand
    {
        get; set;
    }

    public AsyncRelayCommand<Distribution> DisplaySnapshotsListCommand
    {
        get; set;
    }

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
        Log.Information($"Populate list of distributions");
        try
        {
            Distros.Clear();
            foreach (var distro in _distributionService.GetAllDistributions())
            {
                Distros.Add(distro);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to populate list of distributions - Caused by exception : {ex.Message}");
        }
    }

    private async Task RemoveDistributionDialog(Distribution distribution)
    {
        Log.Information($"[COMMAND CALL] - Opening ContentDialog to remove {distribution.Name} ...");

        try
        {
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
        catch (Exception ex)
        {
            Log.Error($"Failed to open distribution's delete confirmation dialog - Caused by exception : {ex}");
        }
    }

    private void RemoveDistributionViewModel(Distribution distribution)
    {
        Log.Information($"Removing distribution {distribution.Name} ...");

        _distributionService.RemoveDistribution(distribution);
        Distros.Remove(distribution);

        if (!Distros.Contains(distribution))
        {
            var removeDistroInfoBar = _infoBarService.FindInfoBar("RemoveDistroInfoSuccess");
            _infoBarService.OpenInfoBar(removeDistroInfoBar, 2000);
            Log.Information($"Distribution {distribution.Name} has been successfully deleted");
        }
        else
        {
            Log.Warning($"Distribution {distribution.Name} has not been found");
        }
    }

    private async Task RenameDistributionDialog(Distribution distribution)
    {
        Log.Information($"[COMMAND CALL] : Opening ContentDialog to rename distribution {distribution.Name} ...");

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
            Log.Error($"Failed to open distribution's rename dialog - Caused by exception : {ex}");
        }
    }

    private void ValidateRenameDistribution(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            ValidateDistributionName(sender, args);
            Log.Information("Successfully validate new distribution name");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to validate new name of distribution - Caused by exception : {ex}");
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
            var textInputValidationHelper = new TextInputValidation(distroNameInput.Text);
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
        Log.Information($"Renaming distribution {distribution.Name} with {newDistroName}");

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
            Log.Error($"Failed to rename distribution {distribution} with {newDistroName} - Caused by exception : {ex}");
        }
    }

    private void LaunchDistributionViewModel(Distribution distribution)
    {
        Log.Information($"[COMMAND CALL] {distribution!.Name} distribution is launching ...");

        _distributionService.LaunchDistribution(distribution);

        // Publish message  (allows us to show the stop button when the start button is clicked)
        WeakReferenceMessenger.Default.Send(new ShowDistroStopButtonMessage(distribution));
    }

    private void StopDistributionViewModel(Distribution distribution)
    {
        Log.Information($"[COMMAND CALL] {distribution!.Name} distribution is stopping ...");

        _distributionService.StopDistribution(distribution);

        WeakReferenceMessenger.Default.Send(new HideDistroStopButtonMessage(distribution));
    }

    private void OpenDistributionWithFileExplorerViewModel(Distribution distribution)
    {
        Log.Information($"[COMMAND CALL] {distribution!.Name} file system is opening ...");

        _distributionService.OpenDistributionFileSystem(distribution);
    }

    private void OpenDistributionWithVsCodeViewModel(Distribution distribution)
    {
        Log.Information($"[COMMAND CALL] Opening {distribution.Name} with VS Code ...");

        _distributionService.OpenDistributionWithVsCode(distribution);

    }

    private void OpenDistroWithWinTermViewModel(Distribution distribution)
    {
        Log.Information($"[COMMAND CALL] Opening {distribution.Name} with Windows Terminal ...");

        _distributionService.OpenDistroWithWinTerm(distribution);

        //WeakReferenceMessenger.Default.Send(new ShowDistroStopButtonMessage(distribution));
    }

    // return a tuple composed of the distro name, the resource origin (file/folder path or docker hub link)
    // and the creation mode chose by the user
    // TODO : Refactor
    private static Tuple<string, string, string>? GetDistroCreationFormInfos(ContentDialog dialog)
    {
        Log.Information("Fetching distribution creation form's information ...");
        try
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
        catch (Exception ex)
        {
            Log.Error($"Failed to fetch distribution creation form's information - Caused by exception : {ex}");
            return null;
        }

    }

    private async Task CreateDistributionDialog()
    {
        Log.Information("[COMMAND CALL] Opening ContentDialog for distribution creation");

        try
        {
            var createDistroDialog = new CreateDistributionView
            {
                XamlRoot = App.MainWindow.Content.XamlRoot,
            };

            createDistroDialog.PrimaryButtonClick += ValidateCreateDistribution;

            if (App.IsDistributionProcessing)
            {
                App.ShowIsProcessingDialog();
                return;
            }

            var buttonClicked = await createDistroDialog.ShowAsync();

            if (buttonClicked == ContentDialogResult.Primary)
            {
                var (distroName, creationMode, resourceOrigin) = GetDistroCreationFormInfos(createDistroDialog);

                await CreateDistributionViewModel(distroName, creationMode, resourceOrigin);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open distribution creation dialog - Caused by exception : {ex}");
        }
    }

    private void ValidateCreateDistribution(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Log.Information("Validating distribution creation ...");
        try
        {
            ValidateDistributionName(sender, args);
            ValidateCreationMode(sender, args);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to validate distribution creation - Caused by exception : {ex}");
        }
    }

    private static void ValidateCreationMode(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Log.Information("Validating distribution creation mode ...");

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
            Log.Error($"Failed to validate distribution creation mode - Caused by exception : {ex}");
        }

    }

    internal async Task CreateDistributionViewModel(string distroName, string creationMode, string resourceOrigin)
    {
        Log.Information("Creating new distribution ...");

        App.IsDistributionProcessing = true;
        var createDistroInfoProgress = _infoBarService.FindInfoBar("CreateDistroInfoProgress");
        // setting initial progress bar message
        WeakReferenceMessenger.Default.Send(new ProgressBarMessage("WSL Studio creates your distribution ..."));
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

        catch (Exception ex)
        {
            Log.Error($"Failed to create new distribution {distroName} - Caused by exception : {ex} ");

            _infoBarService.CloseInfoBar(createDistroInfoProgress);
            var createDistroInfoError = _infoBarService.FindInfoBar("CreateDistroInfoError");
            _infoBarService.OpenInfoBar(createDistroInfoError, ex.Message, 5000);
            App.IsDistributionProcessing = false;
        }
    }

    private async Task DisplaySnapshotsList(Distribution distribution)
    {
        Log.Information($"[COMMAND CALL] Opening ContentDialog to display snapshots of {distribution.Name}");

        try
        {
            var displaySnapshots = new DisplaySnapshotsView()
            {
                XamlRoot = App.MainWindow.Content.XamlRoot,
                DataContext = distribution,
            };

            await displaySnapshots.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to display snapshots of {distribution.Name} - Caused by exception : {ex}");
        }
    }

    private async Task CreateSnapshotDialog(Distribution distribution)
    {
        Log.Information($"[COMMAND CALL] : Opening ContentDialog for snapshot creation of {distribution.Name}");

        try
        {
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
                    .Replace('\n', ' ')
                    .Replace('\r', ' ');
                ; // replace some special characters to avoid error in SnapshotService::GetDistributionSnapshots
                var isFastSnapshot = (createSnapshotDialog.FindChild("IsFastSnapshot") as ToggleSwitch)!.IsOn;
                await CreateSnapshotViewModel(distribution, snapshotName, snapshotDescr, isFastSnapshot);
            }
        }

        catch (Exception ex)
        {
            Log.Error($"Failed to open snapshot creation dialog - Caused by exception : {ex}");
        }
    }

    // TODO : Refactor with ValidateDistroName
    private static void ValidateSnapshotName(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Log.Information("Validating snapshot name ...");

        var snapshotNameInput = sender.FindChild("SnapshotNameInput") as TextBox;
        snapshotNameInput!.ClearValue(Control.BorderBrushProperty);

        var errorInfoBar = sender.FindChild("SnapshotNameErrorInfoBar") as InfoBar;
        errorInfoBar!.IsOpen = false;

        var regex = new Regex("^[a-zA-Z0-9-_ ]*$");
        const int minLength = 2;

        try
        {
            var textInputValidationHelper = new TextInputValidation(snapshotNameInput.Text);
            textInputValidationHelper
                .NotNullOrWhiteSpace()
                .IncludeWhiteSpaceChar()
                .MinimumLength(minLength)
                .InvalidCharacters(regex, "special characters");
        }
        catch (ArgumentException e)
        {
            Log.Warning("Failed to validate snapshot name ...");
            args.Cancel = true;
            errorInfoBar.Message = e.Message;
            errorInfoBar.IsOpen = true;
            snapshotNameInput.BorderBrush = new SolidColorBrush(Colors.DarkRed);
        }
    }

    //TODO : Refactor with CreateDistributionViewModel to avoid boilerplate code
    private async Task CreateSnapshotViewModel(Distribution distribution, string snapshotName,
        string snapshotDescr, bool isFastSnapshot)
    {
        Log.Information($"Creating snapshot {snapshotName} of {distribution.Name} ...");
        try
        {
            var createSnapshotInfoProgress = _infoBarService.FindInfoBar("CreateSnapshotInfoProgress");
            _infoBarService.OpenInfoBar(createSnapshotInfoProgress);
            var isSnapshotCreated =
                await _snapshotService.CreateSnapshot(distribution, snapshotName, snapshotDescr, isFastSnapshot);

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
        catch (Exception ex)
        {
            Log.Error($"Failed to create snapshot {snapshotName} of {distribution.Name} - Caused by exception : {ex}");
        }
    }
}
