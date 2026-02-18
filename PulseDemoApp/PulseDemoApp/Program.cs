using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinRT;

namespace PulseDemoApp;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        XamlCheckProcessRequirements();

        ComWrappersSupport.InitializeComWrappers();

        bool redirected = Bootstrapper.HandleRedirection();
        if (!redirected)
        {
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
    }

    [DllImport("Microsoft.ui.xaml.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern void XamlCheckProcessRequirements();
}
