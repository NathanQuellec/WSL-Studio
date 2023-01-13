using System.Diagnostics;
using WSLStudio.Services;
using WSLStudio.Contracts.Services;
using Microsoft.UI.Xaml.Controls;
using WSLStudio.Helpers;

namespace WSLStudio.Tests.MSTest;

// TODO: Write unit tests.
// https://docs.microsoft.com/visualstudio/test/getting-started-with-unit-testing
// https://docs.microsoft.com/visualstudio/test/using-microsoft-visualstudio-testtools-unittesting-members-in-unit-tests
// https://docs.microsoft.com/visualstudio/test/run-unit-tests-with-test-explorer

[TestClass]
public class ProcessBuilderHelperTest
{
    private ProcessBuilderHelper _processBuilder;

    [TestInitialize]
    public void InitProcess()
    {
        string fileName = "cmd.exe";
        _processBuilder = new ProcessBuilderHelper(fileName);
        Assert.AreEqual(fileName, _processBuilder.Build().StartInfo.FileName);   
    }

    [TestMethod]
    public void TestSetArguments()
    {
        string arguments = "/c wsl --list";
        _processBuilder.SetArguments(arguments);
        Assert.AreEqual(arguments, _processBuilder.Build().StartInfo.Arguments);
    }

    [TestMethod]
    public void TestSetRedirectStandardOutput()
    {
        bool val = true;
        _processBuilder.SetRedirectStandardOutput(val);
        Assert.AreEqual(val, _processBuilder.Build().StartInfo.RedirectStandardOutput);
    }

    [TestMethod]
    public void TestSetUseShellExecute()
    {
        bool val = false;
        _processBuilder.SetUseShellExecute(val);
        Assert.AreEqual(val, _processBuilder.Build().StartInfo.UseShellExecute);
    }

    [TestMethod]
    public void TestSetSetCreateNoWindow()
    {
        bool val = true;
        _processBuilder.SetCreateNoWindow(val);
        Assert.AreEqual(val, _processBuilder.Build().StartInfo.CreateNoWindow);
    }
}
