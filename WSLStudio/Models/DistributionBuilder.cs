using System.Collections.ObjectModel;
using System.Diagnostics;

namespace WSLStudio.Models;

public class DistributionBuilder
{
    private readonly Distribution _distribution;

    public DistributionBuilder()
    {
        _distribution = new Distribution();
    }

    public DistributionBuilder WithId(Guid id)
    {
        _distribution.Id = id;
        return this;
    }

    public DistributionBuilder WithName(string name)
    {
        _distribution.Name = name;
        return this;
    }

    public DistributionBuilder WithPath(string path)
    {
        _distribution.Path = path;
        return this;
    }

    public DistributionBuilder WithWslVersion(int wslVersion)
    {
        _distribution.WslVersion = wslVersion;
        return this;
    }

    public DistributionBuilder WithOsName(string osName)
    {
        _distribution.OsName = osName;
        return this;
    }

    public DistributionBuilder WithOsVersion(string osVersion)
    {
        _distribution.OsVersion = osVersion;
        return this;
    }

    public DistributionBuilder WithSize(string size)
    {
        _distribution.Size = size;
        return this;
    }

    public DistributionBuilder WithUsers(IList<string> users)
    {
        _distribution.Users = users;
        return this;
    }

    public DistributionBuilder WithSnapshots(ObservableCollection<Snapshot> snapshots)
    {
        _distribution.Snapshots = snapshots;
        return this;
    }

    public DistributionBuilder WithRunningProcesses(IList<Process> runningProcesses)
    {
        _distribution.RunningProcesses = runningProcesses;
        return this;
    }

    public Distribution Build()
    {
        return _distribution;
    }

}