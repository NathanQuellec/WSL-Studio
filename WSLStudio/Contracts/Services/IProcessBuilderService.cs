using System.Diagnostics;

namespace WSLStudio.Contracts.Services;

public interface IProcessBuilderService
{
    IProcessBuilderService SetFileName(string fileName);
    IProcessBuilderService SetArguments(string args);
    IProcessBuilderService SetRedirectStandardOutput(bool val);
    IProcessBuilderService SetUseShellExecute(bool val);
    IProcessBuilderService SetCreateNoWindow(bool val);
    Process Build();
}