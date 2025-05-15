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

namespace SolarCleaningSimulation1
{
    /// <summary>
    /// Interaction logic for StartAppWindow.xaml
    /// </summary>
    public partial class StartAppWindow : Window
    {
        public StartAppWindow()
        {
            InitializeComponent();
        }

        private void StartAppButton_Click(object sender, RoutedEventArgs e)
        {
            // 1) Open the MainWindow
            var main = new MainWindow();
            main.Show();

            // 2) Close this start window
            this.Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // closes all windows and ends the process
            Application.Current.Shutdown();
        }
    }
}
