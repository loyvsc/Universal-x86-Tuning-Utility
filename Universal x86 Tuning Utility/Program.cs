using Avalonia;
using Avalonia.ReactiveUI;
using DesktopNotifications.Avalonia;
using Splat;

namespace Universal_x86_Tuning_Utility;

public class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        var appBuilder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .SetupDesktopNotifications(out var notificationManager)
            .LogToTrace()
            .UseReactiveUI();
        
        Locator.CurrentMutable.RegisterConstant(notificationManager);
        
        return appBuilder;
    }
}