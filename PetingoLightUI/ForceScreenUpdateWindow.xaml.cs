using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace PetingoLightUI
{
    public partial class ForceScreenUpdateWindow : Window
    {
        double scaleRatio = Math.Max(Screen.PrimaryScreen.WorkingArea.Width / SystemParameters.PrimaryScreenWidth, Screen.PrimaryScreen.WorkingArea.Height / SystemParameters.PrimaryScreenHeight);

        public ForceScreenUpdateWindow(Screen s, int timeoutMs = 2000)
        {
            InitializeComponent();
            AdjustToScreen(s);
            Task.Run(() =>
            {
                Thread.Sleep(timeoutMs);
                this.Dispatcher.Invoke(() => this.Close());
            });
        }


        private void AdjustToScreen(Screen s)
        {
            Rectangle r2 = s.WorkingArea;
            this.Top = r2.Top / scaleRatio;
            this.Left = r2.Left / scaleRatio;
        }

    }
}
