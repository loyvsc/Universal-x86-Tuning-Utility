﻿using Accord.Math.Distances;
using DuoVia.FuzzyStrings;
using GameLib.Plugin.RiotGames.Model;
using Gma.System.MouseKeyHook;
using HidSharp.Utility;
using Microsoft.Win32;
using RTSSSharedMemoryNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Universal_x86_Tuning_Utility.Properties;
using Universal_x86_Tuning_Utility.Scripts;
using Universal_x86_Tuning_Utility.Services;
using Universal_x86_Tuning_Utility.Services.FanControlServices;
using Universal_x86_Tuning_Utility.Services.GameLauncherServices;
using Universal_x86_Tuning_Utility.Services.PresetServices;
using Universal_x86_Tuning_Utility.Services.RyzenAdj;
using Universal_x86_Tuning_Utility.Services.StatisticsServices;
using Universal_x86_Tuning_Utility.Services.SuperResolutionServices.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;
using static Universal_x86_Tuning_Utility.Services.GameLauncherServices.WindowsGameLauncherService;
using Application = System.Windows.Application;
using MessageBox = System.Windows.Forms.MessageBox;
using Settings = Universal_x86_Tuning_Utility.Properties.Settings;
using Window = Avalonia.Controls.Window;
using WindowState = Avalonia.Controls.WindowState;

namespace Universal_x86_Tuning_Utility.Views.Windows;

public partial class MainWindow : Window
{
    public static bool isMini { get; private set; }
    private static DispatcherTimer _miscTimer;
    private static DispatcherTimer _autoReapplyTimer;
    private static DispatcherTimer _autoRestoreTimer;
    private static bool _firstRun = true;
    private static List<GameLauncherItem> _gamesList;
    private int _garbageCounter;

    public MainWindow()
    {
        InitializeComponent();

        SetupTimers();
        SetupUI();
        ApplyStartupSettings();

        SystemEvents.PowerModeChanged += HandlePowerModeChange;
    }

    private void SetupTimers()
    {
        _miscTimer = CreateTimer(1, (s, e) => HandleMiscellaneousTasks(s, e));
        _autoReapplyTimer = CreateTimer((int)Settings.Default.AutoReapplyTime, (s, e) => AutoReapplySettings(s, e));
        _autoRestoreTimer = CreateTimer(1, (s, e) => Controller.AutoRestore_Tick(s, e));
    }

    private void SetupUI()
    {
        tbMain.Title = $"Universal x86 Tuning Utility - {Family.CPUName}";
        Controller.SetUpMagWindow(this);
        Wpf.Ui.Appearance.Watcher.Watch(this, Wpf.Ui.Appearance.BackgroundType.Mica, true);
    }

    private DispatcherTimer CreateTimer(int intervalInSeconds, EventHandler tickHandler)
    {
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(intervalInSeconds)
        };
        timer.Tick += tickHandler;
        timer.Start();
        return timer;
    }

    private async void ApplyStartupSettings()
    {
        if (!Settings.Default.ApplyOnStart) return;

        if (string.IsNullOrWhiteSpace(Settings.Default.CommandString)) return;

        await Task.Run(GetBatteryStatus);

        var isCharging = statuscode is 2 or 6 or 7 or 8;
        var commandString = isCharging ? Settings.Default.acCommandString : Settings.Default.dcCommandString;

        if (string.IsNullOrWhiteSpace(commandString))
        {
            commandString = Settings.Default.CommandString;
        }

        Settings.Default.CommandString = commandString;
        Settings.Default.Save();
        await Task.Run(() => RyzenAdjService.Translate(commandString));

        var presetType = isCharging ? "Charge" : "Discharge";
        ToastNotification.ShowToastNotification($"{presetType} Preset Applied!", $"Your {presetType.ToLower()} preset settings have been applied!");
    }

    private async void HandleMiscellaneousTasks(object sender, EventArgs e)
    {
        if (!File.Exists(Settings.Default.Path + "\\gameData.json") || !Settings.Default.isTrack) return;

        if (!RTSS.RTSSRunning())
        {
            RTSS.startRTSS();
            return;
        }

        if (!_firstRun)
        {
            foreach (var game in _gamesList)
            {
                await ProcessGamePerformanceData(game);
            }

            if (++_garbageCounter >= 20)
            {
                await Task.Run(Garbage.Garbage_Collect);
                _garbageCounter = 0;
            }
        }
        else
        {
            _miscTimer.Stop();
            _gamesList = WindowsGameLauncherService.ReSearchGames();
            await Task.Run(Garbage.Garbage_Collect);
            _firstRun = false;
            _miscTimer.Start();
        }
    }

    private async Task ProcessGamePerformanceData(GameLauncherItem game)
    {
        var appEntries = RTSSSharedMemoryNET.OSD.GetAppEntries().Where(app => (app.Flags & AppFlags.MASK) != AppFlags.None).ToArray();

        foreach (var app in appEntries)
        {
            if (!IsGameMatched(game, app.Name)) continue;

            var gameDataManager = new GameDataService(Settings.Default.Path + "gameData.json");
            var gameData = gameDataManager.GetPreset(game.gameName);

            UpdateGamePerformanceData(app, ref gameData);
            gameDataManager.SavePreset(game.gameName, gameData);
        }
    }

    private static bool IsGameMatched(GameLauncherItem game, string appName)
    {
        return !string.IsNullOrWhiteSpace(game.path) && appName.Contains(game.path, StringComparison.OrdinalIgnoreCase)
               || appName.Contains(ImageService.CleanFileName(game.gameName), StringComparison.OrdinalIgnoreCase)
               || !string.IsNullOrWhiteSpace(game.exe) && appName.Contains(game.exe, StringComparison.OrdinalIgnoreCase);
    }

    private static void UpdateGamePerformanceData(AppEntry app, ref GameData gameData)
    {
        var fpsArray = ParseAndUpdateData(app.InstantaneousFrames, gameData.fpsAvData, out var averageFps);
        var timeSpans = ParseAndUpdateData(app.InstantaneousFrameTime, gameData.msAvData, out var averageTimeSpan);

        gameData.fpsData = averageFps.ToString();
        gameData.fpsAvData = fpsArray;
        gameData.msData = averageTimeSpan.TotalMilliseconds.ToString("0.##");
        gameData.msAvData = timeSpans;
    }

    private static string ParseAndUpdateData<T>(T newData, string existingData, out T average)
    {
        var dataList = existingData.Split(',').Select(s => (T)Convert.ChangeType(s, typeof(T))).ToList();
        dataList.Add(newData);

        if (dataList.Count > 100) dataList.RemoveAt(0);

        average = (T)Convert.ChangeType(dataList.Average(x => Convert.ToDouble(x)), typeof(T));
        return string.Join(",", dataList);
    }

    private static ushort statuscode;

    public static void GetBatteryStatus()
    {
        try
        {
            var batteryClass = new ManagementClass("Win32_Battery");
            var batteries = batteryClass.GetInstances();

            foreach (var battery in batteries)
            {
                statuscode = (ushort)battery["BatteryStatus"];
            }
        }
        catch
        {
            // Handle exceptions if necessary
        }
    }

    private async void AutoReapplySettings(object sender, EventArgs e)
    {
        if (!Settings.Default.AutoReapply || Settings.Default.isAdaptiveModeRunning) return;

        if (!string.IsNullOrWhiteSpace(Settings.Default.CommandString))
        {
            await Task.Run(() => RyzenAdjService.Translate(Settings.Default.CommandString));
        }

        UpdateTimerInterval(_autoReapplyTimer, (int)Settings.Default.AutoReapplyTime);
    }

    private static void UpdateTimerInterval(DispatcherTimer timer, int newInterval)
    {
        if (timer.Interval == TimeSpan.FromSeconds(newInterval)) return;

        timer.Stop();
        timer.Interval = TimeSpan.FromSeconds(newInterval);
        timer.Start();
    }

    static string lastAppliedState = "";
    private async void HandlePowerModeChange(object sender, PowerModeChangedEventArgs e)
    {
        if ((bool)Settings.Default.isAdaptiveModeRunning == false)
        {
            if (e.Mode == PowerModes.StatusChange)
            {
                await Task.Run(() => GetBatteryStatus());
                await Task.Run(() => PremadePresets.InitializePremadePresets());

                if (statuscode == 2 || statuscode == 6 || statuscode == 7 || statuscode == 8)
                {
                    if (Settings.Default.acCommandString != null && Settings.Default.acCommandString != "" && Settings.Default.acPreset != "None")
                    {
                        if (Settings.Default.acPreset.Contains("PM - Eco"))
                        {
                            Settings.Default.premadePreset = 0;
                            Settings.Default.acCommandString = PremadePresets.EcoPreset;
                        }
                        else if (Settings.Default.acPreset.Contains("PM - Bal"))
                        {
                            Settings.Default.premadePreset = 1;
                            Settings.Default.acCommandString = PremadePresets.BalPreset;
                        }
                        else if (Settings.Default.acPreset.Contains("PM - Perf"))
                        {
                            Settings.Default.premadePreset = 2;
                            Settings.Default.acCommandString = PremadePresets.PerformancePreset;
                        }
                        else if (Settings.Default.acPreset.Contains("PM - Ext"))
                        {
                            Settings.Default.premadePreset = 3;
                            Settings.Default.acCommandString = PremadePresets.ExtremePreset;
                        }

                        Settings.Default.CommandString = Settings.Default.acCommandString;
                        Settings.Default.Save();
                        await Task.Run(() => RyzenAdjService.Translate(Settings.Default.acCommandString));

                        if (lastAppliedState != "ac") ToastNotification.ShowToastNotification("Charge Preset Applied!", $"Your charge preset settings have been applied!");
                        lastAppliedState = "ac";
                    }
                }
                else
                {
                    if (Settings.Default.dcCommandString != null && Settings.Default.dcCommandString != "" && Settings.Default.dcPreset != "None")
                    {
                        if (Settings.Default.dcPreset.Contains("PM - Eco"))
                        {
                            Settings.Default.premadePreset = 0;
                            Settings.Default.dcCommandString = PremadePresets.EcoPreset;
                        }
                        else if (Settings.Default.dcPreset.Contains("PM - Bal"))
                        {
                            Settings.Default.premadePreset = 1;
                            Settings.Default.dcCommandString = PremadePresets.BalPreset;
                        }
                        else if (Settings.Default.dcPreset.Contains("PM - Perf"))
                        {
                            Settings.Default.premadePreset = 2;
                            Settings.Default.dcCommandString = PremadePresets.PerformancePreset;
                        }
                        else if (Settings.Default.dcPreset.Contains("PM - Ext"))
                        {
                            Settings.Default.premadePreset = 3;
                            Settings.Default.dcCommandString = PremadePresets.ExtremePreset;
                        }
                        Settings.Default.CommandString = Settings.Default.dcCommandString;
                        Settings.Default.Save();
                        await Task.Run(() => RyzenAdjService.Translate(Settings.Default.dcCommandString));

                        if (lastAppliedState != "dc") ToastNotification.ShowToastNotification("Discharge Preset Applied!", $"Your discharge preset settings have been applied!");
                        lastAppliedState = "dc";
                    }
                }

                if (e.Mode == PowerModes.Resume)
                {
                    if (Settings.Default.resumeCommandString != null && Settings.Default.resumeCommandString != "" && Settings.Default.resumePreset != "None")
                    {
                        if (Settings.Default.resumePreset.Contains("PM - Eco"))
                        {
                            Settings.Default.premadePreset = 0;
                            Settings.Default.resumeCommandString = PremadePresets.EcoPreset;
                        }
                        else if (Settings.Default.resumePreset.Contains("PM - Bal"))
                        {
                            Settings.Default.premadePreset = 1;
                            Settings.Default.resumeCommandString = PremadePresets.BalPreset;
                        }
                        else if (Settings.Default.resumePreset.Contains("PM - Perf"))
                        {
                            Settings.Default.premadePreset = 2;
                            Settings.Default.resumeCommandString = PremadePresets.PerformancePreset;
                        }
                        else if (Settings.Default.resumePreset.Contains("PM - Ext"))
                        {
                            Settings.Default.premadePreset = 3;
                            Settings.Default.resumeCommandString = PremadePresets.ExtremePreset;
                        }
                        Settings.Default.CommandString = Settings.Default.resumeCommandString;
                        Settings.Default.Save();
                        Task.Run(() => RyzenAdjService.Translate(Settings.Default.resumeCommandString));

                        if (lastAppliedState != "resume") ToastNotification.ShowToastNotification("Resume Preset Applied!", $"Your resume preset settings have been applied!");
                        lastAppliedState = "resume";
                    }
                }
            }
        }
    }

    private void UiWindow_StateChanged(object sender, EventArgs e)
    {
        if (this.WindowState == WindowState.Minimized)
        {
            isMini = true;
            this.WindowStyle = WindowStyle.ToolWindow;
            this.ShowInTaskbar = false;
        }
        else
        {
            isMini = false;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ShowInTaskbar = true;
        }
    }

    private void NotifyIcon_LeftClick(Wpf.Ui.Controls.NotifyIcon sender, RoutedEventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            WindowState = WindowState.Minimized;
        }
        else
        {
            WindowState = WindowState.Normal;
            Activate();
        }
    }

    private void miClose_Click(object sender, RoutedEventArgs e)
    {
        Settings.Default.isAdaptiveModeRunning = false;
        Settings.Default.Save();
        Controller.magWindow?.Dispose();
        Application.Current.Shutdown();
    }

    private void UiWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (Settings.Default.MinimizeClose)
        {
            WindowState = WindowState.Minimized;
            e.Cancel = true;
            return;
        }
            
        Settings.Default.isAdaptiveModeRunning = false;
        Settings.Default.Save();
        Controller.magWindow?.Dispose();
        WindowsFanControlService.DisableFanControl();
    }

    private void MainWindowLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Settings.Default.StartMini)
        {
            this.WindowState = WindowState.Minimized;
        }
        else
        {
            if (GetSystemInfo.Manufacturer.ToUpper().Contains("AYANEO") || GetSystemInfo.Manufacturer.ToUpper().Contains("GPD") || GetSystemInfo.Product.ToUpper().Contains("ONEXPLAYER"))
            {
                int displayCount = Screen.AllScreens.Length;
                if (displayCount < 2)
                {
                    this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                    this.WindowState = WindowState.Maximized;
                }
            }
        }

        PremadePresets.InitializePremadePresets();
    }
}