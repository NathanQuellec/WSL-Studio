using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Collections.ObjectModel;

namespace WSLStudio.ViewModels;

public class DistrosListDetailsViewModel : ObservableObject
{
    private readonly IDistributionService distributionService;
    private ObservableCollection<Distribution> distros = new();

    public DistrosListDetailsViewModel(IDistributionService distributionService)
    {
        this.distributionService = distributionService;
        RetrieveDistrosData();
    }

    public ObservableCollection<Distribution> Distros
    {
        get => distros;
        set => SetProperty(ref distros, value);
    }

    private void RetrieveDistrosData()
    {
        distros.Clear();
        foreach(var distro in distributionService.GetAllDistributions()) {
            distros.Add(distro);
        }
    }
}
