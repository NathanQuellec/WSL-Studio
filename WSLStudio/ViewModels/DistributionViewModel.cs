using System.ComponentModel;
using System.Runtime.CompilerServices;
using WSLStudio.Models;

namespace WSLStudio.ViewModels;

public class DistributionViewModel : INotifyPropertyChanged
{

    private Distribution _distribution;

    public string Name
    {
        get => this._distribution.Name;
        set
        {
            this._distribution.Name = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}