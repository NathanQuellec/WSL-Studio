using System.Diagnostics;

namespace WSLStudio.Helpers;

// TODO : Add comment 

public class ProcessBuilderHelper
{
    private readonly Process _process = new();

    public ProcessBuilderHelper(string fileName)
    {
        this._process.StartInfo.FileName = fileName;
    }

    public ProcessBuilderHelper SetArguments(string args)
    {
        this._process.StartInfo.Arguments = args;
        return this;
    }

    public ProcessBuilderHelper SetRedirectStandardOutput(bool val)
    {
        this._process.StartInfo.RedirectStandardOutput = val;
        return this;
    }

    public ProcessBuilderHelper SetRedirectStandardError(bool val)
    {
        this._process.StartInfo.RedirectStandardError = val;
        return this;
    }

    public ProcessBuilderHelper SetUseShellExecute(bool val)
    {
        this._process.StartInfo.UseShellExecute = val;
        return this;
    }

    public ProcessBuilderHelper SetCreateNoWindow(bool val)
    {
        this._process.StartInfo.CreateNoWindow = val;
        return this;
    }

    public Process Build()
    {
        return this._process;
    }
}