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
using System.Threading;
using System.Net;

namespace OMineWatcher
{
    public class Tumbler : CheckBox { }
    public partial class MainWindow : Window
    {
        public static SynchronizationContext MainContext = SynchronizationContext.Current;
        public static MainWindow ST;

        public MainWindow()
        {
            InitializeComponent();
            ST = this;
            StartingApplication();
        }

        private void StartingApplication()
        {
            GPUsCB.ItemsSource = new string[] { "Auto", "1", "2", "3", "4", "5",
                "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
            RigTypeGen.ItemsSource = new string[] { "OMineGuard", "HiveOS" };

        }
        private void GPUsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GPUswitchSP.Children.Clear();

            string Selected = (string)GPUsCB.SelectedItem;
            if (Selected == "Auto")
            {
                GPUsSwitchHeader.Visibility = Visibility.Hidden;
                GPUswitchB.Visibility = Visibility.Hidden;
            }
            else
            {
                GPUsSwitchHeader.Visibility = Visibility.Visible;
                GPUswitchB.Visibility = Visibility.Visible;
                byte k = Convert.ToByte(Selected);
                
                for (byte n = 0; n < k; n++)
                {
                    Grid GR = new Grid { Width = 60 };
                    GR.Children.Add(new TextBlock { Text = "GPU" + n, Effect = null });
                    GR.Children.Add(new CheckBox { Name = "g" + n.ToString(),
                        Margin = new Thickness(0, 0, 7, 0),
                        HorizontalAlignment = HorizontalAlignment.Right });
                    GPUswitchSP.Children.Add(GR);
                }
            }
        }
    }
}