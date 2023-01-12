using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WSLStudio.Models;

public class Distribution : INotifyPropertyChanged
{
    public Guid Id { get; init; }
    public string Path { get; set; }
    public bool IsDefault { get; set; }
    public int WslVersion { get; set; }

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

    public double MemoryLimit { get; set; } = 2.0;
    public int ProcessorLimit { get; set; } = 4;
    public IList<Process> RunningProcesses { get; set; } = new List<Process>();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
