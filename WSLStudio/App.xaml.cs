using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

using WSLStudio.Activation;
using WSLStudio.Contracts.Services;
using WSLStudio.Core.Contracts.Services;
using WSLStudio.Core.Services;
using WSLStudio.Helpers;
using WSLStudio.Services;
using WSLStudio.ViewModels;
using WSLStudio.Views;
using WSLStudio.Models;

namespace WSLStudio;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging

    private IDataService dataService = new DataService();

    public IHost Host
    {
        get;
    }

    public static void InitializeDistrosList(IDataService dataService)
    {
        ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c wsl --list");
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;

        Process proc = new();
        proc.StartInfo = processStartInfo;
        proc.Start();

        var commandResults = proc.StandardOutput.ReadToEnd();
        commandResults = commandResults.Replace("\0", string.Empty).Replace("\r", string.Empty);
        var distrosResults = commandResults.Split('\n');

        // remove "Default" in the prompt result 
        distrosResults[1] = distrosResults[1].Split(" ")[0];
        Debug.WriteLine("-----------LIST OF WSL DISTROS-----------");
        for (var i = 1; i < distrosResults.Length; i++)
        {
            // Exclude empty line(s) and Docker special-purpose internal Linux distros 
            if (distrosResults[i].Trim().Length > 0 && distrosResults[i] != "docker-desktop" && distrosResults[i] != "docker-desktop-data" )
            {
                dataService.AddDistribution( new Distribution { Name = distrosResults[i].Trim() } );
            }
        }

    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public App()
    {
        InitializeComponent();
        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDataService, DataService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<DistrosListViewModel>();
            services.AddTransient<DistrosList>();

            // Configuration
        }).
        Build();

        dataService = App.GetService<IDataService>();
        InitializeDistrosList(dataService);
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
