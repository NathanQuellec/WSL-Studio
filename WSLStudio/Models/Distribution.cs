using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
    public bool IsDefault { get; set; }
    public int WslVersion { get; set; }
    public string OsName { get; set; }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Debug.WriteLine($"[INFO] PropertyChanged Event Raised for property \"{propertyName}\"");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
