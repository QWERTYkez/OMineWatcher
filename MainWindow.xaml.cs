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
using OMineWatcher.Managers;
using System.Windows.Media.Effects;
using System.Net.NetworkInformation;
using eWeLink.API;

namespace OMineWatcher
{
    public class Tumbler : CheckBox { }
    public partial class MainWindow : Window
    {
        public static SynchronizationContext MainContext = SynchronizationContext.Current;
        public static MainWindow This;

        public MainWindow()
        {
            InitializeComponent();
            This = this;
            StartingApplication();
        }

        private void StartingApplication()
        {
            InitializeRigsSettings();
            GPUsCB.ItemsSource = new string[] { "Auto", "1", "2", "3", "4", "5",
                "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
            RigType.ItemsSource = new string[] { "OMineGuard", "HiveOS" };
        }

        #region Список ригов
        #region Индикация
        private static List<Ellipse> Indicators { get; set; } = new List<Ellipse>();
        public static List<Thread> PingIndicationThreads { get; set; } = new List<Thread>();
        private static int PingCheckDelay = 5; //sec
        private static void SetIndicatorColor(object o)
        {
            object[] x = (object[])o;
            int i = (int)(x[0]);
            Brush B = (Brush)(x[1]);
            Indicators[i].Fill = B;
        }
        private void AddIndicator(int i)
        {
            Ellipse E = new Ellipse
            {
                Height = 15,
                Width = 15,
                Fill = Brushes.Red,
                Margin = new Thickness(2, 4, 2, 4),
                Effect = new BlurEffect { Radius = 5 }
            };
            IndicatorsRigsSP.Children.Add(E);
            Indicators.Add(E);

            PingIndicationThreads.Add(new Thread(() => 
            {
                Ping ping = new Ping();
                IPStatus status;
                while (true)
                {
                    try
                    {
                        status = ping.Send(IPAddress.Parse(Settings.Rigs[i].IP), 200).Status;
                        if (status == IPStatus.Success)
                        {
                            MainContext.Send(SetIndicatorColor, new object[] { i, Brushes.Lime });
                        }
                        else { MainContext.Send(SetIndicatorColor, new object[] { i, Brushes.Red }); }
                    }
                    catch { MainContext.Send(SetIndicatorColor, new object[] { i, Brushes.Red }); }
                    Thread.Sleep(PingCheckDelay * 1000);
                }
            }));
            PingIndicationThreads[i].Start();
        }
        private void RemoveIndicator(int i)
        {
            IndicatorsRigsSP.Children.RemoveAt(i);
            Indicators.RemoveAt(i);
            PingIndicationThreads[i].Abort();
            PingIndicationThreads.RemoveAt(i);
        }
        #endregion

        private static List<Tumbler> Tumblers { get; set; } = new List<Tumbler>();
        private void AddTumbler()
        {
            Tumbler T = new Tumbler();
            WachingRigsSP.Children.Add(T);
            Tumblers.Add(T);
        }
        private void RemoveTumbler(int i)
        {
            WachingRigsSP.Children.RemoveAt(i);
            Tumblers.RemoveAt(i);
        }

        private void InitializeRigsSettings()
        {
            for (int i = 0; i < Settings.Rigs.Count; i++)
            {
                AddTumbler();
                AddIndicator(i);
            }
            RigsListBox.ItemsSource = (from x in Settings.Rigs select x.Name).ToList();
            SelectRig(null, null);
        }

        private void PlusRig(object sender, RoutedEventArgs e)
        {
            AddTumbler(); AddIndicator(Settings.Rigs.Count); Settings.AddRig();
            RigsListBox.ItemsSource = (from x in Settings.Rigs select x.Name).ToList();
            RigsListBox.SelectedIndex = Settings.Rigs.Count - 1;
        }
        private void MinusRig(object sender, RoutedEventArgs e)
        {
            int i = RigsListBox.SelectedIndex;
            if (RigsListBox.SelectedIndex > -1)
            {
                Settings.RemoveRig(i); RemoveTumbler(i); RemoveIndicator(i);
                RigsListBox.ItemsSource = (from x in Settings.Rigs select x.Name).ToList();
                if (RigsListBox.SelectedIndex == -1)
                {
                    RigsListBox.SelectedIndex = Settings.Rigs.Count - 1;
                }
            }
        }
        private void UpRig(object sender, RoutedEventArgs e)
        {
            int i = RigsListBox.SelectedIndex;
            if (i > 0)
            {
                Settings.Rig r = Settings.Rigs[i];
                Settings.Rigs.RemoveAt(i);
                Settings.Rigs.Insert(i - 1, r);
                RigsListBox.ItemsSource = (from x in Settings.Rigs select x.Name).ToList();
                Settings.SaveSettings();
            }
        }
        private void DownRig(object sender, RoutedEventArgs e)
        {
            int i = RigsListBox.SelectedIndex;
            if (i < RigsListBox.Items.Count - 1 && i > -1)
            {
                Settings.Rig r = Settings.Rigs[i];
                Settings.Rigs.RemoveAt(i);
                Settings.Rigs.Insert(i + 1, r);
                RigsListBox.ItemsSource = (from x in Settings.Rigs select x.Name).ToList();
                Settings.SaveSettings();
            }
        }
        private void SaveRig(object sender, RoutedEventArgs e)
        {
            int i = RigsListBox.SelectedIndex;
            Settings.Rigs[i].Name = SettingsRigName.Text;
            Settings.Rigs[i].IP = SettingsRigIP.Text;
            switch (RigType.SelectedItem)
            {
                case "OMineGuard":
                    Settings.Rigs[i].Type = Settings.RigType.OMineGuard;
                    break;
                case "HiveOS":
                    Settings.Rigs[i].Type = Settings.RigType.HiveOS;
                    break;
                default:
                    Settings.Rigs[i].Type = null;
                    break;
            }
            Settings.SaveSettings();
            RigsListBox.ItemsSource = (from x in Settings.Rigs select x.Name).ToList();
            RigsListBox.SelectedIndex = i;
        }
        private void SelectRig(object sender, SelectionChangedEventArgs e)
        {
            int i = RigsListBox.SelectedIndex;
            if (i > -1)
            {
                SettingsRigName.Text = Settings.Rigs[i].Name;
                SettingsRigIP.Text = Settings.Rigs[i].IP;

                switch (Settings.Rigs[i].Type)
                {
                    case Settings.RigType.OMineGuard:
                        RigType.SelectedItem = "OMineGuard";
                        break;
                    case Settings.RigType.HiveOS:
                        RigType.SelectedItem = "HiveOS";
                        break;
                    default:
                        RigType.SelectedIndex = -1;
                        break;
                }
            }
        }

        private void SelectRigType(object sender, SelectionChangedEventArgs e)
        {
            switch (RigType.SelectedItem)
            {
                case "OMineGuard":
                    OMGtabitem1.Visibility = Visibility.Visible;
                    OMGtabitem2.Visibility = Visibility.Visible;
                    OMGtabitem3.Visibility = Visibility.Visible;
                    OMGtabitem4.Visibility = Visibility.Visible;
                    OMGconnect.Visibility = Visibility.Visible;
                    break;
                case "HiveOS":
                    OMGtabitem1.Visibility = Visibility.Collapsed;
                    OMGtabitem2.Visibility = Visibility.Collapsed;
                    OMGtabitem3.Visibility = Visibility.Collapsed;
                    OMGtabitem4.Visibility = Visibility.Collapsed;
                    OMGconnect.Visibility = Visibility.Collapsed;
                    break;
                default:
                    OMGtabitem1.Visibility = Visibility.Collapsed;
                    OMGtabitem2.Visibility = Visibility.Collapsed;
                    OMGtabitem3.Visibility = Visibility.Collapsed;
                    OMGtabitem4.Visibility = Visibility.Collapsed;
                    OMGconnect.Visibility = Visibility.Collapsed;
                    break;
            }
        }


        #region OMineWacher
        #region Конфигурации майнинга
        private void GPUsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GPUswitchSP.Children.Clear();

            string Selected = (string)GPUsCB.SelectedItem;
            if (Selected == "Auto")
            {
                GPUsSwitchHeader.Visibility = Visibility.Collapsed;
                GPUswitchB.Visibility = Visibility.Collapsed;
            }
            else
            {
                GPUsSwitchHeader.Visibility = Visibility.Visible;
                GPUswitchB.Visibility = Visibility.Visible;
                byte k = Convert.ToByte(Selected);

                for (byte n = 0; n < k; n++)
                {
                    Grid GR = new Grid { Width = 60 };
                    GR.Children.Add(new TextBlock { Text = "GPU" + n, Effect = null, Foreground = Brushes.White });
                    GR.Children.Add(new CheckBox
                    {
                        Name = "g" + n.ToString(),
                        Margin = new Thickness(0, 0, 7, 0),
                        IsChecked = true,
                        HorizontalAlignment = HorizontalAlignment.Right
                    });
                    GPUswitchSP.Children.Add(GR);
                }
            }
        }


        #endregion

        #endregion
        #endregion

        #region General Settings
        #region eWeLink
        private async void eWeLinkConnect(object sender, RoutedEventArgs e)
        {
            eWeAccountState.Text = "Подключение";
            string login = eWeLoginBox.Text;
            string pass = eWePasswordBox.Password;
            if (await Task.Run(() => eWeLinkClient.AutheWeLink(login, pass)))
            {
                Settings.GenSets.eWeLogin = login;
                Settings.GenSets.eWePassword = pass;
                eWeAccountState.Text = "Аккаунт подключен";
            }
            else
            {
                Settings.GenSets.eWeLogin = null;
                Settings.GenSets.eWePassword = null;
                eWeAccountState.Text = "Ошибка";
                await Task.Run(() => Thread.Sleep(2000));
                eWeAccountState.Text = "Аккаунт не подключен";
            }
        }
        private void eWeLinkDisconnect(object sender, RoutedEventArgs e)
        {
            Settings.GenSets.eWeLogin = null;
            Settings.GenSets.eWePassword = null;
            eWeLoginBox.Text = "";
            eWePasswordBox.Password = "";
            eWeAccountState.Text = "Аккаунт не подключен";
        }
        #endregion
        #endregion

    }
}