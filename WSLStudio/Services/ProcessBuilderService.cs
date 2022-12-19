using System.Diagnostics;
using WSLStudio.Contracts.Services;

namespace WSLStudio.Services;

public class ProcessBuilderService : IProcessBuilderService
{
    private readonly Process _process;

    public ProcessBuilderService()
    {
        this._process = new Process();
    }
    
    public IProcessBuilderService SetFileName(string fileName)
    {
        this._process.StartInfo.FileName = fileName;
        return this;
    }

    public IProcessBuilderService SetArguments(string args)
    {
        this._process.StartInfo.Arguments = args;
        return this;
    }

    public IProcessBuilderService SetRedirectStandardOutput(bool val)
    {
        this._process.StartInfo.RedirectStandardOutput = val;
        return this;
    }

    public IProcessBuilderService SetUseShellExecute(bool val)
    {
        this._process.StartInfo.UseShellExecute = val;
        return this;
    }

    public IProcessBuilderService SetCreateNoWindow(bool val)
    {
        this._process.StartInfo.CreateNoWindow = val;
        return this;
    }

    public Process Build()
    {
        return this._process;
    }
}