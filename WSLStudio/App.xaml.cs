﻿using CommunityToolkit.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using WSLStudio.Activation;
using WSLStudio.Contracts.Services;
using WSLStudio.Core.Contracts.Services;
using WSLStudio.Core.Services;
using WSLStudio.Helpers;
using WSLStudio.Services;
using WSLStudio.ViewModels;
using WSLStudio.Views;

using Microsoft.UI.Xaml.Controls;
using WSLStudio.Views.Dialogs;
using Serilog;
using Serilog.Events;
using WSLStudio.Contracts.Services.Storage;
using WSLStudio.Contracts.Services.UserInterface;
using WSLStudio.Services.Storage;
using WSLStudio.Services.UserInterface;

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

    private static readonly string ROAMING_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly Windows.Storage.StorageFolder LocalFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

    private const string APP_FOLDER_NAME = "WslStudio";
    private const string TMP_FOLDER_NAME = ".tmp";
    private const string LOG_FOLDER_NAME = ".log";

    public static string? DistroDirPath { get; set; } = Path.Combine(ROAMING_PATH, APP_FOLDER_NAME);
    public static string? AppDirPath { get; set; }
    public static string? TmpDirPath { get; set; }
    public static string? LogDirPath { get; set; }

    public IHost Host
    {
        get;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    private static async Task CreateProjectFolders()
    {
        await Task.Run(() =>
        {
            Log.Information("Creating project folders ...");
            AppDirPath = FilesHelper.CreateDirectory(LocalFolder.Path, APP_FOLDER_NAME);
            if (AppDirPath == null)
            {
                Log.Error("Cannot create project folders");
                MainWindow.Close();
            }
            else
            {
                TmpDirPath = FilesHelper.CreateDirectory(AppDirPath, TMP_FOLDER_NAME);
                LogDirPath = FilesHelper.CreateDirectory(AppDirPath, LOG_FOLDER_NAME);
            }
        });
    }

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
            Log.Error($"Failed to open NoWSL dialog - Caused by exception : {ex}");
            MainWindow.Close();
        }
    }

    public static async Task ShowNoWslDialog()
    {
        var dispatchQueue = DispatcherQueue.GetForCurrentThread();
        await Task.Run(async () =>
            await dispatchQueue.EnqueueAsync(async () => await NoWslDialog()));
    }

    public static async Task VirtualizationDisabledDialog()
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
            Log.Error($"Failed to open VirtualizationDisabled dialog - Caused by exception : {ex}");
            MainWindow.Close();
        }
    }

    public static async Task ShowVirtualizationDisabledDialog()
    {
        var dispatchQueue = DispatcherQueue.GetForCurrentThread();
        await Task.Run(async () =>
            await dispatchQueue.EnqueueAsync(async () => await VirtualizationDisabledDialog()));
    }

    public static void InstallWslFromMicrosoftStoreCommand(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            WslHelper.InstallWslFromMicrosoftStore();
        }
        catch (Exception ex)
        {
            Log.Error($"Cannot installed WSL from the Microsoft Store - Caused by exception : {ex}");
        }
    }


    public async Task InstallWslFromMicrosoftStoreDialog()
    {
        try
        {
            var dialog = new ContentDialog()
            {
                Title = "WSL is not installed from the Microsoft Store",
                Content =
                    "WSL Studio cannot works properly, do you want to install WSL from the Microsoft Store ?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = MainWindow.Content.XamlRoot,
            };

            dialog.PrimaryButtonClick += InstallWslFromMicrosoftStoreCommand;

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open VirtualizationDisabled dialog - Caused by exception : {ex}");
            MainWindow.Close();
        }
    }

    public async Task ShowInstallWslFromMicrosoftStoreDialog()
    {
        var dispatcher = DispatcherQueue.GetForCurrentThread();

        await Task.Run(async () =>
        {
            await dispatcher.EnqueueAsync(async () =>
            {
                await InstallWslFromMicrosoftStoreDialog();
            });
        });
    }

    public static async void ShowIsProcessingDialog()
    {
        try
        {
            var dialog = new ContentDialog()
            {
                Title = "WSL Studio is currently creating a distribution",
                CloseButtonText = "Close",
                XamlRoot = MainWindow.Content.XamlRoot,
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open ShowSnapshotProcessing dialog - Caused by exception : {ex}");
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
            services.AddSingleton<IDistributionInfosService, DistributionInfosService>();
            services.AddSingleton<ISnapshotService, SnapshotService>();
            services.AddSingleton<IFileStorageService, FlatFileStorageService>();
            services.AddSingleton<IFileStorageService, JsonFileStorageService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddSingleton<DistrosListDetailsVM>();
            services.AddSingleton<DistrosListDetailsView>();
            services.AddSingleton<DisplaySnapshotsVM>();
            services.AddSingleton<DisplaySnapshotsView>();

            // Configuration
        }).
        UseSerilog().
        Build();
        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs ex)
    {
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
        Log.Debug($"App_UnhandledException caught : {ex}");
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        await App.GetService<IActivationService>().ActivateAsync(args);

        await CreateProjectFolders();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(LogDirPath, "log.txt"),
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();

        var wslInstalled = await WslHelper.CheckWslMicrosoftStore();
        if (!wslInstalled)
        {
            await ShowInstallWslFromMicrosoftStoreDialog();
        }

        if (!WslHelper.CheckWsl())
        {
            await ShowNoWslDialog();
        }

        var virtualizationEnabled = WslHelper.CheckHypervisor();
        if (!virtualizationEnabled)
        {
            await ShowVirtualizationDisabledDialog();
        }

       
    }
}
