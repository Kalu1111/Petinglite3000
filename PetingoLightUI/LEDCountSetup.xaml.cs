using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PetingoLightUI
{
    /// <summary>
    /// Interaction logic for LEDCountSetup.xaml
    /// </summary>
    public partial class LEDCountSetup : Window
    {
        private TargetColor targetColor;

        public LEDCountSetup(TargetColor targetColor)
        {
            this.targetColor = targetColor;

            InitializeComponent();
            Init();
        }

        private void Init()
        {
            switch (targetColor)
            {
                case TargetColor.RED:
                    lblLedCountHor.Text = "" + Settings.LED_COUNT_HOR_RED * 2;
                    lblLedCountVer.Text = "" + Settings.LED_COUNT_VER_RED * 2;
                    break;
                case TargetColor.YELLOW:
                    lblLedCountHor.Text = "" + Settings.LED_COUNT_HOR_YELLOW * 2;
                    lblLedCountVer.Text = "" + Settings.LED_COUNT_VER_YELLOW * 2;
                    break;
                case TargetColor.BLUE:
                    lblLedCountHor.Text = "" + Settings.LED_COUNT_HOR_BLUE * 2;
                    lblLedCountVer.Text = "" + Settings.LED_COUNT_VER_BLUE * 2;
                    break;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            switch (targetColor)
            {
                case TargetColor.RED:
                    Settings.LED_COUNT_HOR_RED = (byte)(byte.Parse(lblLedCountHor.Text) / 2);
                    Settings.LED_COUNT_VER_RED = (byte)(byte.Parse(lblLedCountVer.Text) / 2);
                    break;
                case TargetColor.YELLOW:
                    Settings.LED_COUNT_HOR_YELLOW = (byte)(byte.Parse(lblLedCountHor.Text) / 2);
                    Settings.LED_COUNT_VER_YELLOW = (byte)(byte.Parse(lblLedCountVer.Text) / 2);
                    break;
                case TargetColor.BLUE:
                    Settings.LED_COUNT_HOR_BLUE = (byte)(byte.Parse(lblLedCountHor.Text) / 2);
                    Settings.LED_COUNT_VER_BLUE = (byte)(byte.Parse(lblLedCountVer.Text) / 2);
                    break;
            }

            MainManager.Instance.ResetLEDCount(targetColor);

            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !byte.TryParse(((TextBox)sender).Text + e.Text, out var res) && res % 2 != 0;
        }
    }
}
