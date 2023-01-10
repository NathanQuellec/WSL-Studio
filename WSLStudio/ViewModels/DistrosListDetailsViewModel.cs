using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WSLStudio.Messages;

namespace WSLStudio.ViewModels;

public class DistrosListDetailsViewModel : ObservableObject
{
    private readonly IDistributionService _distributionService;
    private ObservableCollection<Distribution> _distros = new();

    public DistrosListDetailsViewModel(IDistributionService distributionService)
    {
        this._distributionService = distributionService;
        LaunchDistroCommand = new RelayCommand<Distribution>(LaunchDistributionViewModel);
        RetrieveDistrosData();
    }

    public RelayCommand<Distribution> LaunchDistroCommand { get; set; }

    public ObservableCollection<Distribution> Distros
    {
        get => _distros;
        set => SetProperty(ref _distros, value);
    }

    private void LaunchDistributionViewModel(Distribution? distro)
    {
        Debug.WriteLine($"[INFO] Command called : ${distro} distribution is launching ...");

        if (distro == null)
        {
            Debug.WriteLine($"[ERROR] Impossible to retrieve the distro object from the xaml source");
        }

        _distributionService.LaunchDistribution(distro);

        // Publish message  (allows us to show the stop button when the start button is clicked)
        WeakReferenceMessenger.Default.Send(new ShowStopButtonMessage());
    }

    private void RetrieveDistrosData()
    {
        _distros.Clear();
        foreach(var distro in _distributionService.GetAllDistributions()) {
            _distros.Add(distro);
        }
    }

}
