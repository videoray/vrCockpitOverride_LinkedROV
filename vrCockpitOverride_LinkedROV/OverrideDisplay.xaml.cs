using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace vrCockpitOverride
{
    /// <summary>
    /// Interaction logic for OverrideDisplay.xaml
    /// </summary>
    public partial class OverrideDisplay : UserControl
    {
        VideoRay.UI.TransparentFloatingWindow tw;
        public OverrideDisplay()
        {
            InitializeComponent();
            tw = new VideoRay.UI.TransparentFloatingWindow(true);
            Viewbox vb = new Viewbox();
            tw.Top = 5;
            tw.Left = (VideoRay.UI.ScreenEnumerator.ScreenCoords[VideoRay.UI.ScreenEnumerator.primary].Width - this.Width) / 2;
            vb.Height = tw.Height = this.Height;
            vb.Width = tw.Width = this.Width;
            vb.Child = this;
            tw.Bind(vb, false);
        }

        public void Show()
        {
            tw.Show();
        }

        public void Hide()
        {
            tw.Hide();
        }

        public void SetPort()
        {
            Port.Background = Brushes.GreenYellow;
            Starboard.Background = Brushes.DarkGray;
            Starboard.IsEnabled = false;
        }

        public void SetStarboard()
        {
            Starboard.Background = Brushes.GreenYellow;
            Port.Background = Brushes.DarkGray;
            Port.IsEnabled = false;
        }

        public void SetCancel()
        {
            Port.Background = Brushes.LightBlue;
            Starboard.Background = Brushes.LightBlue;
            Port.IsEnabled = true;
            Starboard.IsEnabled = true;
            master_slave.Fill = Brushes.Gray;
            netlink.Fill = Brushes.Gray;
        }

        public void SetSlave()
        {
            master_slave.Fill = Brushes.Cyan;
            netlink.Fill = Brushes.GreenYellow;
        }

        public void SetMaster()
        {
            master_slave.Fill = Brushes.GreenYellow;
            netlink.Fill = Brushes.GreenYellow;
        }
    }
}
