using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Collections.ObjectModel;

namespace WSLStudio.ViewModels;

public class DistrosListViewModel : ObservableObject
{
    private readonly IDataService dataService;
    private ObservableCollection<Distribution> distros = new();

    public DistrosListViewModel(IDataService dataService)
    {
        this.dataService = dataService;
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
        foreach(var distro in dataService.GetAllDistributions()) {
            distros.Add(distro);
        }
    }
}
