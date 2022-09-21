using PetingoLightUI.Helper;
using PetingoLightUI.Lib;
using System.Diagnostics;
using System.Threading;

namespace PetingoLightUI
{
    public static class ScreenshotOrchestrator
    {
        private static DrawMode drawMode = DrawMode.SINGLE;

        private static Semaphore[] monitorMutex = {
            new Semaphore (0,1, TargetColor.RED.ToString()),
            new Semaphore (0,1, TargetColor.YELLOW.ToString()),
            new Semaphore (0,1, TargetColor.BLUE.ToString())
        };
        private static bool[] monitorIsWaiting = {
            true,
            true,
            true
        };
        private static bool[] monitorIsRunning = {
            false,
            false,
            false
        };

        private static short SkipTurnLimiter = 0;


        public static void ForceUnlock(TargetColor t)
        {
            ResumeThread(t);
        }
        public static void UpdateMonitorStatus()
        {
            byte LEDCount = 0;

            monitorIsRunning = new bool[]{
                MainManager.Instance.ScreenshotManagers[(int)TargetColor.RED].IsRunning,
                MainManager.Instance.ScreenshotManagers[(int)TargetColor.YELLOW].IsRunning,
                MainManager.Instance.ScreenshotManagers[(int)TargetColor.BLUE].IsRunning
            };

            //2 monitor setup - if left and right are on
            if (monitorIsRunning[0] && !monitorIsRunning[1] && monitorIsRunning[2])
                drawMode = DrawMode.DOUBLE;
            else

            //1 monitor setup - if only center is on
            if (monitorIsRunning[1] && (!monitorIsRunning[0] || !monitorIsRunning[2]))
                drawMode = DrawMode.SINGLE;
            else

            //3 monitor setup - if left, center and right are on
            if (monitorIsRunning[0] && monitorIsRunning[1] && monitorIsRunning[2])
                drawMode = DrawMode.TRIPLE;



            switch (drawMode)
            {
                case DrawMode.SINGLE:

                    LEDCount = (byte)(Settings.LED_COUNT_VER_YELLOW + Settings.LED_COUNT_VER_YELLOW + Settings.LED_COUNT_HOR_YELLOW);
                    if (monitorIsWaiting[(int)TargetColor.YELLOW])
                        ResumeThread(TargetColor.YELLOW);

                    break;
                case DrawMode.DOUBLE:

                    LEDCount = (byte)(Settings.LED_COUNT_VER_RED + Settings.LED_COUNT_VER_BLUE + Settings.LED_COUNT_HOR_RED + Settings.LED_COUNT_HOR_BLUE);
                    if (monitorIsWaiting[(int)TargetColor.RED] && monitorIsWaiting[(int)TargetColor.BLUE])
                        ResumeThread(TargetColor.RED);

                    break;
                case DrawMode.TRIPLE:

                    LEDCount = (byte)(Settings.LED_COUNT_VER_RED + Settings.LED_COUNT_VER_BLUE + Settings.LED_COUNT_HOR_RED + Settings.LED_COUNT_HOR_YELLOW + Settings.LED_COUNT_HOR_BLUE);
                    if (monitorIsWaiting[(int)TargetColor.RED] && monitorIsWaiting[(int)TargetColor.YELLOW] && monitorIsWaiting[(int)TargetColor.BLUE])
                        ResumeThread(TargetColor.RED);

                    break;
            }

            LEDCount *= 2; //LED strip will draw 2 leds for every pixel sent

            if (BluetoothManager.CheckConnection())
            {
                BluetoothManager.SendSetLedCountCommand(LEDCount);
                BluetoothManager.ResetPacketComparisonHistory();
                BluetoothManager.SendSetBrightnessCommand(Settings.SCREEN_BRIGHTNESS);
                BluetoothManager.SendSetBrightnessCommand(Settings.SCREEN_BRIGHTNESS);
                MainManager.Instance.FlashLogoOnScreens();
            }
        }
        public static void WaitTurnToSendFrameInfo(ScreenshotManager screen, bool skipTurn = false)
        {
            SkipTurnLimiter++;
            if (SkipTurnLimiter > 60)
            {
                SkipTurnLimiter = 0;
                BluetoothManager.ResetPacketComparisonHistory();
            }

            switch (drawMode)
            {
                case DrawMode.SINGLE:
                    WaitTurnToSendFrameInfo_singleMode(screen, skipTurn);
                    break;
                case DrawMode.DOUBLE:
                    if (!PauseThread(screen.Target)) return;
                    WaitTurnToSendFrameInfo_doubleMode(screen, skipTurn);
                    break;
                case DrawMode.TRIPLE:
                    if (!PauseThread(screen.Target)) return;
                    WaitTurnToSendFrameInfo_tripleMode(screen, skipTurn);
                    break;
            }
        }
        public static bool IsDeviceConnected()
        {
            return BluetoothManager.CheckConnection();
        }

        private static void WaitTurnToSendFrameInfo_singleMode(ScreenshotManager screen, bool skipTurn = false)
        {
            if (screen.Target != TargetColor.YELLOW) return;

            if (IsDeviceConnected())
            {
                try
                {
                    BluetoothManager.DrawRight(screen);
                    BluetoothManager.DrawTop(screen);
                    BluetoothManager.DrawLeft(screen);
                }
                catch (System.Exception e)
                {
                    Debug.WriteLine("CAlhou mal " + e);
                    Logger.Error(e.ToString());
                }
            }
            else
                Thread.Sleep(33);
        }
        private static void WaitTurnToSendFrameInfo_doubleMode(ScreenshotManager screen, bool skipTurn = false)
        {
            if (IsDeviceConnected())
            {
                try
                {
                    if (screen.Target == TargetColor.RED)
                    {
                        BluetoothManager.DrawRight(screen, skipTurn);
                        BluetoothManager.DrawTop(screen, skipTurn);
                    }
                    else
                    if (screen.Target == TargetColor.BLUE)
                    {
                        BluetoothManager.DrawTop(screen, skipTurn);
                        BluetoothManager.DrawLeft(screen, skipTurn);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.WriteLine("Calhou mal " + e);
                    Logger.Error(e.ToString());
                }
            }
            else
                Thread.Sleep(33);

            ResumeThread(screen.Target == TargetColor.RED ? TargetColor.BLUE : TargetColor.RED);
        }
        private static void WaitTurnToSendFrameInfo_tripleMode(ScreenshotManager screen, bool skipTurn = false)
        {
            if (IsDeviceConnected())
            {
                try
                {
                    if (screen.Target == TargetColor.RED)
                    {
                        BluetoothManager.DrawRight(screen);
                        BluetoothManager.DrawTop(screen);
                    }
                    else
                    if (screen.Target == TargetColor.YELLOW)
                        BluetoothManager.DrawTop(screen);
                    else
                    if (screen.Target == TargetColor.BLUE)
                    {

                        BluetoothManager.DrawTop(screen);
                        BluetoothManager.DrawLeft(screen);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.WriteLine("CAlhou mal " + e);
                    Logger.Error(e.ToString());
                }
            }
            else
                Thread.Sleep(33);

            if (screen.Target == TargetColor.RED) ResumeThread(TargetColor.YELLOW);
            else
            if (screen.Target == TargetColor.YELLOW) ResumeThread(TargetColor.BLUE);
            else
            if (screen.Target == TargetColor.BLUE) ResumeThread(TargetColor.RED);
        }
        private static bool PauseThread(TargetColor t)
        {
            monitorIsWaiting[(int)t] = true;
            monitorMutex[(int)t].WaitOne();

            return monitorIsRunning[(int)t];
        }
        private static void ResumeThread(TargetColor t)
        {
            try
            {
                monitorIsWaiting[(int)t] = false;
                monitorMutex[(int)t].Release();
            }
            catch (System.Threading.SemaphoreFullException) { }
        }
    }



    public enum DrawMode
    {
        SINGLE,
        DOUBLE,
        TRIPLE
    }
}
