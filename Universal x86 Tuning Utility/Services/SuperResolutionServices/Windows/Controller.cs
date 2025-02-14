﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
using Gma.System.MouseKeyHook;
using Universal_x86_Tuning_Utility.Properties;
using Universal_x86_Tuning_Utility.Views.Windows;

namespace Universal_x86_Tuning_Utility.Services.SuperResolutionServices.Windows
{
    //
    // This is a customised version of Magpie from https://github.com/Blinue/Magpie
    // I do not take credit for the full functionality of the code.
    //

    internal class Controller
    {
        public static MagWindow? magWindow = null;

        private static readonly ScaleModelManager scaleModelManager = new();

        static string AppName = "";
        static string currentAppName = "";
        static bool Reapply = false;
        static bool canReapply = false;

        private static int DOWN_COUNT = 5;
        private static int countDownNum;

        private static IntPtr Handle;
        private static IntPtr prevSrcWindow = IntPtr.Zero;

        private static void ToggleOverlay()
        {
            NativeMethods.BroadcastMessage(NativeMethods.MAGPIE_WM_TOGGLE_OVERLAY);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool SystemParametersInfo(int uAction, int uParam, ref RECT lpvParam, int fuWinIni);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        const uint SWP_SHOWWINDOW = 0x0040;
        const int SPI_GETWORKAREA = 48;

        const int SM_CYBORDER = 6;

        const int GW_OWNER = 4;
        const int GWL_STYLE = -16;
        const int WS_CHILD = 0x40000000;
        const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        public static void ToggleMagWindow()
        {
            if (Settings.Default.isMagpie || magWindow.Running)
            {
                if (!scaleModelManager.IsValid() || magWindow == null)
                {
                    return;
                }

                if (magWindow.Running)
                {
                    magWindow.Destory();
                    canReapply = false;
                    using (Process p = Process.GetCurrentProcess()) p.PriorityClass = ProcessPriorityClass.Normal;
                    return;
                }
                else
                {
                    using (Process p = Process.GetCurrentProcess()) p.PriorityClass = ProcessPriorityClass.AboveNormal;
                    canReapply = true;
                }

                string sharpness = Convert.ToString(Settings.Default.Sharpness);

                double scaleFactor = 0.59;

                IntPtr foregroundWindow = GetForegroundWindow();

                if (Settings.Default.ResMode == 1) scaleFactor = 0.77;
                if (Settings.Default.ResMode == 2) scaleFactor = 0.67;
                if (Settings.Default.ResMode == 3) scaleFactor = 0.59;
                if (Settings.Default.ResMode == 4) scaleFactor = 0.50;
                if (Settings.Default.ResMode == 5) scaleFactor = 0.33;

                if (foregroundWindow != IntPtr.Zero && Settings.Default.ResMode > 0)
                {
                    if (GetWindowRect(foregroundWindow, out RECT windowRect))
                    {
                        Screen primaryScreen = Screen.PrimaryScreen;
                        int screenWidth = primaryScreen.Bounds.Width;
                        int screenHeight = primaryScreen.Bounds.Height;

                        int newWidth = (int)Math.Floor(screenWidth * scaleFactor);
                        int newHeight = (int)Math.Floor(screenHeight * scaleFactor);

                        int windowStyle = GetWindowLong(foregroundWindow, GWL_STYLE);

                        StringBuilder windowText = new StringBuilder(256);
                        GetWindowText(foregroundWindow, windowText, windowText.Capacity);

                        string windowTitle = windowText.ToString();

                        AppName = windowTitle;

                        // Check if the window has the WS_BORDER style bit set
                        bool isBorderless = (windowStyle & 0x00800000) == 0;

                        if (!isBorderless)
                        {

                            windowStyle = GetWindowLong(foregroundWindow, GWL_STYLE);
                            int extendedWindowStyle = GetWindowLong(foregroundWindow, GWL_STYLE);

                            // Check if it's a Win32 application by looking for certain window styles
                            bool isWin32Application = (windowStyle & WS_CHILD) == 0 && (extendedWindowStyle & WS_EX_TOOLWINDOW) == 0;

                            if (isWin32Application) newHeight = newHeight + 32;
                            else newHeight = newHeight + 40;
                        }

                        int newX = (screenWidth - newWidth) / 2;
                        int newY = (screenHeight - newHeight) / 2;

                        SetWindowPos(foregroundWindow, IntPtr.Zero, newX, newY, newWidth, newHeight, SWP_SHOWWINDOW);
                    }
                }

                string effectsJson = scaleModelManager.GetScaleModels()![Settings.Default.ScaleMode].Effects;

                int index = effectsJson.LastIndexOf(":");
                if (index >= 0)
                    effectsJson = effectsJson.Substring(0, index);



                effectsJson = effectsJson + ":" + sharpness + "}]";

                //MessageBox.Show(effectsJson);

                magWindow.Create(effectsJson);

                prevSrcWindow = magWindow.SrcWindow;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public static async void AutoRestore_Tick(object sender, EventArgs e)
        {
            if (Settings.Default.AutoRestore && Settings.Default.isMagpie && canReapply)
            {
                IntPtr foregroundWindow = GetForegroundWindow();

                if (foregroundWindow != IntPtr.Zero)
                {
                    StringBuilder windowText = new StringBuilder(256);
                    GetWindowText(foregroundWindow, windowText, windowText.Capacity);
                    currentAppName = windowText.ToString();

                    if (currentAppName == AppName && !magWindow.Running) ToggleMagWindow();
                }
            }
        }
        MainWindow mainWin = null;
        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.MAGPIE_WM_SHOWME)
            {
                _ = NativeMethods.SetForegroundWindow(Handle);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public static async void SetUpMagWindow(MainWindow main)
        {
            Handle = new WindowInteropHelper(main).Handle;

            magWindow = new MagWindow(main);
            magWindow.Closed += MagWindow_Closed;

            OnHotkeyChanged();

            await Task.Run(() => Garbage.Garbage_Collect());
        }

        private static void MagWindow_Closed()
        {
            if (!Settings.Default.AutoRestore || prevSrcWindow == Handle)
            {
                return;
            }
        }

        private static IKeyboardMouseEvents? keyboardEvents = null;

        public static bool OnHotkeyChanged()
        {
            keyboardEvents?.Dispose();
            keyboardEvents = Hook.GlobalEvents();

            try
            {
                keyboardEvents.OnCombination(new Dictionary<Combination, Action> {
                    {Combination.FromString(Settings.Default.MagpieHotkey), () => ToggleMagWindow()},
                });
            }
            catch (ArgumentException e)
            {
                return false;
            }

            return true;
        }
    }
}
