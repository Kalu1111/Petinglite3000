using HashTableHashing;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using NuGet.Packaging.Signing;
using PetingoLightUI.Helper;
using PetingoLightUI.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Xamarin.Forms.PlatformConfiguration;

namespace PetingoLightUI
{
    static class BluetoothManager
    {
        private const string BT_DEVICE_LOWER_NAME = "petinglite 3000";
        private const string BT_DEVICE_AUTH_CODE = "0000";
        private const byte HIGHEST_AVAILABLE_VALUE = 249;
        private const byte PACKET_FILLER = 250; //values to ignore if you need to flush a packet with less bytes than PACKET_MAX_SIZE
        private const byte CMD_SELECT_RGB = 251;
        private const byte CMD_SELECT_BRIGHTNESS = 252;
        private const byte CMD_SELECT_DRAW = 253;
        private const byte CMD_SELECT_LEDCOUNT = 254;
        private const byte CMD_END_OF_MSG = 255;
        private const byte PACKET_MAX_SIZE = 9;
        private const byte PACKET_HEAD_PLUS_TAIL_SIZE = 4;

        private static Stopwatch timer = new Stopwatch();
        private static SerialPort btStream;
        private static MurmurHash2Unsafe murmurHash = new MurmurHash2Unsafe();

        private static object sendLock = new object();
        private static uint[] LastPacketHash = new uint[50];
        private static short bytesSentPerDraw = 0;
        private static byte[] msgPacket = new byte[PACKET_MAX_SIZE + PACKET_HEAD_PLUS_TAIL_SIZE];
        private static byte packetCounter = 0;
        private static byte packetByteCounter = 0;
        private static byte PER_DRAW_DELAY = 35;
        private static byte MAX_FPS = 30;
        private static bool isAttemptingToConnect = false;
        private static bool initiated = false;

        public static void Init(Action onSuccessfulConnection = null)
        {
            if (initiated)
                throw new Exception("BluetoothManager has already been initiated.");
            else
                initiated = true;
            Task.Run(() =>
            {
                while (initiated)
                {
                    if (btStream == null || !btStream.IsOpen)
                    {
                        if (!isAttemptingToConnect)
                        {
                            isAttemptingToConnect = true;
                            while (true)
                                if (!BluetoothManager.InitConnection(onSuccessfulConnection))
                                    Thread.Sleep(1000);
                                else
                                    break;
                            ;
                            isAttemptingToConnect = false;
                        }
                    }
                    else
                        Thread.Sleep(1000);
                }
                Close();
            });
        }
        public static bool CheckConnection()
        {
            return (btStream != null && btStream.IsOpen);
        }
        public static void SendSetLedCountCommand(byte ledCount)
        {
            if (!CheckConnection() || ledCount > 60 || ledCount < 1) return;

            lock (sendLock)
                if (btStream == null || ledCount > 60 || ledCount < 1) return;
                else
                    try
                    {
                        Thread.Sleep(500);
                        if (btStream != null)
                        {
                            btStream.Write(new byte[] { CMD_SELECT_LEDCOUNT, ledCount, ledCount, CMD_END_OF_MSG }, 0, 4);
                            bytesSentPerDraw += 4;
                        }

                        Logger.Info($"Bluetooth sending: SendSetLedCountCommand({ledCount})");
                    }
                    catch (System.Exception ex)
                    {
                        Close();
                        Debug.WriteLine(ex);
                        Logger.Error(ex.ToString());
                    }
        }
        public static void SendSetBrightnessCommand(byte value)
        {
            if (!CheckConnection()) return;

            value = Math.Min(HIGHEST_AVAILABLE_VALUE, value);

            lock (sendLock)
                try
                {
                    Thread.Sleep(500);
                    if (btStream != null)
                    {
                        btStream.Write(new byte[] { CMD_SELECT_BRIGHTNESS, value, value, CMD_END_OF_MSG }, 0, 4);
                        bytesSentPerDraw += 4;
                    }

                    Logger.Info($"Bluetooth sending: SendSetBrightnessCommand({value})");
                }
                catch (System.Exception ex)
                {
                    Close();
                    Debug.WriteLine(ex);
                    Logger.Error(ex.ToString());
                }
        }
        public static void DrawRight(ScreenshotManager screen, bool skipTurn = false)
        {
            if (!CheckConnection()) return;

            lock (sendLock)
            {
                packetByteCounter = 0;
                packetCounter = 0;
                try
                {
                    for (short i = (short)(screen.PixelArrayR.Length - 3); i >= 0; i -= 3)
                        for (int j = 0; j < 3; j++)
                            SendRGBByte(screen.PixelArrayR[i + j], skipTurn);
                }
                catch (System.TimeoutException ex0)
                {
                    Close();
                    Debug.WriteLine(ex0);
                    Logger.Error(ex0.ToString());
                }
                catch (System.IO.IOException ex1)
                {
                    Close();
                    Debug.WriteLine(ex1);
                    Logger.Error(ex1.ToString());
                }
            }
        }
        public static void DrawTop(ScreenshotManager screen, bool skipTurn = false)
        {
            if (!CheckConnection()) return;

            lock (sendLock)
            {
                try
                {
                    for (short i = (short)(screen.PixelArrayT.Length - 3); i >= 0; i -= 3)
                        for (int j = 0; j < 3; j++)
                            SendRGBByte(screen.PixelArrayT[i + j], skipTurn);
                }
                catch (System.TimeoutException ex0)
                {
                    Close();
                    Debug.WriteLine(ex0);
                    Logger.Error(ex0.ToString());
                }
                catch (System.IO.IOException ex1)
                {
                    Close();
                    Debug.WriteLine(ex1);
                    Logger.Error(ex1.ToString());
                }
            }
        }
        public static void DrawLeft(ScreenshotManager screen, bool skipTurn = false)
        {
            if (!CheckConnection())
                return;

            lock (sendLock)
            {
                try
                {
                    for (short i = 0; i < screen.PixelArrayL.Length; i++)
                        SendRGBByte(screen.PixelArrayL[i], skipTurn);

                    FlushRGB();
                    SendDrawCommand();
                }
                catch (System.TimeoutException ex0)
                {
                    Close();
                    Debug.WriteLine(ex0);
                    Logger.Error(ex0.ToString());
                }
                catch (System.IO.IOException ex1)
                {
                    Close();
                    Debug.WriteLine(ex1);
                    Logger.Error(ex1.ToString());
                }
            }
        }
        public static void ResetPacketComparisonHistory()
        {
            for (int i = 0; i < LastPacketHash.Length; i++)
                LastPacketHash[i] = 0;
        }
        public static void Dispose()
        {
            initiated = false;
            Close();
        }
        private static void Close()
        {
            lock (sendLock)
            {
                try
                {
                    if (btStream != null)
                        btStream.Close();
                }
                catch (IOException) { }
                catch (Exception) { }
                btStream = null;
                isAttemptingToConnect = false;
            }
        }
        private static string GetBluetoothPort(string deviceAddress)
        {
            const string Win32_SerialPort = "Win32_SerialPort";
            SelectQuery q = new SelectQuery(Win32_SerialPort);
            ManagementObjectSearcher s = new ManagementObjectSearcher(q);
            foreach (object cur in s.Get())
            {
                ManagementObject mo = (ManagementObject)cur;
                string pnpId = mo.GetPropertyValue("PNPDeviceID").ToString();

                if (pnpId.Contains(deviceAddress))
                {
                    object captionObject = mo.GetPropertyValue("Caption");
                    string caption = captionObject.ToString();
                    int index = caption.LastIndexOf("(COM");
                    if (index > 0)
                    {
                        string portString = caption.Substring(index);
                        string comPort = portString.
                                      Replace("(", string.Empty).Replace(")", string.Empty);
                        return comPort;
                    }
                }
            }
            return null;
        }
        private static bool InitConnection(Action onSuccessfulConnection = null)
        {
            if (CheckConnection())
                return true;

            Logger.Info($"Bluetooth attempting to find a connection...");
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow != null)
                    try { ((MainWindow)(Application.Current.MainWindow)).ShowConnecting(); } catch (System.InvalidCastException) { }
            });

            BluetoothDeviceInfo device = null;
            BluetoothClient client = new BluetoothClient();
            try
            {
                foreach (var dev in client.DiscoverDevices())
                    if (dev.DeviceName.ToLower().Contains(BT_DEVICE_LOWER_NAME))
                    {
                        device = dev;
                        break;
                    }

                if (device == null || !device.Authenticated)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current.MainWindow != null)
                            try { ((MainWindow)(Application.Current.MainWindow)).ShowBTPairRequest(); } catch (System.InvalidCastException) { }
                    });
                    return false;
                }
                else
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current.MainWindow != null)
                            try { ((MainWindow)(Application.Current.MainWindow)).HideBTPairRequest(); } catch (System.InvalidCastException) { }
                    });


                var comPort = GetBluetoothPort(device.DeviceAddress.ToString());
                if (string.IsNullOrWhiteSpace(comPort))
                    return false;

                btStream = new SerialPort(comPort, 115200);//38400);
                btStream.WriteTimeout = 1000;
                btStream.Handshake = Handshake.None;
                btStream.Parity = Parity.None;
                btStream.StopBits = StopBits.One;

                lock (sendLock)
                {
                    if (initiated)
                        btStream.Open();
                    if (onSuccessfulConnection != null)
                        onSuccessfulConnection();
                }

                Logger.Info($"Bluetooth connected ({comPort})");

            }
            catch (System.UnauthorizedAccessException ex00)
            {
                Debug.WriteLine(ex00);
                Logger.Error(ex00.ToString());
                LogErrorRestartComPort();
                MainManager.Instance.Shutdown();
                return false;
            }
            catch (System.IO.IOException ex01)
            {
                Debug.WriteLine(ex01);
                if (!ex01.ToString().Contains("The semaphore timeout period has expired"))
                    Logger.Error(ex01.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Logger.Error(ex.ToString());
                return false;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow != null)
                    try { ((MainWindow)(Application.Current.MainWindow)).HideConnecting(); } catch (System.InvalidCastException) { }
            });

            return true;
        }
        private static void LogErrorRestartComPort()
        {
            string messageBoxText = "Your bluetooth connection wasn't properly closed and now I'm not able to reconnect to it again.\n\nBummer.\n\n\n Here's how you can fix this:\n - Go to 'Bluetooth and other devices settings' and unpair your Petinglite 3000 device.\n - Unplug your Petinglite 3000 from the wall.\n - Restart your computer.\n - Power your Petinglite 3000 back up.\n - Re-pair your Petinglite device\n - Restart this application.\n\n\nSorry :)";
            Logger.Error(messageBoxText);
        }
        private static void SendDrawCommand()
        {
            if (!CheckConnection()) return;

            byte[] packet = { CMD_SELECT_DRAW, CMD_END_OF_MSG };
            bytesSentPerDraw += 2;

            if (btStream != null)
                btStream.Write(packet, 0, 2);
            AddVariableDelayToLockFPS();
        }
        private static void AddVariableDelayToLockFPS()
        {

            //short minWait = (short)Math.Max((1000f / MAX_FPS), (1000f / (3800f / bytesSentPerDraw)));
            short minWait = (short)Math.Max((1000f / MAX_FPS), (1000f / (10000f / bytesSentPerDraw)));
            bytesSentPerDraw = 0;

            PER_DRAW_DELAY = (byte)Math.Max(0, Math.Min(minWait, minWait - (int)timer.Elapsed.TotalMilliseconds));

            Thread.Sleep(PER_DRAW_DELAY);
            //Debug.WriteLine(minWait + " - " + PER_DRAW_DELAY + "+" + (int)timer.Elapsed.TotalMilliseconds);

            timer.Reset();
            timer.Start();
        }
        private static void SendRGBByte(byte piece, bool skipTurn = false)
        {
            if (packetByteCounter == PACKET_MAX_SIZE)
                FlushRGB(skipTurn);

            if (!skipTurn)
                msgPacket[3 + packetByteCounter] = piece <= HIGHEST_AVAILABLE_VALUE ? piece : HIGHEST_AVAILABLE_VALUE;
            else
                msgPacket[3 + packetByteCounter] = PACKET_FILLER;
            packetByteCounter++;
        }
        private static void FlushRGB(bool skipTurn = false)
        {
            if (packetByteCounter == 0) return;
            if (!CheckConnection()) return;

            msgPacket[2] = packetCounter;
            msgPacket[1] = GetChecksum(msgPacket);
            msgPacket[0] = CMD_SELECT_RGB;
            msgPacket[msgPacket.Length - 1] = CMD_END_OF_MSG;

            if (!skipTurn || msgPacket[3] < PACKET_FILLER || msgPacket[6] < PACKET_FILLER || msgPacket[9] < PACKET_FILLER)
                if (!IsSameAsLastPacketWithSameID())
                {
                    if (btStream != null)
                    {
                        btStream.Write(msgPacket, 0, PACKET_MAX_SIZE + PACKET_HEAD_PLUS_TAIL_SIZE);
                        bytesSentPerDraw += PACKET_MAX_SIZE + PACKET_HEAD_PLUS_TAIL_SIZE;
                    }
                }

            //clean up; ready for the next packet
            for (int i = 0; i < msgPacket.Length; i++)
                msgPacket[i] = 0;

            packetCounter++;
            packetByteCounter = 0;
        }
        private static byte GetChecksum(byte[] bytes)
        {
            byte ret = 0;
            foreach (var b in bytes)
                ret += b;

            if (ret > 250)
                ret += 10;

            return ret;
        }
        private static bool IsSameAsLastPacketWithSameID()
        {
            var hash = murmurHash.Hash(msgPacket);

            if (LastPacketHash[msgPacket[2]] == hash)
                return true;
            else
                LastPacketHash[msgPacket[2]] = hash;

            return false;
        }
    }
}
