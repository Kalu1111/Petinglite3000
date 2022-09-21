using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PetingoLightUI
{
    public class ScheduledEvents
    {
        private Timer ResumeOnSchedule;
        private Timer PauseOnSchedule;

        public void Init()
        {
            SetupResumeOnSchedule();
            SetupPauseOnSchedule();
        }

        private void SetupResumeOnSchedule()
        {
            if (ResumeOnSchedule != null)
                ResumeOnSchedule.Dispose();

            if (Settings.LED_TARGET_TIME_ON < 0 || Settings.LED_TARGET_TIME_OFF < 0 || Settings.LED_TARGET_TIME_ON == Settings.LED_TARGET_TIME_OFF) return;

            DateTime now = DateTime.Now;
            DateTime targetOn = DateTime.Today.AddHours((int)(Settings.LED_TARGET_TIME_ON / 100f));
            targetOn = targetOn.AddMinutes((int)(Settings.LED_TARGET_TIME_ON % 100f));

            if (now > targetOn) targetOn = targetOn.AddDays(1.0);

            int msUntil = (int)((targetOn - now).TotalMilliseconds);

            ResumeOnSchedule = new Timer(TriggerResumeOnSchedule, null, msUntil, Timeout.Infinite);
        }
        private void SetupPauseOnSchedule()
        {
            if (PauseOnSchedule != null)
                PauseOnSchedule.Dispose();

            if (Settings.LED_TARGET_TIME_ON < 0 || Settings.LED_TARGET_TIME_OFF < 0 || Settings.LED_TARGET_TIME_ON == Settings.LED_TARGET_TIME_OFF) return;

            DateTime now = DateTime.Now;
            DateTime targetOn = DateTime.Today.AddHours((int)(Settings.LED_TARGET_TIME_OFF / 100f));
            targetOn = targetOn.AddMinutes((int)(Settings.LED_TARGET_TIME_OFF % 100f));

            if (now > targetOn) targetOn = targetOn.AddDays(1.0);

            int msUntil = (int)((targetOn - now).TotalMilliseconds);

            PauseOnSchedule = new Timer(TriggerPauseOnSchedule, null, msUntil, Timeout.Infinite);
        }

        private void TriggerResumeOnSchedule(object state)
        {
            MainManager.Instance.Resume();
            Thread.Sleep(60000);
            SetupResumeOnSchedule();
        }
        private void TriggerPauseOnSchedule(object state)
        {
            MainManager.Instance.Pause();
            Thread.Sleep(60000);
            SetupPauseOnSchedule();
        }


        public static bool ShouldBePaused()
        {
            var start = Settings.LED_TARGET_TIME_ON;
            var end = Settings.LED_TARGET_TIME_OFF;
            if (start >= 0 && end >= 0 && start != end)
            {
                var now = DateTime.Now.Hour * 100 + DateTime.Now.Minute;

                if (start <= end)
                {
                    if (now >= start && now <= end)
                        return false;
                }
                else
                {
                    if (now >= start || now <= end)
                        return false;
                }

                return true;
            }

            return false;
        }
    }
}
