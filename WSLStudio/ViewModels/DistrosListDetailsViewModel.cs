using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Collections.ObjectModel;

namespace WSLStudio.ViewModels;

public class DistributionsListDetailsViewModel : ObservableObject
{
    private readonly IDistributionService distributionService;
    private ObservableCollection<Distribution> distros = new();

    public DistributionsListDetailsViewModel(IDistributionService distributionService)
    {
        this.distributionService = distributionService;
        PopulateData();
    }

    public ObservableCollection<Distribution> Distros
    {
        get => distros;
        set => SetProperty(ref distros, value);
    }

    private void PopulateData()
    {
        distros.Clear();
        foreach(var distro in distributionService.GetAllDistributions()) {
            distros.Add(distro);
        }
    }
}
