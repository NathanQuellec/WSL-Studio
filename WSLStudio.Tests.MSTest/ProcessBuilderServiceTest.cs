using System.Diagnostics;
using WSLStudio.Services;
using WSLStudio.Contracts.Services;
using Microsoft.UI.Xaml.Controls;

namespace WSLStudio.Tests.MSTest;

// TODO: Write unit tests.
// https://docs.microsoft.com/visualstudio/test/getting-started-with-unit-testing
// https://docs.microsoft.com/visualstudio/test/using-microsoft-visualstudio-testtools-unittesting-members-in-unit-tests
// https://docs.microsoft.com/visualstudio/test/run-unit-tests-with-test-explorer

[TestClass]
public class ProcessBuilderServiceTest
{
    private IProcessBuilderService processBuilder;

    [TestInitialize]
    public void TestInitialize()
    {
        processBuilder = new ProcessBuilderService();
    }

    [TestCleanup] public void Cleanup()
    {
        processBuilder = null;
    }

    [TestMethod]
    public void TestSetFileName()
    {
        string fileName = "wsl.exe";
        processBuilder.SetFileName(fileName);
        Assert.AreEqual(fileName, processBuilder.Build().StartInfo.FileName);   
    }

    [TestMethod]
    public void TestSetArguments()
    {
        string arguments = "/c wsl --list";
        processBuilder.SetArguments(arguments);
        Assert.AreEqual(arguments, processBuilder.Build().StartInfo.Arguments);
    }

    [TestMethod]
    public void TestSetRedirectStandardOutput()
    {
        bool val = true;
        processBuilder.SetRedirectStandardOutput(val);
        Assert.AreEqual(val, processBuilder.Build().StartInfo.RedirectStandardOutput);
    }

    [TestMethod]
    public void TestSetUseShellExecute()
    {
        bool val = false;
        processBuilder.SetUseShellExecute(val);
        Assert.AreEqual(val, processBuilder.Build().StartInfo.UseShellExecute);
    }

    [TestMethod]
    public void TestSetSetCreateNoWindow()
    {
        bool val = true;
        processBuilder.SetCreateNoWindow(val);
        Assert.AreEqual(val, processBuilder.Build().StartInfo.CreateNoWindow);
    }


    /*[UITestMethod]
    public void UITestMethod()
    {
        Assert.AreEqual(0, new Grid().ActualWidth);
    }*/
}
