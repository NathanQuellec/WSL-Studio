using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

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
        _distributionService.LaunchDistribution(distro);
    }

    private void RetrieveDistrosData()
    {
        _distros.Clear();
        foreach(var distro in _distributionService.GetAllDistributions()) {
            _distros.Add(distro);
        }
    }

}
