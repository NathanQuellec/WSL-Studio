using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WSLStudio.Models;

public class Distribution : INotifyPropertyChanged
{
    public Guid Id { get; set; }
    public string Path { get; set; }
    public int WslVersion { get; set; }
    public string OsName { get; set; }
    public string OsVersion { get; set; }
    public string Size { get; set; }

    public IList<string> Users { get; set; } = new List<string>();

    public ObservableCollection<Snapshot> Snapshots { get; set; } = new();
    public IList<Process> RunningProcesses { get; set; } = new List<Process>();

    // TODO : Replace OnPropertyChanged by SetProperty OR move to view model
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Debug.WriteLine($"[INFO] PropertyChanged Event Raised for property \"{propertyName}\"");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
