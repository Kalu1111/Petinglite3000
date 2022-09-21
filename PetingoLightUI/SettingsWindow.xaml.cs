using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PetingoLightUI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            InitTimeSettings();
            InitStartupCheckBox();
            InitBrightnessSlider();
            InitMonitorMode();
        }


        void checkboxRunOnSchedule_ValueChanged(object sender, RoutedEventArgs args)
        {
            cbHours_Start.IsEnabled = checkboxRunOnSchedule.IsChecked.Value;
            cbHours_End.IsEnabled = checkboxRunOnSchedule.IsChecked.Value;
            cbMinutes_Start.IsEnabled = checkboxRunOnSchedule.IsChecked.Value;
            cbMinutes_End.IsEnabled = checkboxRunOnSchedule.IsChecked.Value;
        }
        void InitTimeSettings()
        {
            checkboxRunOnSchedule.IsChecked = true;
            checkboxRunOnSchedule.Checked += checkboxRunOnSchedule_ValueChanged;
            checkboxRunOnSchedule.Unchecked += checkboxRunOnSchedule_ValueChanged;

            for (int i = 0; i < 24; i++)
            {
                var v = i < 10 ? "0" + i + "H" : i + "H";
                cbHours_Start.Items.Add(v);
                cbHours_End.Items.Add(v);
            }
            for (int i = 0; i < 60; i++)
            {
                var v = i < 10 ? "0" + i + "M" : i + "M";
                cbMinutes_Start.Items.Add(v);
                cbMinutes_End.Items.Add(v);
            }

            if (Settings.LED_TARGET_TIME_ON >= 0)
            {
                cbHours_Start.SelectedIndex = (byte)(Settings.LED_TARGET_TIME_ON / 100f);
                cbMinutes_Start.SelectedIndex = (byte)(Settings.LED_TARGET_TIME_ON % 100f);
            }
            if (Settings.LED_TARGET_TIME_OFF >= 0)
            {
                cbHours_End.SelectedIndex = (byte)(Settings.LED_TARGET_TIME_OFF / 100f);
                cbMinutes_End.SelectedIndex = (byte)(Settings.LED_TARGET_TIME_OFF % 100f);
            }

            checkboxRunOnSchedule.IsChecked = Settings.LED_TARGET_TIME_ON >= 0 && Settings.LED_TARGET_TIME_OFF >= 0;
        }
        void InitBrightnessSlider()
        {
            sliderBrightness.ValueChanged += (object sender, RoutedPropertyChangedEventArgs<double> e) =>
            {
                lblBrightness.Content = $"Intensity Slider - {(int)(e.NewValue + .5f)}%";
            };
            sliderBrightness.Value = MainManager.Instance.GetBrightness();
        }
        void InitStartupCheckBox()
        {
            checkboxStartupWithWindows.Checked += (object sender, RoutedEventArgs args) =>
            {
                MainManager.Instance.SetStartupWithWindows(checkboxStartupWithWindows.IsChecked.Value);
            };
            checkboxStartupWithWindows.Unchecked += (object sender, RoutedEventArgs args) =>
            {
                MainManager.Instance.SetStartupWithWindows(checkboxStartupWithWindows.IsChecked.Value);
            };
            checkboxStartupWithWindows.IsChecked = Settings.STARTUP_WITH_WINDOWS;
        }
        void InitMonitorMode()
        {
            cbMonitorMode.Items.Clear();
            cbMonitorMode.Items.Add("Single Monitor");
            cbMonitorMode.Items.Add("Dual Monitor");
            cbMonitorMode.Items.Add("Triple Monitor");

            cbMonitorMode.SelectedIndex = Settings.MODE - 1;
        }
        void BrightnessSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            MainManager.Instance.SetBrightness(sliderBrightness.Value);
            sliderBrightness.Value = (Settings.SCREEN_BRIGHTNESS / 255.0f) * 100;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            short on = -1, off = -1;
            if (checkboxRunOnSchedule.IsChecked.Value && (cbHours_Start.SelectedIndex > -1 && cbMinutes_Start.SelectedIndex > -1 && cbHours_End.SelectedIndex > -1 && cbMinutes_End.SelectedIndex > -1))
            {
                on = (short)((cbHours_Start.SelectedIndex * 100f) + cbMinutes_Start.SelectedIndex);
                off = (short)((cbHours_End.SelectedIndex * 100f) + cbMinutes_End.SelectedIndex);
            }
            if (on != off)
            {
                Settings.LED_TARGET_TIME_ON = on;
                Settings.LED_TARGET_TIME_OFF = off;
            }
            else
            {
                Settings.LED_TARGET_TIME_ON = -1;
                Settings.LED_TARGET_TIME_OFF = -1;
            }

            MainManager.Instance.RefreshScheduledEvents();
            MainManager.Instance.SetMonitorMode((byte)(cbMonitorMode.SelectedIndex + 1));

            this.Close();
        }
    }
}
