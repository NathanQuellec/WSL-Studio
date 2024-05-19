using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Serilog;
using WSLStudio.Contracts.Models;

namespace WSLStudio.Models;

public class Distribution : INotifyPropertyChanged, IBaseModel
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

    private string _snapshotsTotalSize = "0.0";
    public string SnapshotsTotalSize
    {
        get => _snapshotsTotalSize;
        set
        {
            _snapshotsTotalSize = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Log.Debug($"PropertyChanged Event Raised for property \"{propertyName}\"");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
