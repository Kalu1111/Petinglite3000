using Microsoft.Win32;
using PetingoLightUI.Helper;
using PetingoLightUI.Lib;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace PetingoLightUI
{
    public class MainManager : IDisposable
    {
        private static object syncLock = new object();

        private static MainManager instance;
        public static MainManager Instance
        {
            get
            {
                if (instance == null)
                    lock (syncLock)
                        if (instance == null)
                            instance = new MainManager();
                return instance;
            }
        }

        public bool DEBUG_MODE { get; private set; } = false;
        public bool IsPaused { get; private set; } = false;
        public byte Brightness { get; private set; } = 128;

        public ScreenshotManager[] ScreenshotManagers; //0-red 1-yellow 2-blue
        public ScheduledEvents schedule;

        private MainManager()
        {
            BluetoothManager.Init(() =>
            {
                ScreenshotOrchestrator.UpdateMonitorStatus();
            });

            ScreenshotManagers = new ScreenshotManager[]
            {
                new ScreenshotManager(TargetColor.RED,Settings.LED_COUNT_VER_RED, Settings.LED_COUNT_HOR_RED),
                new ScreenshotManager(TargetColor.YELLOW,Settings.LED_COUNT_VER_YELLOW, Settings.LED_COUNT_HOR_YELLOW),
                new ScreenshotManager(TargetColor.BLUE,Settings.LED_COUNT_VER_BLUE, Settings.LED_COUNT_HOR_BLUE),
            };

            ScreenshotManagers[(int)TargetColor.RED].outputToBitmap = DEBUG_MODE;
            ScreenshotManagers[(int)TargetColor.YELLOW].outputToBitmap = DEBUG_MODE;
            ScreenshotManagers[(int)TargetColor.BLUE].outputToBitmap = DEBUG_MODE;

            schedule = new ScheduledEvents();
        }

        public void Start()
        {
            IsPaused = true;

            RefreshScheduledEvents();
            if (!ScheduledEvents.ShouldBePaused())
                Resume();
            else
                Pause();
        }
        public bool StartScreenshotManager(TargetColor t, string screenAddress)
        {
            if (t == TargetColor.RED)
                Settings.SCREEN_INDEX_RED = screenAddress;
            else
            if (t == TargetColor.YELLOW)
                Settings.SCREEN_INDEX_YELLOW = screenAddress;
            else
            if (t == TargetColor.BLUE)
                Settings.SCREEN_INDEX_BLUE = screenAddress;

            return StartScreenshotManager(t);
        }
        public bool StartScreenshotManager(TargetColor t)
        {
            Resume();

            var success = false;
            if (t == TargetColor.RED)
            {
                success = ScreenshotManagers[(int)t].Start(Settings.SCREEN_INDEX_RED);
                if (!success)
                    Settings.SCREEN_INDEX_RED = Settings.SCREEN_INDEX_OFF;
            }
            else
            if (t == TargetColor.YELLOW)
            {
                success = ScreenshotManagers[(int)t].Start(Settings.SCREEN_INDEX_YELLOW);
                if (!success)
                    Settings.SCREEN_INDEX_YELLOW = Settings.SCREEN_INDEX_OFF;
            }
            else
            if (t == TargetColor.BLUE)
            {
                success = ScreenshotManagers[(int)t].Start(Settings.SCREEN_INDEX_BLUE);
                if (!success)
                    Settings.SCREEN_INDEX_BLUE = Settings.SCREEN_INDEX_OFF;
            }
            return success;
        }

        public void TogglePauseResume()
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
        public void SetStartupWithWindows(bool start)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (start)
                registryKey.SetValue(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, System.Reflection.Assembly.GetExecutingAssembly().Location);
            else
                registryKey.DeleteValue(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

            Settings.STARTUP_WITH_WINDOWS = start;
        }
        public void SetBrightness(double percentage)
        {
            if (percentage < 0 || percentage > 100) return;

            Brightness = (byte)(255.0f * (percentage / 100.0f));
            Settings.SCREEN_BRIGHTNESS = Brightness;
            BluetoothManager.SendSetBrightnessCommand(Settings.SCREEN_BRIGHTNESS);
        }
        public byte GetBrightness()
        {
            return (byte)((Settings.SCREEN_BRIGHTNESS / 255f) * 100);
        }
        public void ResetLEDCount(TargetColor t)
        {
            if (t == TargetColor.RED)
                ScreenshotManagers[(int)t].SetLEDCount(Settings.LED_COUNT_VER_RED, Settings.LED_COUNT_HOR_RED);
            else
           if (t == TargetColor.YELLOW)
                ScreenshotManagers[(int)t].SetLEDCount(Settings.LED_COUNT_VER_YELLOW, Settings.LED_COUNT_HOR_YELLOW);
            else
           if (t == TargetColor.BLUE)
                ScreenshotManagers[(int)t].SetLEDCount(Settings.LED_COUNT_VER_BLUE, Settings.LED_COUNT_HOR_BLUE);

            ScreenshotOrchestrator.UpdateMonitorStatus();
        }
        public void Dispose()
        {
            Stop(TargetColor.RED);
            Stop(TargetColor.YELLOW);
            Stop(TargetColor.BLUE);
            BluetoothManager.Dispose();

            instance = null;
        }
        public void Shutdown()
        {
            Logger.Info("MainManager Shutdown()");
            System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
        }
        public void RefreshScheduledEvents()
        {
            schedule.Init();
        }
        public void SetMonitorMode(byte nrOfMonitors)
        {
            Settings.MODE = nrOfMonitors;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (System.Windows.Application.Current.MainWindow != null)
                    try { ((MainWindow)(System.Windows.Application.Current.MainWindow)).ChangeMode(Settings.MODE); } catch (System.InvalidCastException) { }
            });
        }
        public void FlashLogoOnScreens()
        {
            foreach (var s in Screen.AllScreens)
                System.Windows.Application.Current.Dispatcher.Invoke(() => new ForceScreenUpdateWindow(s).Show());
        }
        private void Stop(TargetColor t)
        {
            ScreenshotManagers[(int)t].Stop();
        }

        #region thread safe
        public void Pause()
        {
            lock (syncLock)
            {
                if (!IsPaused)
                {
                    Logger.Info("MainManager Pause()");

                    IsPaused = true;
                    Stop(TargetColor.RED);
                    Stop(TargetColor.YELLOW);
                    Stop(TargetColor.BLUE);
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Application.Current.MainWindow != null)
                        try { ((MainWindow)(System.Windows.Application.Current.MainWindow)).ShowPaused(); } catch (System.InvalidCastException) { }
                });
            }
        }
        public void Resume()
        {
            lock (syncLock)
            {
                if (IsPaused)
                {
                    Logger.Info("MainManager Resume()");

                    IsPaused = false;
                    StartScreenshotManager(TargetColor.RED);
                    StartScreenshotManager(TargetColor.YELLOW);
                    StartScreenshotManager(TargetColor.BLUE);
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Application.Current.MainWindow != null)
                        try { ((MainWindow)(System.Windows.Application.Current.MainWindow)).HidePaused(); } catch (System.InvalidCastException) { }
                });
            }
        }
        #endregion
    }

    public enum TargetColor
    {
        RED,
        YELLOW,
        BLUE
    }
}
