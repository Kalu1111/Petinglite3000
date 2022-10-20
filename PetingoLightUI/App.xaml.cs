using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using PetingoLightUI.Helper;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace PetingoLightUI
{

    public partial class App : Application
    {
        private TaskbarIcon notifyIcon;


        protected override void OnStartup(StartupEventArgs e)
        {
            //only one instance is allow to run, and only one instance is allowed to wait for the older one to shut itself down
            if (Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 2)
                Current.Shutdown();

            while (Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
                Thread.Sleep(1000);

            Logger.Info("Application OnStartup");

            base.OnStartup(e);
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            SystemEvents.PowerModeChanged += OnPowerChange;
            SystemEvents.SessionSwitch += OnSessionSwitch;

            Settings.Init();
            MainManager.Instance.Start();
            InterceptKeys.Hook();

        }


        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Application OnExit");
            InterceptKeys.UnHook();
            MainManager.Instance.Dispose();
            notifyIcon.Dispose();
            SystemEvents.PowerModeChanged -= OnPowerChange;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            base.OnExit(e);
        }

        private void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Logger.Info("Application OnPowerChange (Resume)");
                    Process.Start(ResourceAssembly.Location);
                    Current.Shutdown();
                    break;
                case PowerModes.Suspend:
                    Logger.Info("Application OnPowerChange (Suspend)");
                    MainManager.Instance.Dispose();
                    break;
            }
        }

        public void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionUnlock:
                    Logger.Info("Application OnSessionSwitch (SessionUnlock)");
                    Process.Start(ResourceAssembly.Location);
                    Current.Shutdown();
                    break;
                case SessionSwitchReason.SessionLock:
                    Logger.Info("Application OnSessionSwitch (SessionLock)");
                    MainManager.Instance.Dispose();
                    break;
            }
        }
    }

}
