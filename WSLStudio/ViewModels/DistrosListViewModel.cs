using CommunityToolkit.Mvvm.ComponentModel;
using WSLStudio.Models;
using WSLStudio.Contracts.Services;
using System.Collections.ObjectModel;

namespace WSLStudio.ViewModels;

public class DistrosListViewModel : ObservableObject
{
    private readonly IDataService dataService;
    private ObservableCollection<Distribution> distros = new ObservableCollection<Distribution>();
    private ObservableCollection<string> distrosNames= new ObservableCollection<string>();

    public DistrosListViewModel(IDataService dataService)
    {
        this.dataService = dataService;
        PopulateData();
    }

    public ObservableCollection<Distribution> Distros
    {
        get
        {
            return distros;
        }
        set
        {
            SetProperty(ref distros, value);
        }
    }

    public ObservableCollection<string> DistrosNames
    {
        get
        {
            return distrosNames;
        }
        set
        {
            SetProperty(ref distrosNames, value);
        }
    }

    private void PopulateData()
    {
        distros.Clear();

        foreach(var distro in dataService.GetAllDistributions()) {
            distros.Add(distro);                    
        }

        distrosNames.Clear();

        foreach(var distro in Distros)
        {
            distrosNames.Add(distro.Name);
        }
    }
    
}
