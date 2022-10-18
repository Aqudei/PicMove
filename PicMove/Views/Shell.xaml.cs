using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace PicMove.Views
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : MetroWindow
    {
        public Shell()
        {
            InitializeComponent();


        }

        private void RotateLeft_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Preview.RenderTransform is not RotateTransform rotate)
                return;

            rotate.Angle -= 90;
        }


        private void RotateRight_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Preview.RenderTransform is not RotateTransform rotate)
                return;
            
            rotate.Angle += 90;
        }
    }
}
