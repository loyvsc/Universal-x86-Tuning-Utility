using Avalonia;
using System;
using ApplicationCore.Interfaces;
using DAL.Services;
using Splat;
using Universal_x86_Tuning_Utility.Linux.Services;
using Universal_x86_Tuning_Utility.Linux.Services.GPUs;

namespace Universal_x86_Tuning_Utility.Linux;

class LinuxApplication : App
{
    public override void Initialize()
    {
        base.Initialize();

        SplatRegistrations.RegisterLazySingleton<IASUSWmiService, LinuxAsusWmiService>();
        SplatRegistrations.RegisterLazySingleton<ICliService, LinuxCliService>();
        SplatRegistrations.RegisterLazySingleton<ICpuControlService, LinuxCpuControlService>();
        SplatRegistrations.RegisterLazySingleton<IDisplayInfoService, LinuxDisplayInfoService>();
        SplatRegistrations.RegisterLazySingleton<IFanControlService, LinuxFanControlService>();
        SplatRegistrations.RegisterLazySingleton<IGameLauncherService, LinuxGameLauncherService>();
        SplatRegistrations.RegisterLazySingleton<IAmdGpuService, LinuxAmdGpuService>();
        SplatRegistrations.RegisterLazySingleton<INvidiaGpuService, LinuxNvidiaGpuService>();
        SplatRegistrations.RegisterLazySingleton<IIntelManagementService, LinuxIntelManagementService>();
        SplatRegistrations.RegisterLazySingleton<IPowerPlanService, LinuxPowerPlanService>();
        SplatRegistrations.RegisterLazySingleton<ISensorsService, LinuxSensorsService>();
        SplatRegistrations.RegisterLazySingleton<IRtssService, LinuxRtssService>();
        SplatRegistrations.RegisterLazySingleton<IStressTestService, LinuxStressTestService>();
        SplatRegistrations.RegisterLazySingleton<ISystemBootService, LinuxSystemBootService>();
        SplatRegistrations.RegisterLazySingleton<ISystemInfoService, LinuxSystemInfoService>();
        SplatRegistrations.RegisterLazySingleton<IUpdateService, UpdateService>();
        SplatRegistrations.RegisterLazySingleton<IUpdateInstallerService, LinuxUpdateInstallerService>();
        SplatRegistrations.RegisterLazySingleton<IBatteryInfoService, LinuxBatteryInfoService>();
        
        SplatRegistrations.SetupIOC();
    }
}

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<LinuxApplication>()
            .UseX11()
            .UseSkia()
            .LogToTrace();
}