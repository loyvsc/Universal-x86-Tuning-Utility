﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ApplicationCore.Interfaces;
using Universal_x86_Tuning_Utility.Properties;

namespace Universal_x86_Tuning_Utility.Services.StatisticsServices;

public class WindowsRtssService : IRtssService
{
    /// <summary>
    /// 0 - fps limit is disabled
    /// </summary>
    public int FpsLimit
    {
        get
        {
            if (IsRTSSRunning())
            {
                LoadProfile();
                TryGetProfileProperty("FramerateLimit", out int fpsLimit);
                return fpsLimit;
            }

            return 0;
        }
        set
        {
            if (IsRTSSRunning())
            {
                LoadProfile();
                TrySetProfileProperty("FramerateLimit", value);
                SaveProfile();
                UpdateProfiles();
            }
        }
    }
    
    private const string GLOBAL_PROFILE = "";
    
    private readonly string _rtssExecutableFilePath = Path.Combine(Settings.Default.directoryRTSS + "RTSS.exe");
    private readonly Process _rtssProcess;

    public WindowsRtssService()
    {
        _rtssProcess = new Process();
        _rtssProcess.StartInfo = new ProcessStartInfo()
        {
            FileName = _rtssExecutableFilePath
        };
    }
    
    public void Start()
    {
        if (File.Exists(_rtssExecutableFilePath))
        {
            _rtssProcess.Start();
        }
    }

    public void Stop()
    {
        _rtssProcess.Close();
    }
    
    public bool IsRTSSRunning()
    {
        var rtssProcesses = Process.GetProcessesByName("rtss");
        return rtssProcesses.Length != 0;
    }

    private bool TryGetProfileProperty<T>(string propertyName, out T value)
    {
        var bytes = new byte[Marshal.SizeOf<T>()];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        value = default;
        try
        {
            if (!GetProfileProperty(propertyName, handle.AddrOfPinnedObject(), (uint)bytes.Length))
                return false;

            value = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            handle.Free();
        }
    }

    private bool TrySetProfileProperty<T>(string propertyName, T value)
    {
        var bytes = new byte[Marshal.SizeOf<T>()];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            return SetProfileProperty(propertyName, handle.AddrOfPinnedObject(), (uint)bytes.Length);
        }
        catch
        {
            return false;
        }
        finally
        {
            handle.Free();
        }
    }

    #region PInvoke Declarations

    private const uint WM_APP = 0x8000;
    private const uint WM_RTSS_UPDATESETTINGS = WM_APP + 100;
    private const uint WM_RTSS_SHOW_PROPERTIES = WM_APP + 102;

    private const uint RTSSHOOKSFLAG_OSD_VISIBLE = 1;
    private const uint RTSSHOOKSFLAG_LIMITER_DISABLED = 4;
    
    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("RTSSHooks64.dll")]
    private static extern uint SetFlags(uint dwAND, uint dwXOR);

    [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
    private static extern void LoadProfile(string profile = GLOBAL_PROFILE);

    [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
    private static extern void SaveProfile(string profile = GLOBAL_PROFILE);

    [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
    private static extern void DeleteProfile(string profile = GLOBAL_PROFILE);

    [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
    private static extern bool GetProfileProperty(string propertyName, IntPtr value, uint size);

    [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
    private static extern bool SetProfileProperty(string propertyName, IntPtr value, uint size);

    [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
    private static extern void ResetProfile(string profile = GLOBAL_PROFILE);

    [DllImport("RTSSHooks64.dll", CharSet = CharSet.Ansi)]
    private static extern void UpdateProfiles();

    #endregion

    private void PostMessage(uint Msg, IntPtr wParam, IntPtr lParam)
    {
        var hWnd = FindWindow(null, "RTSS");
        if (hWnd == IntPtr.Zero)
            hWnd = FindWindow(null, "RivaTuner Statistics Server");

        if (hWnd != IntPtr.Zero)
            PostMessage(hWnd, Msg, wParam, lParam);
    }

    private uint EnableFlag(uint flag, bool status)
    {
        var current = SetFlags(~flag, status ? flag : 0);
        UpdateSettings();
        return current;
    }

    private void UpdateSettings()
    {
        PostMessage(WM_RTSS_UPDATESETTINGS, IntPtr.Zero, IntPtr.Zero);
    }
}

// public static class RunningGames
// {
//     public static List<AppFlags> appFlags = new List<AppFlags>()
//     {
//         {AppFlags.Direct3D9Ex },
//         {AppFlags.Direct3D9 },
//         {AppFlags.Direct3D10 },
//         {AppFlags.Direct3D11 },
//         {AppFlags.OpenGL }
//
//     };
//     //public static unsafe int closeGame()
//     //{
//     //    int processID = 0;
//     //    if (RTSS.RTSSRunning())
//     //    {
//
//     //        AppFlags appFlag = appFlags[0];
//     //        AppEntry[] appEntries = OSD.GetAppEntries(appFlag);
//
//
//     //        while (appEntries.Length == 0)
//     //        {
//     //            foreach (AppFlags af in appFlags)
//     //            {
//     //                appEntries = OSD.GetAppEntries(af);
//     //                if (appEntries.Length > 0) { appFlag = af; break; }
//     //            }
//
//     //        }
//
//     //        foreach (var app in appEntries)
//     //        {
//     //            processID = app.ProcessId;
//
//     //            System.Diagnostics.Process procs = null;
//
//     //            try
//     //            {
//     //                procs = Process.GetProcessById(processID);
//
//
//
//     //                if (!procs.HasExited)
//     //                {
//     //                    procs.CloseMainWindow();
//     //                }
//     //            }
//     //            finally
//     //            {
//     //                if (procs != null)
//     //                {
//     //                    procs.Dispose();
//     //                }
//     //            }
//     //        }
//
//
//
//     //    }
//     //    return processID;
//
//
//     //}
//     //public static Dictionary<string, int> gameRunningDictionary()
//     //{
//     //    //RTSS01
//     //    Dictionary<string, int> returnDictionary = new Dictionary<string, int>();
//     //    try
//     //    {
//
//     //        if (RTSS.RTSSRunning())
//     //        {
//
//
//     //            AppEntry[] appEntries;
//
//
//     //            foreach (AppFlags af in appFlags)
//     //            {
//     //                appEntries = OSD.GetAppEntries(af);
//     //                if (appEntries.Length > 0)
//     //                {
//     //                    foreach (var app in appEntries)
//     //                    {
//     //                        string[] gamedir = app.Name.Split('\\');
//     //                        if (gamedir.Length > 0)
//     //                        {
//     //                            string currGameName = gamedir[gamedir.Length - 1].Substring(0, gamedir[gamedir.Length - 1].Length - 4);
//     //                            returnDictionary.Add(currGameName, app.ProcessId);
//     //                        }
//
//     //                    }
//
//     //                    break;
//     //                }
//     //            }
//     //        }
//     //    }
//     //    catch (Exception ex)
//     //    {
//     //        MessageBox.Show(ex.Message);
//     //        return returnDictionary;
//     //    }
//     //    return returnDictionary;
//     //}
//     //public static unsafe int gameRunningProcessID()
//     //{
//
//     //    int gameRunning = 0;
//     //    try
//     //    {
//
//     //        if (RTSS.RTSSRunning())
//     //        {
//
//     //            AppFlags appFlag = appFlags[0];
//     //            AppEntry[] appEntries = OSD.GetAppEntries(appFlag);
//
//     //            foreach (AppFlags af in appFlags)
//     //            {
//     //                appEntries = OSD.GetAppEntries(af);
//     //                if (appEntries.Length > 0) { appFlag = af; break; }
//     //            }
//
//     //            foreach (var app in appEntries)
//     //            {
//     //                gameRunning = app.ProcessId;
//     //                break;
//
//     //            }
//
//     //        }
//
//
//
//
//     //    }
//     //    catch (Exception ex)
//     //    {
//     //        MessageBox.Show(ex.Message);
//     //        return 0;
//     //    }
//     //    return gameRunning;
//     //}
// }