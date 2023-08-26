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

using Community.Wsl.Sdk;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Controls;

namespace WSLStudio;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging

    public IHost Host
    {
        get;
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

    public static async Task NoWslDialog()
    {
        var contentDialog = App.GetService<IDialogBuilderService>()
            .SetTitle("Impossible to detect WSL")
            .SetContent("Check if WSL is supported or installed on your system")
            .SetCloseButtonText("Ok")
            .SetDefaultButton(ContentDialogButton.Close)
            .SetXamlRoot(MainWindow.Content.XamlRoot)
            .Build();

        await contentDialog.ShowAsync();
        MainWindow.Close();
    }

    public static void ShowNoWslDialog()
    {
        if (MainWindow.Content is FrameworkElement fe)
        {
            fe.Loaded += (ss, se) => NoWslDialog();
        }

    }

    public static async Task VirtualizationDisabled()
    {
        var contentDialog = App.GetService<IDialogBuilderService>()
            .SetTitle("Virtualization Disabled in Firmware")
            .SetContent("Enable the optional 'Virtual Computer Platform' component and ensure that virtualization is enabled in the BIOS.")
            .SetCloseButtonText("Ok")
            .SetDefaultButton(ContentDialogButton.Close)
            .SetXamlRoot(MainWindow.Content.XamlRoot)
            .Build();

        await contentDialog.ShowAsync();
        MainWindow.Close();
    }

    public static void ShowVirtualizationDisabledDialog()
    {
        if (MainWindow.Content is FrameworkElement fe)
        {
            fe.Loaded += (ss, se) => VirtualizationDisabled();
        }
    }

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
            services.AddTransient<IDialogBuilderService, DialogBuilderService>();
            services.AddTransient<IInfoBarService, InfoBarService>();
            services.AddSingleton<IDistributionService, DistributionService>();
            services.AddSingleton<ISnapshotService, SnapshotService>();
            services.AddSingleton<IWslService, WslService>();
            

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<DistrosListDetailsVM>();
            services.AddTransient<SnapshotsVM>();
            services.AddTransient<DistrosListDetailsView>();

            // Configuration
        }).
        Build();
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
        Console.WriteLine("App_UnhandledException caught : " + e.Message);
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        await App.GetService<IActivationService>().ActivateAsync(args);

        var wslService = App.GetService<IWslService>();

        if (!wslService.CheckWsl())
        {
            ShowNoWslDialog();
        }

        var virtualizationEnabled = wslService.CheckHypervisor();
        if (!virtualizationEnabled)
        {
            ShowVirtualizationDisabledDialog();
        }
    }
}
