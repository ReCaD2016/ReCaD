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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Configurator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var seconds = new List<Second>
            {
                new Second(0),
                new Second(60),
                new Second(120),
                new Second(180),
                new Second(240),
                new Second(300),
                new Second(360),
                new Second(420),
                new Second(480),
                new Second(540),
                new Second(600),
            };

            var vms = seconds.Select(s => new SecondVM(s));
            lv.ItemsSource = vms;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var width = canvas.ActualWidth - 10;
            seeker.X1 = seeker.X2 = (slider.Value * width) / slider.Maximum;
        }
    }
}
