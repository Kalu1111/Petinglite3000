using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PetingoLightUI.WPF
{
    /// <summary>
    /// Provides bindable properties and commands for the NotifyIcon. In this sample, the
    /// view model is assigned to the NotifyIcon in XAML. Alternatively, the startup routing
    /// in App.xaml.cs could have created this view model, and assigned it to the NotifyIcon.
    /// </summary>
    public class NotifyIconViewModel
    {

        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => Application.Current.MainWindow == null,
                    CommandAction = () =>
                    {
                        Application.Current.MainWindow = new MainWindow();
                        Application.Current.MainWindow.Show();
                    }
                };
            }
        }

        public ICommand PauseCommand
        {
            get
            {

                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        if (InterceptKeys.KeyCombo_TOGGLE_PAUSE.inCooldown) return;

                        InterceptKeys.KeyCombo_TOGGLE_PAUSE.Action();
                        InterceptKeys.KeyCombo_TOGGLE_PAUSE.ReloadCooldown();
                    },
                    CanExecuteFunc = () => !MainManager.Instance.IsPaused && !InterceptKeys.KeyCombo_TOGGLE_PAUSE.inCooldown
                };
            }
        }

        public ICommand RunCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        if (InterceptKeys.KeyCombo_TOGGLE_PAUSE.inCooldown) return;

                        InterceptKeys.KeyCombo_TOGGLE_PAUSE.Action();
                        InterceptKeys.KeyCombo_TOGGLE_PAUSE.ReloadCooldown();
                    },
                    CanExecuteFunc = () => MainManager.Instance.IsPaused && !InterceptKeys.KeyCombo_TOGGLE_PAUSE.inCooldown
                };
            }
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand { CommandAction = () => Application.Current.Shutdown() };
            }
        }
    }


    /// <summary>
    /// Simplistic delegate command for the demo.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        public Action CommandAction { get; set; }
        public Func<bool> CanExecuteFunc { get; set; }

        public void Execute(object parameter)
        {
            CommandAction();
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteFunc == null || CanExecuteFunc();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
