﻿using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using WSLStudio.Messages;
using WSLStudio.Services;

namespace WSLStudio.ViewModels;

public class DistrosListDetailsViewModel : ObservableObject
{
    private readonly IDistributionService _distributionService;
    private readonly IDialogBuilderService _dialogBuilderService;

    private ObservableCollection<Distribution> _distros = new();

    public DistrosListDetailsViewModel ( IDistributionService distributionService,
                                         IWslService wslService,
                                         IDialogBuilderService dialogBuilderService )
    {
        this._distributionService = distributionService;
        this._dialogBuilderService = dialogBuilderService;

        RemoveDistroCommand = new RelayCommand<Distribution>(RemoveDistributionViewModel);
        OpenRenameDialogCommand = new AsyncRelayCommand<Distribution>(OpenRenameDialogViewModel);
        LaunchDistroCommand = new RelayCommand<Distribution>(LaunchDistributionViewModel);
        StopDistroCommand = new RelayCommand<Distribution>(StopDistributionViewModel);

        this._distributionService.InitDistributionsList();
        this.RetrieveDistrosData();
        _dialogBuilderService = dialogBuilderService;
    }

    public RelayCommand<Distribution> RemoveDistroCommand { get; set; }

    public AsyncRelayCommand<Distribution> OpenRenameDialogCommand { get; set; }

    public RelayCommand<Distribution> LaunchDistroCommand { get; set; }

    public RelayCommand<Distribution> StopDistroCommand { get; set; }

    public ObservableCollection<Distribution> Distros
    {
        get => this._distros;
        set => SetProperty(ref this._distros, value);
    }

    private void RemoveDistributionViewModel(Distribution? distribution)
    {
        Debug.WriteLine($"[INFO] Command called : Removing ${distribution} ...");

        if (distribution == null)
        {
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the xaml source");
        }
        else
        {
            this._distributionService.RemoveDistribution(distribution);
            this._distros.Remove(distribution);
        }
    }

    public async Task OpenRenameDialogViewModel(Distribution? distribution)
    {
        Debug.WriteLine($"[INFO] Command called : Opening dialog to rename ${distribution.Name} ...");
        var input = new TextBox()
        {
            Header = "Renaming",
            Height = 64,
        };

        var dialog = this._dialogBuilderService.SetTitle($"Rename '{distribution.Name}'")
            .SetContent(input)
            .SetPrimaryButtonText("Rename")
            .SetCloseButtonText("Cancel")
            .SetXamlRoot(App.MainWindow.Content.XamlRoot);
        var content = (TextBox)dialog.GetDialogContent();

        var renameDistro = await dialog.ShowAsync();

        if (renameDistro)
        {
            RenameDistributionViewModel(distribution, content.Text);
        }
    }

    public void RenameDistributionViewModel(Distribution distribution, string newDistroName)
    {

        Debug.WriteLine($"[INFO] Renaming {distribution.Name} for {newDistroName} in DistrosListDetailsViewModel");

        if (distribution == null)
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the xaml source");

        else
        {
            this._distributionService.RenameDistribution(distribution, newDistroName);

            int index = this._distros.ToList().FindIndex(distro => distro.Name == distribution.Name);
            if (index != -1)
            {
                _distros[index].Name = newDistroName;
            }
        }
    }

    private void LaunchDistributionViewModel(Distribution? distribution)
    {
        Debug.WriteLine($"[INFO] Command called : ${distribution} distribution is launching ...");

        if (distribution == null)
        {
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the xaml source");
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
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the xaml source");
        }
        else
        {
            this._distributionService.StopDistribution(distribution);
            WeakReferenceMessenger.Default.Send(new HideDistroStopButtonMessage(distribution));
        }
    }

    private void RetrieveDistrosData()
    {
        try
        {
            this._distros.Clear();
            foreach (var distro in this._distributionService.GetAllDistributions())
            {
                this._distros.Add(distro);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

}
