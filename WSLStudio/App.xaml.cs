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
using WSLStudio.Views.Dialogs;

namespace WSLStudio;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging

    public static bool IsDistributionProcessing { get; set; } = false;

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
        try
        {
            var dialog = new ContentDialog()
            {
                Title = "WSL is not detected on the system",
                Content = "Check if WSL is supported or installed on your system.",
                CloseButtonText = "Ok",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = MainWindow.Content.XamlRoot,
            };

            await dialog.ShowAsync();
            MainWindow.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            MainWindow.Close();
        }
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
        try
        {
            var dialog = new ContentDialog()
            {
                Title = "Virtualization Disabled in Firmware",
                Content = "Enable the optional 'Virtual Computer Platform' component and ensure that virtualization is enabled in the BIOS.",
                CloseButtonText = "Ok",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = MainWindow.Content.XamlRoot,
            };

            await dialog.ShowAsync();
            MainWindow.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            MainWindow.Close();
        }
    }

    public static void ShowVirtualizationDisabledDialog()
    {
        if (MainWindow.Content is FrameworkElement fe)
        {
            fe.Loaded += (ss, se) => VirtualizationDisabled();
        }
    }

    public static async void ShowSnapshotProcessingDialog()
    {
        try
        {
            var dialog = new ContentDialog()
            {
                Title = "WSL Studio is currently creating a distribution",
                CloseButtonText = "Close",
                XamlRoot = App.MainWindow.Content.XamlRoot,
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
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
            services.AddTransient<IInfoBarService, InfoBarService>();
            services.AddSingleton<IDistributionService, DistributionService>();
            services.AddSingleton<ISnapshotService, SnapshotService>();
            services.AddSingleton<IWslService, WslService>();
            

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddSingleton<DistrosListDetailsVM>();
            services.AddSingleton<DistrosListDetailsView>();
            services.AddSingleton<DisplaySnapshotsVM>();
            services.AddSingleton<DisplaySnapshotsView>();

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
