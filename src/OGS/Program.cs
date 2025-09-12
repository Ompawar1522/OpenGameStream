using Avalonia;
using OGS.Core.Common;
using OGS.Di;
using OGS.Plaform.Windows.ViewModels;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OGS;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WinQuickSwitchVm))]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>(CreateApp)
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static App CreateApp()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsDiContainer().Resolve<App>().Value;
        }

        throw new PlatformNotSupportedException();
    }
}
