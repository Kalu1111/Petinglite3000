using PetingoLightUI.Helper;
using PetingoLightUI.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PetingoLightUI.Core
{

    //Experimental and not being used
    public class LightingFXManager : ScreenshotManager
    {
        public LightingFXManager(TargetColor t, byte LEDCountVer, byte LEDCountHor) : base(t, LEDCountVer, LEDCountHor)
        { }

        public override bool Start(string displayAddress)
        {
            if (IsRunning)
                return true;

            if (IsRunning)
            {
                requiresReload = true;
                return true;
            }
            IsRunning = true;


            base.mainLoop = new Thread(new ThreadStart(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                ScreenshotOrchestrator.UpdateMonitorStatus();
                while (IsRunning)
                {
                    try
                    {
                        PixelArrayL = Enumerable.Repeat<byte>(128, LEDVerticalCount * 3).ToArray(); //*3 for RGB
                        PixelArrayR = Enumerable.Repeat<byte>(128, LEDVerticalCount * 3).ToArray();
                        PixelArrayT = Enumerable.Repeat<byte>(128, LEDHorizontalCount * 3).ToArray();

                        requiresReload = false;
                        while (!requiresReload)
                        {
                            for (int i = 0; i < PixelArrayL.Length; i++)
                            {
                                PixelArrayL[i] = 125;
                                PixelArrayR[i] = 125;
                            }
                            for (int i = 0; i < PixelArrayT.Length; i++)
                                PixelArrayT[i] = 125;

                            ScreenshotOrchestrator.WaitTurnToSendFrameInfo(this); //delay inside this func sets the max framerate
                        }
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
    }
}
