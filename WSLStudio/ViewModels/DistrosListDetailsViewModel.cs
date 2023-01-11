using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WSLStudio.Messages;
using WSLStudio.Services;

namespace WSLStudio.ViewModels;

public class DistrosListDetailsViewModel : ObservableObject
{
    private readonly IDistributionService _distributionService;
    private readonly IWslService _wslService;
    private ObservableCollection<Distribution> _distros = new();

    public DistrosListDetailsViewModel(IDistributionService distributionService, IWslService wslService)
    {
        this._distributionService = distributionService;
        this._wslService = wslService;

        RemoveDistroCommand = new RelayCommand<Distribution>(RemoveDistributionViewModel);
        RenameDistroCommand = new RelayCommand<RenameDistroCmd>(RenameDistributionViewModel);
        LaunchDistroCommand = new RelayCommand<Distribution>(LaunchDistributionViewModel);
        StopDistroCommand = new RelayCommand<Distribution>(StopDistributionViewModel);

        this._distributionService.InitDistributionsList();
        this.RetrieveDistrosData();
    }

    public RelayCommand<Distribution> RemoveDistroCommand { get; set; }

    public RelayCommand<RenameDistroCmd> RenameDistroCommand { get; set;}

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

    public void RenameDistributionViewModel(RenameDistroCmd renameDistroCmd)
    {
        var distribution = renameDistroCmd.distribution;
        var newDistroName = renameDistroCmd.newDistroName;

        Debug.WriteLine($"[INFO] Command called : Renaming ${distribution.Name} ...");

        if (renameDistroCmd.distribution == null)
        {
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distribution object from the xaml source");
        }
        else
        {
            this._distributionService.RenameDistribution(distribution, newDistroName);
            foreach (var distro in this._distros)
            {
                if (distro.Name == distribution.Name)
                {
                    distro.Name = newDistroName;
                }
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
