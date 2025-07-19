﻿using Avalonia;
using System;
using ApplicationCore.Interfaces;
using Splat;
using Universal_x86_Tuning_Utility.Windows.Services;
using Universal_x86_Tuning_Utility.Windows.Services.Asus;
using Universal_x86_Tuning_Utility.Windows.Services.GPUs;
using Universal_x86_Tuning_Utility.Windows.Services.SystemInfoServices;

namespace Universal_x86_Tuning_Utility.Windows;

public partial class WindowsApp : App
{
    public override void Initialize()
    {
        base.Initialize();

        SplatRegistrations.RegisterLazySingleton<IASUSWmiService, WindowsAsusWmiService>();
        SplatRegistrations.RegisterLazySingleton<ICliService, WindowsCliService>();
        SplatRegistrations.RegisterLazySingleton<ICpuControlService, WindowsCpuControlService>();
        SplatRegistrations.RegisterLazySingleton<IDisplayInfoService, WindowsDisplayInfoService>();
        SplatRegistrations.RegisterLazySingleton<IFanControlService, WindowsFanControlService>();
        SplatRegistrations.RegisterLazySingleton<IGameLauncherService, WindowsGameLauncherService>();
        SplatRegistrations.RegisterLazySingleton<IAmdGpuService, WindowsAmdGpuService>();
        SplatRegistrations.RegisterLazySingleton<INvidiaGpuService, WindowsNvidiaGpuService>();
        SplatRegistrations.RegisterLazySingleton<IIntelManagementService, WindowsIntelManagementService>();
        SplatRegistrations.RegisterLazySingleton<IPowerPlanService, WindowsPowerPlanService>();
        SplatRegistrations.RegisterLazySingleton<IRyzenAdjService, RyzenAdjService>();
        SplatRegistrations.RegisterLazySingleton<ISensorsService, WindowsSensorsService>();
        SplatRegistrations.RegisterLazySingleton<IRtssService, WindowsRtssService>();
        SplatRegistrations.RegisterLazySingleton<IStressTestService, WindowsStressTestService>();
        SplatRegistrations.RegisterLazySingleton<ISystemBootService, WindowsSystemBootService>();
        SplatRegistrations.RegisterLazySingleton<ISystemInfoService, WindowsSystemInfoService>();
        SplatRegistrations.RegisterLazySingleton<IUpdateInstallerService, WindowsUpdateInstallerService>();
        SplatRegistrations.RegisterLazySingleton<IBatteryInfoService, WindowsBatteryInfoService>();
        
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
        => AppBuilder.Configure<WindowsApp>()
            .UseWin32()
            .WithInterFont()
            .LogToTrace();
}