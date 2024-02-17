using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Serilog;

namespace WSLStudio;

/**
 * This class makes WSL Studio single-instanced.
 * For more details : https://blogs.windows.com/windowsdeveloper/2022/01/28/making-the-app-single-instanced-part-3/
 */
public class Program
{
    [STAThread]
    static async Task<int> Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        bool isRedirect = await DecideRedirection();
        if (!isRedirect)
        {
            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
        await Log.CloseAndFlushAsync();
        return 0;
    }

    private static async Task<bool> DecideRedirection()
    {
        bool isRedirect = false;
        AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
        ExtendedActivationKind kind = args.Kind;
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey("randomKey");

        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += OnActivated;
        }
        else
        {
            isRedirect = true;
            await keyInstance.RedirectActivationToAsync(args);
        }
        return isRedirect;
    }

    private static void OnActivated(object sender, AppActivationArguments args)
    {
        ExtendedActivationKind kind = args.Kind;
    }
}