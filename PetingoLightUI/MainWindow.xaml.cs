using PetingoLightUI.Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PetingoLightUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string COLOR_BLUE = "#FF0097FF";
        private const string COLOR_YELLOW = "#FFF5D400";
        private const string COLOR_RED = "#FFCD0000";
        private const string COLOR_GRAY = "#FF7E7070";

        public MainWindow()
        {
            InitializeComponent();

            StartDisplay(TargetColor.RED);
            StartDisplay(TargetColor.YELLOW);
            StartDisplay(TargetColor.BLUE);
            InitScreenSelectionDropdownLists();

            ChangeMode(Settings.MODE);

            if (!BluetoothManager.CheckConnection())
                ShowConnecting();

            if (MainManager.Instance.IsPaused)
                ShowPaused();
            else
                HidePaused();
        }

        private Action BuildBitmapToImageSourceAction(TargetColor t, Bitmap data)
        {
            Action action = null;
            if (t == TargetColor.RED)
                action = new Action(() => { if (ScreenSelectionDropdownRed.SelectedIndex > 0) this.ImgPreviewRed.Source = BitmapToImageSource(data); });
            else
            if (t == TargetColor.YELLOW)
                action = new Action(() => { if (ScreenSelectionDropdownYellow.SelectedIndex > 0) this.ImgPreviewYellow.Source = BitmapToImageSource(data); });
            else
            if (t == TargetColor.BLUE)
                action = new Action(() => { if (ScreenSelectionDropdownBlue.SelectedIndex > 0) this.ImgPreviewBlue.Source = BitmapToImageSource(data); });

            return action;
        }
        private void StartDisplay(TargetColor t)
        {
            //clear event handler
            if (MainManager.Instance.ScreenshotManagers[(int)t].ScreenRefreshed != null)
                foreach (Delegate d in MainManager.Instance.ScreenshotManagers[(int)t].ScreenRefreshed.GetInvocationList())
                    MainManager.Instance.ScreenshotManagers[(int)t].ScreenRefreshed -= (EventHandler<Bitmap>)d;

            //add new event
            MainManager.Instance.ScreenshotManagers[(int)t].ScreenRefreshed += (sender, data) =>
            {
                this.Dispatcher.Invoke(DispatcherPriority.Background,
                BuildBitmapToImageSourceAction(t, data));
            };

            MainManager.Instance.ScreenshotManagers[(int)t].shouldDrawScreenPreview = true;
            MainManager.Instance.ScreenshotManagers[(int)t].outputToBitmap = MainManager.Instance.DEBUG_MODE;
        }
        private void InitScreenSelectionDropdownLists()
        {
            ScreenSelectionDropdownRed.Items.Clear();
            ScreenSelectionDropdownYellow.Items.Clear();
            ScreenSelectionDropdownBlue.Items.Clear();

            ScreenSelectionDropdownRed.Items.Add("OFF");
            ScreenSelectionDropdownYellow.Items.Add("OFF");
            ScreenSelectionDropdownBlue.Items.Add("OFF");

            var displayAddresses = ScreenshotManager.ListAvailableDisplayAdresses();

            foreach (var addr in displayAddresses)
            {
                ScreenSelectionDropdownRed.Items.Add("Monitor " + addr);
                ScreenSelectionDropdownYellow.Items.Add("Monitor " + addr);
                ScreenSelectionDropdownBlue.Items.Add("Monitor " + addr);
            }

            ScreenSelectionDropdownRed.SelectedIndex = -1;
            ScreenSelectionDropdownYellow.SelectedIndex = -1;
            ScreenSelectionDropdownBlue.SelectedIndex = -1;

            if (Settings.SCREEN_INDEX_RED == Settings.SCREEN_INDEX_OFF)
                ScreenSelectionDropdownRed.SelectedIndex = 0;
            else
                ScreenSelectionDropdownRed.SelectedItem = "Monitor " + Settings.SCREEN_INDEX_RED;

            if (Settings.SCREEN_INDEX_YELLOW == Settings.SCREEN_INDEX_OFF)
                ScreenSelectionDropdownYellow.SelectedIndex = 0;
            else
                ScreenSelectionDropdownYellow.SelectedItem = "Monitor " + Settings.SCREEN_INDEX_YELLOW;

            if (Settings.SCREEN_INDEX_BLUE == Settings.SCREEN_INDEX_OFF)
                ScreenSelectionDropdownBlue.SelectedIndex = 0;
            else
                ScreenSelectionDropdownBlue.SelectedItem = "Monitor " + Settings.SCREEN_INDEX_BLUE;

            ScreenSelectionDropdownRed.SelectionChanged += ScreenSelectionDropdownRedSelectionChanged;
            ScreenSelectionDropdownYellow.SelectionChanged += ScreenSelectionDropdownYellowSelectionChanged;
            ScreenSelectionDropdownBlue.SelectionChanged += ScreenSelectionDropdownBlueSelectionChanged;
        }
        private void ScreenSelectionDropdownRedSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!MainManager.Instance.StartScreenshotManager(TargetColor.RED, ((string)ScreenSelectionDropdownRed.SelectedItem).Replace("Monitor ", "")))
            {
                ScreenSelectionDropdownRed.SelectedIndex = 0;
                this.ImgPreviewRed.Source = null;
            }
        }
        private void ScreenSelectionDropdownBlueSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!MainManager.Instance.StartScreenshotManager(TargetColor.BLUE, ((string)ScreenSelectionDropdownBlue.SelectedItem).Replace("Monitor ", "")))
            {
                ScreenSelectionDropdownBlue.SelectedIndex = 0;
                this.ImgPreviewBlue.Source = null;
            }
        }
        private void ScreenSelectionDropdownYellowSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!MainManager.Instance.StartScreenshotManager(TargetColor.YELLOW, ((string)ScreenSelectionDropdownYellow.SelectedItem).Replace("Monitor ", "")))
            {
                ScreenSelectionDropdownYellow.SelectedIndex = 0;
                this.ImgPreviewYellow.Source = null;
            }
        }
        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;
                    BitmapImage bitmapimage = new BitmapImage();
                    bitmapimage.BeginInit();
                    bitmapimage.StreamSource = memory;
                    bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapimage.EndInit();

                    return bitmapimage;
                }
            }
            catch (System.ArgumentException) { }
            return null;
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            foreach (var t in (TargetColor[])Enum.GetValues(typeof(TargetColor)))
            {
                MainManager.Instance.ScreenshotManagers[(int)t].shouldDrawScreenPreview = false;
                MainManager.Instance.ScreenshotManagers[(int)t].outputToBitmap = false;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        private void btnSettingsBlue_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new LEDCountSetup(TargetColor.BLUE);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }
        private void btnSettingsYellow_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new LEDCountSetup(TargetColor.YELLOW);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }
        private void btnSettingsRed_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new LEDCountSetup(TargetColor.RED);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }
        private void btnMainSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        public void ShowPaused()
        {
            lblPaused.Visibility = Visibility.Visible;
        }
        public void HidePaused()
        {
            lblPaused.Visibility = Visibility.Hidden;
        }
        public void ShowConnecting()
        {
            lblConnecting.Visibility = Visibility.Visible;
        }
        public void HideConnecting()
        {
            lblConnecting.Visibility = Visibility.Hidden;
        }
        public void ShowBTPairRequest()
        {
            lblBTPairRequest.Visibility = Visibility.Visible;
        }
        public void HideBTPairRequest()
        {
            lblBTPairRequest.Visibility = Visibility.Hidden;
        }
        public void ChangeMode(byte nrOfMonitors)
        {
            var converter = new System.Windows.Media.BrushConverter();
            switch (nrOfMonitors)
            {
                case 1:
                    btnSettingsBlue.Visibility = Visibility.Hidden;
                    rectRight.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    lblRightLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    ScreenSelectionDropdownBlue.SelectedIndex = 0;
                    ScreenSelectionDropdownBlue.IsEnabled = false;
                    ImgPreviewBlue.Source = null;

                    btnSettingsRed.Visibility = Visibility.Hidden;
                    rectLeft.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    lblLeftLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    ScreenSelectionDropdownRed.SelectedIndex = 0;
                    ScreenSelectionDropdownRed.IsEnabled = false;
                    ImgPreviewRed.Source = null;

                    btnSettingsYellow.Visibility = Visibility.Visible;
                    rectCenter.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_YELLOW);
                    lblCenterLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_YELLOW);
                    ScreenSelectionDropdownYellow.IsEnabled = true;
                    break;

                case 2:
                    btnSettingsYellow.Visibility = Visibility.Hidden;
                    rectCenter.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    lblCenterLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    ScreenSelectionDropdownYellow.SelectedIndex = 0;
                    ScreenSelectionDropdownYellow.IsEnabled = false;
                    ImgPreviewYellow.Source = null;

                    btnSettingsBlue.Visibility = Visibility.Visible;
                    btnSettingsRed.Visibility = Visibility.Visible;
                    rectRight.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_RED);
                    rectLeft.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_BLUE);
                    lblRightLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_RED);
                    lblLeftLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_BLUE);
                    ScreenSelectionDropdownRed.IsEnabled = true;
                    ScreenSelectionDropdownBlue.IsEnabled = true;
                    break;

                case 3:
                    btnSettingsBlue.Visibility = Visibility.Visible;
                    btnSettingsYellow.Visibility = Visibility.Visible;
                    btnSettingsRed.Visibility = Visibility.Visible;

                    rectRight.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_RED);
                    rectCenter.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_YELLOW);
                    rectLeft.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_BLUE);

                    lblRightLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_RED);
                    lblCenterLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_YELLOW);
                    lblLeftLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_BLUE);

                    ScreenSelectionDropdownRed.IsEnabled = true;
                    ScreenSelectionDropdownYellow.IsEnabled = true;
                    ScreenSelectionDropdownBlue.IsEnabled = true;
                    break;

                default:
                    btnSettingsBlue.Visibility = Visibility.Hidden;
                    btnSettingsYellow.Visibility = Visibility.Hidden;
                    btnSettingsRed.Visibility = Visibility.Hidden;

                    rectRight.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    rectCenter.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    rectLeft.Fill = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);

                    lblRightLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    lblCenterLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);
                    lblLeftLable.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(COLOR_GRAY);

                    ScreenSelectionDropdownBlue.SelectedIndex = 0;
                    ScreenSelectionDropdownBlue.IsEnabled = false;
                    ImgPreviewBlue.Source = null;

                    ScreenSelectionDropdownRed.SelectedIndex = 0;
                    ScreenSelectionDropdownRed.IsEnabled = false;
                    ImgPreviewRed.Source = null;

                    ScreenSelectionDropdownYellow.SelectedIndex = 0;
                    ScreenSelectionDropdownYellow.IsEnabled = false;
                    ImgPreviewYellow.Source = null;
                    break;
            }
        }
    }
}
