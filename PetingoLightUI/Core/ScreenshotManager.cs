using NuGet.Packaging.Signing;
using PetingoLightUI.Helper;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PetingoLightUI.Lib
{
    public class ScreenshotManager
    {
        protected Thread mainLoop;
        protected Bitmap bitmapFull;
        protected Rectangle boundsRectFull;


        protected static readonly byte[] gammaAdjust = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 10, 10, 11, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17, 18, 18, 19, 19, 20, 21, 21, 22, 22, 23, 24, 24, 25, 25, 26, 27, 27, 28, 29, 29, 30, 31, 32, 32, 33, 34, 34, 35, 36, 37, 37, 38, 39, 40, 40, 41, 42, 43, 44, 44, 45, 46, 47, 48, 49, 49, 50, 51, 52, 53, 54, 55, 56, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 87, 88, 89, 90, 91, 92, 93, 94, 96, 97, 98, 99, 100, 101, 103, 104, 105, 106, 107, 108, 110, 111, 112, 113, 115, 116, 117, 118, 120, 121, 122, 123, 125, 126, 127, 129, 130, 131, 133, 134, 135, 137, 138, 139, 141, 142, 143, 145, 146, 148, 149, 150, 152, 153, 155, 156, 157, 159, 160, 162, 163, 165, 166, 168, 169, 171, 172, 174, 175, 177, 178, 180, 181, 183, 184, 186, 187, 189, 190, 192, 193, 195, 197, 198, 200, 201, 203, 205, 206, 208, 209, 211, 213, 214, 216, 218, 219, 221, 223, 224, 226, 228, 229, 231, 233, 234, 236, 238, 240, 241, 243, 245, 246, 248, 249 };
        //private static readonly byte[] gammaAdjust = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10, 10, 11, 11, 11, 12, 12, 13, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17, 17, 18, 18, 19, 20, 20, 21, 21, 22, 22, 23, 23, 24, 25, 25, 26, 27, 27, 28, 29, 29, 30, 31, 31, 32, 33, 34, 34, 35, 36, 37, 37, 38, 39, 40, 41, 42, 43, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 64, 65, 66, 67, 68, 69, 71, 72, 73, 74, 76, 77, 78, 79, 81, 82, 83, 85, 86, 88, 89, 90, 92, 93, 95, 96, 98, 99, 101, 102, 104, 105, 107, 108, 110, 112, 113, 115, 117, 118, 120, 122, 123, 125, 127, 129, 131, 132, 134, 136, 138, 140, 142, 144, 145, 147, 149, 151, 153, 155, 157, 159, 161, 164, 166, 168, 170, 172, 174, 176, 179, 181, 183, 185, 187, 190, 192, 194, 197, 199, 201, 204, 206, 209, 211, 214, 216, 219, 221, 224, 226, 229, 231, 234, 237, 239, 242, 245, 247, 249 };
        public byte[] PixelArrayL { get; protected set; }
        public byte[] PixelArrayR { get; protected set; }
        public byte[] PixelArrayT { get; protected set; }

        public EventHandler<Bitmap> ScreenRefreshed;
        public EventHandler<Bitmap> ScreenRefreshedTop;
        public EventHandler<Bitmap> ScreenRefreshedLeft;
        public EventHandler<Bitmap> ScreenRefreshedRight;

        public TargetColor Target { get; protected set; }

        public static float BlueCorrection = 0.4f;
        public static float GreenCorrection = 0.6f;
        public static ushort BrightnessGateThreshold = 80;
        public static ushort TopBorderPixelsJump = 80;

        protected int displayAdaptor = int.Parse(Settings.SCREEN_INDEX_OFF.Split('@')[1]);
        protected int displayIdx = int.Parse(Settings.SCREEN_INDEX_OFF.Split('@')[0]);
        protected byte avgDepthLR = 10;              //how many rows/col of pixels do you take into account for the single line color avg
        public byte LEDVerticalCount { get; protected set; } = 1;
        public byte LEDHorizontalCount { get; protected set; } = 1;


        public bool hasStopped { get; protected set; } = false;
        public bool IsRunning { get; protected set; } = false;
        protected bool requiresReload = false;
        public bool outputToBitmap = true;
        public bool shouldDrawScreenPreview = false;

        public ScreenshotManager(TargetColor t, byte LEDVerCount, byte LEDHorCount)
        {
            Target = t;
            SetLEDCount(LEDVerCount, LEDHorCount);
        }
        public virtual bool Start(string displayAddress)
        {
            if (IsRunning && displayAddress.Equals(displayIdx + "@" + displayAdaptor))
                return true;

            if (!DisplayAddressChecks(displayAddress))
            {
                Stop();
                return false;
            }

            if (IsRunning)
            {
                requiresReload = true;
                return true;
            }
            IsRunning = true;


            mainLoop = new Thread(new ThreadStart(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                ScreenshotOrchestrator.UpdateMonitorStatus();
                while (IsRunning)
                {
                    try
                    {
                        Texture2D screenTexture2D;
                        DataBox mapSource;
                        IntPtr sourcePtr;

                        var factory = new Factory1();
                        var adapter = factory.GetAdapter1(displayAdaptor);
                        var device = new SharpDX.Direct3D11.Device(adapter);
                        var output = adapter.GetOutput(displayIdx);
                        var output1 = output.QueryInterface<Output1>();
                        var screen = Screen.AllScreens.Where(x => x.DeviceName == output.Description.DeviceName).First();
                        float scalingFactor = GetDpiScalingFactor(screen);
                        ushort width = (ushort)(screen.Bounds.Width * scalingFactor);
                        ushort height = (ushort)(screen.Bounds.Height * scalingFactor);
                        ushort widthAdjusted = GetNearestDivided(width, LEDHorizontalCount);
                        ushort stepHorExcess = (ushort)(width - widthAdjusted);
                        ushort midSectionJump = (ushort)((width * 4) - (avgDepthLR * 10 * 8)); //*10 because we only read 1px every 10
                        ushort pos = 0;
                        ushort posR = 0;
                        byte DrawScreenPreviewCounter = 0;
                        byte LEDVerCount = LEDVerticalCount;
                        ushort stepHor = (ushort)(widthAdjusted / LEDHorizontalCount);
                        ushort stepVer = (ushort)(GetNearestDivided((ushort)(height - TopBorderPixelsJump), LEDVerticalCount) / LEDVerticalCount);
                        ushort stepSum = 0;
                        uint[] colorSum = { 0, 0, 0 }; //B-G-R
                        bool leftSide = true;

                        SetupPixelArrays(width, height);

                        var textureDesc = new Texture2DDescription
                        {
                            CpuAccessFlags = CpuAccessFlags.Read,
                            BindFlags = BindFlags.None,
                            Format = Format.B8G8R8A8_UNorm,
                            Width = width,
                            Height = height,
                            OptionFlags = ResourceOptionFlags.None,
                            MipLevels = 1,
                            ArraySize = 1,
                            SampleDescription = { Count = 1, Quality = 0 },
                            Usage = ResourceUsage.Staging
                        };


                        using (var screenTexture = new Texture2D(device, textureDesc))
                        using (var duplicatedOutput = output1.DuplicateOutput(device))
                        {
                            SharpDX.DXGI.Resource screenResource;
                            OutputDuplicateFrameInformation duplicateFrameInformation;

                            requiresReload = false;
                            while (!requiresReload)
                            {

                                if (duplicatedOutput.TryAcquireNextFrame(5, out duplicateFrameInformation, out screenResource).Failure)
                                {
                                    ScreenshotOrchestrator.WaitTurnToSendFrameInfo(this, true); //delay inside this func sets the max framerate
                                    continue;
                                }

                                using (screenTexture2D = screenResource.QueryInterface<Texture2D>())
                                    device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                                mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);


                                if (shouldDrawScreenPreview)
                                {
                                    sourcePtr = mapSource.DataPointer;
                                    DrawScreenPreviewCounter++;
                                    if (DrawScreenPreviewCounter == 10)
                                    {
                                        var mapDestFull = bitmapFull.LockBits(boundsRectFull, ImageLockMode.WriteOnly, bitmapFull.PixelFormat);
                                        var destPtrFull = mapDestFull.Scan0;
                                        for (int y = 0; y < height; y++)
                                        {
                                            Utilities.CopyMemory(destPtrFull, sourcePtr, 4 * width);
                                            destPtrFull = IntPtr.Add(destPtrFull, mapDestFull.Stride);
                                            sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                                        }
                                        bitmapFull.UnlockBits(mapDestFull);
                                        ScreenRefreshed?.Invoke(this, bitmapFull);
                                        DrawScreenPreviewCounter = 0;
                                    }
                                }

                                pos = 0;
                                stepSum = 0;
                                colorSum[0] = 0; //BLUE
                                colorSum[1] = 0; //GREEN
                                colorSum[2] = 0; //RED

                                //calc top avgs
                                sourcePtr = IntPtr.Add(mapSource.DataPointer, mapSource.RowPitch * TopBorderPixelsJump); //start reading 80 rows lower
                                for (short x = 0; x < widthAdjusted; x++)
                                {
                                    colorSum[0] += Marshal.ReadByte(sourcePtr, 0);
                                    colorSum[1] += Marshal.ReadByte(sourcePtr, 1);
                                    colorSum[2] += Marshal.ReadByte(sourcePtr, 2);
                                    sourcePtr = IntPtr.Add(sourcePtr, 4);
                                    stepSum++;

                                    if (stepSum == stepHor)
                                    {
                                        PixelArrayT[pos] = (byte)(gammaAdjust[(byte)(colorSum[0] / stepHor)] * BlueCorrection); pos++;
                                        PixelArrayT[pos] = (byte)(gammaAdjust[(byte)(colorSum[1] / stepHor)] * GreenCorrection); pos++;
                                        PixelArrayT[pos] = gammaAdjust[(byte)(colorSum[2] / stepHor)]; pos++;

                                        colorSum[0] = 0; colorSum[1] = 0; colorSum[2] = 0; stepSum = 0;
                                    }
                                }

                                pos = 0;
                                posR = 0;
                                leftSide = true;

                                //calc left and right avgs
                                sourcePtr = IntPtr.Add(mapSource.DataPointer, mapSource.RowPitch * TopBorderPixelsJump); //start reading 80 rows lower
                                for (byte y = 0; y < LEDVerCount; y++)
                                {
                                    for (byte x = 0; x < avgDepthLR * 2; x++)
                                    {
                                        colorSum[0] += Marshal.ReadByte(sourcePtr, 0);
                                        colorSum[1] += Marshal.ReadByte(sourcePtr, 1);
                                        colorSum[2] += Marshal.ReadByte(sourcePtr, 2);
                                        sourcePtr = IntPtr.Add(sourcePtr, 40); //4*10 because we only read 1px every 10
                                        stepSum++;

                                        if (stepSum == avgDepthLR)
                                        {
                                            if (leftSide)
                                            {
                                                PixelArrayL[pos] = (byte)(gammaAdjust[(byte)((colorSum[0] / stepSum))] * BlueCorrection); pos++;
                                                PixelArrayL[pos] = (byte)(gammaAdjust[(byte)((colorSum[1] / stepSum))] * GreenCorrection); pos++;
                                                PixelArrayL[pos] = gammaAdjust[(byte)((colorSum[2] / stepSum))]; pos++;
                                                sourcePtr = IntPtr.Add(sourcePtr, midSectionJump);
                                            }
                                            else
                                            {
                                                PixelArrayR[posR] = (byte)(gammaAdjust[(byte)((colorSum[0] / stepSum))] * BlueCorrection); posR++;
                                                PixelArrayR[posR] = (byte)(gammaAdjust[(byte)((colorSum[1] / stepSum))] * GreenCorrection); posR++;
                                                PixelArrayR[posR] = gammaAdjust[(byte)((colorSum[2] / stepSum))]; posR++;
                                            }
                                            colorSum[0] = 0; colorSum[1] = 0; colorSum[2] = 0; stepSum = 0;
                                            leftSide = !leftSide;
                                        }
                                    }
                                    sourcePtr = IntPtr.Add(sourcePtr, stepVer * mapSource.RowPitch);
                                }

                                device.ImmediateContext.UnmapSubresource(screenTexture, 0);
                                screenResource.Dispose();
                                duplicatedOutput.ReleaseFrame();

                                ScreenshotOrchestrator.WaitTurnToSendFrameInfo(this); //delay inside this func sets the max framerate
                            }
                        }
                    }
                    catch (SharpDX.SharpDXException ex)
                    {
                        ScreenshotOrchestrator.WaitTurnToSendFrameInfo(this, true); //delay inside this func sets the max framerate
                        Debug.WriteLine(ex);
                        Logger.Error(ex.ToString());
                        Thread.Sleep(500);

                        if (ex.ToString().Contains("E_ACCESSDENIED/General access denied error")) //most likely the windows session is locked, this behaviour is expected
                            MainManager.Instance.Pause();
                    }
                    catch (Exception ex1)
                    {
                        ScreenshotOrchestrator.WaitTurnToSendFrameInfo(this, true); //delay inside this func sets the max framerate
                        Debug.WriteLine(ex1);
                        Logger.Error(ex1.ToString());
                        Thread.Sleep(1000);
                    }
                }
                hasStopped = true;
            }));

            mainLoop.Start();

            return true;
        }
        public void Stop()
        {
            hasStopped = false;
            requiresReload = true;
            IsRunning = false;
            ScreenshotOrchestrator.ForceUnlock(Target);
            //while (mainLoop!=null && mainLoop.IsAlive && !hasStopped) 
            //  Thread.Sleep(100);
        }
        public void SetLEDCount(byte LEDVerCount, byte LEDHorCount)
        {
            if (LEDVerCount == 0) LEDVerCount = 1;
            if (LEDHorCount == 0) LEDHorCount = 1;

            LEDVerticalCount = LEDVerCount;
            LEDHorizontalCount = LEDHorCount;

            requiresReload = true;
        }
        public static string[] ListAvailableDisplayAdresses()
        {
            var res = new List<string>();
            var factory = new Factory1();
            for (int i = 0; i < factory.Adapters.Length; i++)
                for (int j = 0; j < factory.Adapters[i].Outputs.Length; j++)
                    res.Add(j + "@" + i);

            return res.ToArray();
        }

        private bool DisplayAddressChecks(string displayAddress)
        {
            if (displayAddress == null || !displayAddress.Contains("@") || displayAddress == Settings.SCREEN_INDEX_OFF)
                return false;

            var parsed = true;
            var tempFactory = new Factory1();
            parsed = parsed ? int.TryParse(displayAddress.Split('@')[0], out displayIdx) : parsed;
            parsed = parsed ? int.TryParse(displayAddress.Split('@')[1], out displayAdaptor) : parsed;

            if (!parsed || !(displayAdaptor < tempFactory.Adapters1.Length && displayIdx < tempFactory.Adapters1[displayAdaptor].Outputs.Length))
                return false;

            return true;
        }
        private void SetupPixelArrays(ushort width, ushort height)
        {
            if (bitmapFull != null)
                bitmapFull.Dispose();

            boundsRectFull = new Rectangle(0, 0, width, height);
            bitmapFull = new Bitmap(width, height, PixelFormat.Format32bppArgb); //todo   System.ArgumentException: 'Parameter is not valid.'

            PixelArrayL = Enumerable.Repeat<byte>(128, LEDVerticalCount * 3).ToArray(); //*3 for RGB
            PixelArrayR = Enumerable.Repeat<byte>(128, LEDVerticalCount * 3).ToArray();
            PixelArrayT = Enumerable.Repeat<byte>(128, LEDHorizontalCount * 3).ToArray();
        }
        private float GetDpiScalingFactor(Screen screen)
        {
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            EnumDisplaySettings(screen.DeviceName, -1, ref dm);
            return (float)Math.Round(Decimal.Divide(dm.dmPelsWidth, screen.Bounds.Width), 2);
        }
        private Bitmap CopyDataToBitmap(byte[] data, bool isTop = false)
        {
            //Here create the Bitmap to the know height, width and format
            Bitmap bmp = new Bitmap(isTop ? LEDHorizontalCount : 1, isTop ? 1 : LEDVerticalCount, PixelFormat.Format24bppRgb);

            //Create a BitmapData and Lock all pixels to be written 
            BitmapData bmpData = bmp.LockBits(
                                 new Rectangle(0, 0, bmp.Width, bmp.Height),
                                 ImageLockMode.WriteOnly, bmp.PixelFormat);

            byte[] data2 = null;
            if (!isTop)
            {
                data2 = new byte[LEDVerticalCount * 4];
                for (int i = 0, j = 0; i < LEDVerticalCount * 4; i++)
                    if (i % 4 != 0)
                    {
                        data2[i - 1] = data[j];
                        j++;
                    }
            }
            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(isTop ? data : data2, 0, bmpData.Scan0, isTop ? data.Length : data2.Length);


            //Unlock the pixels
            bmp.UnlockBits(bmpData);


            //Return the bitmap 
            return bmp;
        }
        private static ushort GetNearestDivided(ushort dividend, ushort divisor)
        {
            var output = (ushort)(dividend / divisor);
            if (output == 0 && dividend > 0) output += 1;
            return (ushort)(output * divisor);
        }

        #region DPI Adjustment
        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }
        #endregion
    }
}
