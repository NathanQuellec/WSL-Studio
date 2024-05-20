using System.Diagnostics;

namespace WSLStudio.Helpers;

// TODO : Add comment 

public class ProcessBuilder
{
    private readonly Process _process = new();

    public ProcessBuilder(string fileName)
    {
        this._process.StartInfo.FileName = fileName;
    }

    public ProcessBuilder SetArguments(string args)
    {
        this._process.StartInfo.Arguments = args;
        return this;
    }

    public ProcessBuilder SetRedirectStandardOutput(bool val)
    {
        this._process.StartInfo.RedirectStandardOutput = val;
        return this;
    }

    public ProcessBuilder SetRedirectStandardError(bool val)
    {
        this._process.StartInfo.RedirectStandardError = val;
        return this;
    }

    public ProcessBuilder SetUseShellExecute(bool val)
    {
        this._process.StartInfo.UseShellExecute = val;
        return this;
    }

    public ProcessBuilder SetCreateNoWindow(bool val)
    {
        this._process.StartInfo.CreateNoWindow = val;
        return this;
    }

    public Process Build()
    {
        return this._process;
    }
}