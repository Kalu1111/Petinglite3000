using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using NuGet.Packaging.Signing;
using PetingoLightUI.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        private static object sendLock = new object();

        private static Stopwatch timer = new Stopwatch();

        private static BluetoothClient client;
        private static NetworkStream btStream;

        private static uint[] LastPacketHash = new uint[50];
        private static short bytesSentPerDraw = 0;
        private static byte[] msgPacket = new byte[PACKET_MAX_SIZE + PACKET_HEAD_PLUS_TAIL_SIZE];
        private static byte packetCounter = 0;
        private static byte packetByteCounter = 0;
        private static byte skipEveryOtherPixel = 0;
        private static byte PER_DRAW_DELAY = 35;
        private static byte MAX_FPS = 30;


        public static void CheckConnection()
        {
            if (client == null || !client.Connected)
                lock (sendLock)
                    while (!InitConnection()) { Thread.Sleep(5000); }
        }
        public static void SendSetLedCountCommand(byte ledCount)
        {
            if (btStream == null || ledCount > 60 || ledCount < 1) return;

            byte[] packet = { CMD_SELECT_LEDCOUNT, ledCount, ledCount, CMD_END_OF_MSG };
            bytesSentPerDraw += 4;

            lock (sendLock)
                try
                {
                    btStream.Write(packet, 0, 4);
                    btStream.Flush();
                    Thread.Sleep(500);

                }
                catch (IOException) { return; }
        }
        public static void SendSetBrightnessCommand(byte value)
        {
            if (btStream == null) return;

            value = Math.Min(HIGHEST_AVAILABLE_VALUE, value);

            byte[] packet = { CMD_SELECT_BRIGHTNESS, value, value, CMD_END_OF_MSG };
            bytesSentPerDraw += 4;

            lock (sendLock)
                try
                {
                    btStream.Write(packet, 0, 4);
                    btStream.Flush();
                    Thread.Sleep(1000);

                }
                catch (IOException) { return; }
        }
        public static void Close()
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }
        }
        public static void DrawLeft(ScreenshotManager screen, bool skipTurn = false)
        {
            if (btStream == null) return;

            lock (sendLock)
            {
                packetCounter = 0;

                for (short i = (short)(screen.PixelArrayL.Length - 3); i >= 0; i -= 3)
                    for (int j = 0; j < 3; j++)
                        SendEveryOtherByte(screen.PixelArrayL[i + j], skipTurn);
            }
        }
        public static void DrawTop(ScreenshotManager screen, bool skipTurn = false)
        {
            if (btStream == null) return;

            lock (sendLock)
            {
                for (short i = 0; i < screen.PixelArrayT.Length; i++)
                    SendEveryOtherByte(screen.PixelArrayT[i], skipTurn);
            }
        }
        public static void DrawRight(ScreenshotManager screen, bool skipTurn = false)
        {
            if (btStream == null)
                return;

            lock (sendLock)
            {
                for (short i = 0; i < screen.PixelArrayR.Length; i++)
                    SendEveryOtherByte(screen.PixelArrayR[i], skipTurn);

                Flush();
                SendDrawCommand();
            }
        }

        private static bool InitConnection()
        {
            if (client != null && client.Connected)
                return true;

            client = new BluetoothClient();

            BluetoothDeviceInfo device = null;
            while (device == null)
            {
                foreach (var dev in client.DiscoverDevices())
                {
                    if (dev.DeviceName.ToLower().Contains(BT_DEVICE_LOWER_NAME))
                    {
                        device = dev;
                        break;
                    }
                }
                if (device == null)
                    Thread.Sleep(3000);
            }

            if (!device.Authenticated)
            {
                BluetoothSecurity.PairRequest(device.DeviceAddress, BT_DEVICE_AUTH_CODE);
            }

            device.Refresh();

            try
            {
                client.Connect(device.DeviceAddress, BluetoothService.SerialPort);
                btStream = client.GetStream();
            }
            catch (Exception ex) { return false; }


            ScreenshotOrchestrator.UpdateMonitorStatus();

            return true;
        }
        private static void SendDrawCommand()
        {
            if (btStream == null) return;

            byte[] packet = { CMD_SELECT_DRAW, CMD_END_OF_MSG };
            bytesSentPerDraw += 2;

            try
            {
                btStream.Write(packet, 0, 2);
                btStream.Flush();
                AddVariableDelayToLockFPS();
            }
            catch (IOException) { }
        }
        private static void AddVariableDelayToLockFPS()
        {
            short minWait = (short)Math.Max((1000f / MAX_FPS), (1000f / (1500f / bytesSentPerDraw)));
            bytesSentPerDraw = 0;

            PER_DRAW_DELAY = (byte)Math.Max(0, Math.Min(minWait, minWait - (int)timer.Elapsed.TotalMilliseconds));

            Thread.Sleep(PER_DRAW_DELAY);
            Debug.WriteLine(PER_DRAW_DELAY + " - " + minWait);

            timer.Reset();
            timer.Start();
        }
        private static void SendEveryOtherByte(byte piece, bool skipTurn = false)
        {
            if (skipEveryOtherPixel > 5)
                skipEveryOtherPixel = 0;
            else if (skipEveryOtherPixel > 2)
                SendByte(piece, skipTurn);

            skipEveryOtherPixel++;
        }
        private static void SendByte(byte piece, bool skipTurn = false)
        {
            if (packetByteCounter == PACKET_MAX_SIZE)
                Flush(skipTurn);

            if (!skipTurn)
                msgPacket[3 + packetByteCounter] = piece;
            else
                msgPacket[3 + packetByteCounter] = PACKET_FILLER;
            packetByteCounter++;
        }
        private static void Flush(bool skipTurn = false)
        {
            if (packetByteCounter == 0) return;

            CheckConnection();

            msgPacket[2] = packetCounter;
            msgPacket[1] = GetChecksum(msgPacket);
            msgPacket[0] = CMD_SELECT_RGB;
            msgPacket[msgPacket.Length - 1] = CMD_END_OF_MSG;

            if (!skipTurn || msgPacket[3] < PACKET_FILLER || msgPacket[6] < PACKET_FILLER || msgPacket[9] < PACKET_FILLER)
                if (!IsSameAsLastPacketWithSameID())
                    try
                    {
                        btStream.Write(msgPacket, 0, PACKET_MAX_SIZE + PACKET_HEAD_PLUS_TAIL_SIZE);
                        btStream.Flush();
                        bytesSentPerDraw += PACKET_MAX_SIZE + PACKET_HEAD_PLUS_TAIL_SIZE;
                    }
                    catch (IOException) { }

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
            var crc = Crc32.CalculateCrc(msgPacket);
            if (LastPacketHash[msgPacket[2]] == crc)
                return true;
            else
                LastPacketHash[msgPacket[2]] = crc;

            return false;
        }

        public static void ResetPacketComparisonHistory()
        {
            for (int i = 0; i < LastPacketHash.Length; i++)
                LastPacketHash[i] = 0;
        }
    }
}
