﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ApplicationCore.Enums;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using ApplicationCore.Utilities;
using Avalonia.Threading;
using DAL.Services;
using DesktopNotifications;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using RTSSSharedMemoryNET;
using Universal_x86_Tuning_Utility.Extensions;
using Universal_x86_Tuning_Utility.Properties;
using Universal_x86_Tuning_Utility.Services.FanControlServices;
using Universal_x86_Tuning_Utility.Services.GameLauncherServices;
using Universal_x86_Tuning_Utility.Services.PresetServices;
using Universal_x86_Tuning_Utility.Services.RyzenAdj;
using Universal_x86_Tuning_Utility.Services.StatisticsServices;
using Universal_x86_Tuning_Utility.Services.SuperResolutionServices.Windows;
using PowerModeChangedEventArgs = ApplicationCore.Events.PowerModeChangedEventArgs;

namespace Universal_x86_Tuning_Utility.ViewModels;

public partial class MainWindowViewModel : NotifyPropertyChangedBase
{
    private readonly ISystemInfoService _systemInfoService;
    private readonly INotificationManager _toastNotificationManager;
    private readonly IRyzenAdjService _ryzenAdjService;
    private readonly IRtssService _rtssService;
    private readonly IPowerPlanService _powerPlanService;
    private readonly IGameLauncherService _gameLauncherService;
    private readonly IImageService _imageService;
    private readonly IFanControlService _fanControlService;
    private bool _isInitialized = false;
    private string _lastAppliedState = "";

    [ObservableProperty]
    private ObservableCollection<INavigationControl> _navigationItems = new();

    [ObservableProperty]
    private ObservableCollection<INavigationControl> _navigationFooter = new();

    [ObservableProperty]
    private ObservableCollection<MenuItem> _trayMenuItems = new();

    [ObservableProperty]
    private string _downloads = "Downloads: ";

    [ObservableProperty]
    private bool _isDownloads = false;

    public string Title
    {
        get => _title;
        set => SetValue(ref _title, value);
    }

    private readonly DispatcherTimer _miscTimer;
    private readonly DispatcherTimer _autoReapplyTimer;
    private readonly DispatcherTimer _autoRestoreTimer;
    
    private string _title;
    public static bool isMini { get; private set; }
    private static bool _firstRun = true;
    private static List<GameLauncherItem> _gamesList;

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, 
                               ISystemInfoService systemInfoService,
                               INotificationManager toastNotificationManager,
                               IRyzenAdjService ryzenAdjService,
                               IRtssService rtssService,
                               IPowerPlanService powerPlanService,
                               IGameLauncherService gameLauncherService,
                               IImageService imageService,
                               IFanControlService fanControlService)
    {
        _systemInfoService = systemInfoService;
        _toastNotificationManager = toastNotificationManager;
        _ryzenAdjService = ryzenAdjService;
        _rtssService = rtssService;
        _powerPlanService = powerPlanService;
        _gameLauncherService = gameLauncherService;
        _imageService = imageService;
        _fanControlService = fanControlService;
        _powerPlanService.PowerModeChanged += OnPowerModeChange;

        _miscTimer = CreateTimer(1, (s, e) => HandleMiscellaneousTasks(s, e));
        _autoReapplyTimer = CreateTimer(Settings.Default.AutoReapplyTime, (s, e) => AutoReapplySettings(s, e));
        _autoRestoreTimer = CreateTimer(1, (s, e) => WindowsSuperResolutionService.AutoRestore_Tick(s, e));
        
        InitializeViewModel();
        
        Title = $"Universal x86 Tuning Utility - {_systemInfoService.Cpu.Name}";
    }
    
    private async void OnPowerModeChange(PowerModeChangedEventArgs e)
    {
        if ((bool)Settings.Default.isAdaptiveModeRunning == false)
        {
            if (e.BatteryStatus == BatteryStatus.Charging)
            {
                await Task.Run(() => PremadePresets.InitializePremadePresets());

                var batteryStatus = _systemInfoService.GetBatteryStatus();

                if (batteryStatus == BatteryStatus.Charging)
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

                       await _ryzenAdjService.Translate(Settings.Default.acCommandString);

                       if (_lastAppliedState != "ac")
                       {
                           await _toastNotificationManager.ShowTextNotification("Charge Preset Applied!", $"Your charge preset settings have been applied!");
                       }
                       _lastAppliedState = "ac";
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

                        if (_lastAppliedState != "dc")
                        {
                            _toastNotificationManager.ShowToastNotification("Discharge Preset Applied!", $"Your discharge preset settings have been applied!");
                        }
                        _lastAppliedState = "dc";
                    }
                }

                if (e.PowerMode == PowerMode.Resume) // 
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

                        if (_lastAppliedState != "resume") ToastNotification.ShowToastNotification("Resume Preset Applied!", $"Your resume preset settings have been applied!");
                        _lastAppliedState = "resume";
                    }
                }
            }
        }
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
    
    private async void HandleMiscellaneousTasks(object sender, EventArgs e)
    {
        if (!File.Exists(Settings.Default.Path + "\\gameData.json") || !Settings.Default.isTrack) return;

        if (!_rtssService.IsRTSSRunning())
        {
            _rtssService.Start();
            return;
        }

        if (!_firstRun)
        {
            foreach (var game in _gamesList)
            {
                await ProcessGamePerformanceData(game);
            }
        }
        else
        {
            _miscTimer.Stop();
            _gamesList = _gameLauncherService.ReSearchGames();
            _firstRun = false;
            _miscTimer.Start();
        }
    }

    private async Task ApplyStartupSettings()
    {
        if (!Settings.Default.ApplyOnStart || string.IsNullOrWhiteSpace(Settings.Default.CommandString)) return;

        var statusCode = _systemInfoService.GetBatteryStatus();

        var isCharging = statusCode == BatteryStatus.Charging;
        var commandString = isCharging ? Settings.Default.acCommandString : Settings.Default.dcCommandString;

        if (string.IsNullOrWhiteSpace(commandString))
        {
            commandString = Settings.Default.CommandString;
        }

        Settings.Default.CommandString = commandString;
        Settings.Default.Save();
        
        await _ryzenAdjService.Translate(commandString);

        var presetType = isCharging ? "Charge" : "Discharge";
        await _toastNotificationManager.ShowTextNotification($"{presetType} Preset Applied!", $"Your {presetType.ToLower()} preset settings have been applied!");
    }
    
    private void InitializeViewModel()
    {
        if (_systemInfoService.Cpu.Manufacturer == Manufacturer.Intel)
        {
            NavigationItems = new ObservableCollection<INavigationControl>
            {
                new NavigationViewItem()
                {
                    Content = "Home", 
                    PageTag = "dashboard",
                    Icon = SymbolRegular.Home20,
                    PageType = typeof(Views.Pages.DashboardPage)
                },
                //new NavigationItem()
                //{
                //    Content = "Premade",
                //    PageTag = "premade",
                //    Icon = SymbolRegular.Predictions20,
                //    PageType = typeof(Views.Pages.Premade)
                //},
                new NavigationItem()
                {
                    Content = "Custom",
                    PageTag = "custom",
                    Icon = SymbolRegular.Book20,
                    PageType = typeof(Views.Pages.CustomPresetsPage)
                },
                new NavigationItem()
                {
                    Content = "Adaptive",
                    PageTag = "adaptive",
                    Icon = SymbolRegular.Radar20,
                    PageType = typeof(Views.Pages.AdaptivePage)
                },
                new NavigationItem()
                {
                    Content = "Games",
                    PageTag = "games",
                    Icon = SymbolRegular.Games20,
                    PageType = typeof(Views.Pages.GamesPage)
                },
                new NavigationItem()
                {
                    Content = "Auto",
                    PageTag = "auto",
                    Icon = SymbolRegular.Transmission20,
                    PageType = typeof(Views.Pages.AutomationsPage)
                },
                //new NavigationItem()
                //{
                //    Content = "Fan",
                //    PageTag = "fan",
                //    Icon = SymbolRegular.WeatherDuststorm20,
                //    PageType = typeof(Views.Pages.FanControl)
                //},
                // new NavigationItem()
                //{
                //    Content = "Magpie",
                //    PageTag = "magpie",
                //    Icon = SymbolRegular.FullScreenMaximize20,
                //    PageType = typeof(Views.Pages.DataPage)
                //},
                new NavigationItem()
                {
                    Content = "Info",
                    PageTag = "info",
                    Icon = SymbolRegular.Info20,
                    PageType = typeof(Views.Pages.SystemInfoPage)
                }
            };

            NavigationFooter = new ObservableCollection<INavigationControl>
            {
                new NavigationItem()
                {
                    Content = "Settings",
                    PageTag = "settings",
                    Icon = SymbolRegular.Settings20,
                    PageType = typeof(Views.Pages.SettingsPage)
                }
            };

            TrayMenuItems = new ObservableCollection<MenuItem>
            {
                new MenuItem
                {
                    Header = "Home",
                    Tag = "tray_home"
                }
            };
        }
        else
        {
            NavigationItems = new ObservableCollection<INavigationControl>
            {
                new NavigationItem()
                {
                    Content = "Home",
                    PageTag = "dashboard",
                    Icon = SymbolRegular.Home20,
                    PageType = typeof(Views.Pages.DashboardPage)
                },
                new NavigationItem()
                {
                    Content = "Premade",
                    PageTag = "premade",
                    Icon = SymbolRegular.Predictions20,
                    PageType = typeof(Views.Pages.PremadePage)
                },
                new NavigationItem()
                {
                    Content = "Custom",
                    PageTag = "custom",
                    Icon = SymbolRegular.Book20,
                    PageType = typeof(Views.Pages.CustomPresetsPage)
                },
                new NavigationItem()
                {
                    Content = "Adaptive",
                    PageTag = "adaptive",
                    Icon = SymbolRegular.Radar20,
                    PageType = typeof(Views.Pages.AdaptivePage)
                },
                new NavigationItem()
                {
                    Content = "Games",
                    PageTag = "games",
                    Icon = SymbolRegular.Games20,
                    PageType = typeof(Views.Pages.GamesPage)
                },
                new NavigationItem()
                {
                    Content = "Auto",
                    PageTag = "auto",
                    Icon = SymbolRegular.Transmission20,
                    PageType = typeof(Views.Pages.AutomationsPage)
                },
                //new NavigationItem()
                //{
                //    Content = "Fan",
                //    PageTag = "fan",
                //    Icon = SymbolRegular.WeatherDuststorm20,
                //    PageType = typeof(Views.Pages.FanControl)
                //},
                // new NavigationItem()
                //{
                //    Content = "Magpie",
                //    PageTag = "magpie",
                //    Icon = SymbolRegular.FullScreenMaximize20,
                //    PageType = typeof(Views.Pages.DataPage)
                //},
                new NavigationItem()
                {
                    Content = "Info",
                    PageTag = "info",
                    Icon = SymbolRegular.Info20,
                    PageType = typeof(Views.Pages.SystemInfoPage)
                }
            };

            NavigationFooter = new ObservableCollection<INavigationControl>
            {
                new NavigationItem()
                {
                    Content = "Settings",
                    PageTag = "settings",
                    Icon = SymbolRegular.Settings20,
                    PageType = typeof(Views.Pages.SettingsPage)
                }
            };

            TrayMenuItems = new ObservableCollection<MenuItem>
            {
                new MenuItem
                {
                    Header = "Home",
                    Tag = "tray_home"
                }
            };
        }

        _isInitialized = true;
    }
        
    private ICommand _navigateCommand;
    public ICommand NavigateCommand => _navigateCommand ??= new RelayCommand<string>(OnNavigate);

    private void OnNavigate(string parameter)
    {
        switch (parameter)
        {
            case "download":
                Process.Start(new ProcessStartInfo("https://github.com/JamesCJ60/Universal-x86-Tuning-Utility/releases") { UseShellExecute = true });
                return;

            case "discord":
                Process.Start(new ProcessStartInfo("https://www.discord.gg/3EkYMZGJwq") { UseShellExecute = true });
                return;

            case "support":
                Process.Start(new ProcessStartInfo("https://www.paypal.com/paypalme/JamesCJ60") { UseShellExecute = true });
                Process.Start(new ProcessStartInfo("https://patreon.com/uxtusoftware") { UseShellExecute = true });
                return;
        }
    }
    
    private void SetupUI()
    {
        WindowsSuperResolutionService.SetUpMagWindow(this);
    }

    private async Task ProcessGamePerformanceData(GameLauncherItem game)
    {
        var appEntries = RTSSSharedMemoryNET.OSD.GetAppEntries()
            .Where(app => (app.Flags & AppFlags.MASK) != AppFlags.None).ToArray();

        foreach (var app in appEntries)
        {
            if (!IsGameMatched(game, app.Name)) continue;

            var gameDataManager = new GameDataService(Settings.Default.Path + "gameData.json");
            var gameData = gameDataManager.GetPreset(game.GameName);

            UpdateGamePerformanceData(app, gameData);
            gameDataManager.SavePreset(game.GameName, gameData);
        }
    }

    private bool IsGameMatched(GameLauncherItem game, string appName)
    {
        return !string.IsNullOrWhiteSpace(game.Path) && appName.Contains(game.Path, StringComparison.OrdinalIgnoreCase)
               || appName.Contains(_imageService.CleanFileName(game.GameName), StringComparison.OrdinalIgnoreCase)
               || !string.IsNullOrWhiteSpace(game.Executable) && appName.Contains(game.Executable, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateGamePerformanceData(AppEntry app, GameData gameData)
    {
        var fpsArray = ParseAndUpdateData(app.InstantaneousFrames, gameData.FpsAverageData, out var averageFps);
        var timeSpans = ParseAndUpdateData(app.InstantaneousFrameTime, gameData.MsAverageData, out var averageTimeSpan);

        gameData.FpsData = averageFps.ToString();
        gameData.FpsAverageData = fpsArray;
        gameData.MsData = averageTimeSpan.TotalMilliseconds.ToString("0.##");
        gameData.MsAverageData = timeSpans;
    }

    private string ParseAndUpdateData<T>(T newData, string existingData, out T average)
    {
        var dataList = existingData.Split(',').Select(s => (T)Convert.ChangeType(s, typeof(T))).ToList();
        dataList.Add(newData);

        if (dataList.Count > 100) dataList.RemoveAt(0);

        average = (T)Convert.ChangeType(dataList.Average(x => Convert.ToDouble(x)), typeof(T));
        return string.Join(",", dataList);
    }

    private async Task AutoReapplySettings(object sender, EventArgs e)
    {
        if (!Settings.Default.AutoReapply || Settings.Default.isAdaptiveModeRunning) return;

        if (!string.IsNullOrWhiteSpace(Settings.Default.CommandString))
        {
            await _ryzenAdjService.Translate(Settings.Default.CommandString))
        }

        UpdateTimerInterval(_autoReapplyTimer, Settings.Default.AutoReapplyTime);
    }

    private static void UpdateTimerInterval(DispatcherTimer timer, int newInterval)
    {
        if (timer.Interval == TimeSpan.FromSeconds(newInterval)) return;

        timer.Stop();
        timer.Interval = TimeSpan.FromSeconds(newInterval);
        timer.Start();
    }
    

    public void Dispose()
    {
        Settings.Default.isAdaptiveModeRunning = false;
        Settings.Default.Save();
        WindowsSuperResolutionService.MagWindow?.Dispose();
        _fanControlService.DisableFanControl();
    }
}