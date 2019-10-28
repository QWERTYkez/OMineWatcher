using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading;
using System.Net;
using OMineWatcher.Managers;
using System.Windows.Media.Effects;
using System.Net.NetworkInformation;
using eWeLink.API;
using OMG = OMineWatcher.Managers.OMG_TCP;

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
            InitializeeWeLink();
            InitializeOMGcontrol();

        }

        #region Список ригов
        #region Блок выбора
        #region Индикация
        private static List<Ellipse> Indicators { get; set; } = new List<Ellipse>();
        private static List<RigStatus> RigsStatuses { get; set; } = new List<RigStatus>();
        private enum RigStatus
        {
            offline,
            online,
            works
        }
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
            RigsStatuses.Add(new RigStatus());

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
                            RigsStatuses[i] = RigStatus.online;
                            MainContext.Send(SetIndicatorColor, new object[] { i, Brushes.Yellow });
                        }
                        else
                        {
                            RigsStatuses[i] = RigStatus.offline;
                            MainContext.Send(SetIndicatorColor, new object[] { i, Brushes.Red });
                        }
                    }
                    catch
                    {
                        RigsStatuses[i] = RigStatus.offline;
                        MainContext.Send(SetIndicatorColor, new object[] { i, Brushes.Red });
                    }
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
            RigType.ItemsSource = new string[] { "OMineGuard", "HiveOS" };
            GPUsCB.ItemsSource = new string[] { "Auto", "1", "2", "3", "4", "5",
                "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
            SelectRigType(null, null);
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

                eWeDevicesBox.SelectionChanged -= SelecteWeDevice;
                if (Settings.Rigs[i].eWeDevice != "")
                {
                    eWeTumbler.IsEnabled = true;
                    eWeTumbler.IsChecked = true;
                    eWeDevicesBox.SelectedItem = Settings.Rigs[i].eWeDevice;
                }
                else
                {
                    eWeTumbler.IsEnabled = false;
                    eWeDevicesBox.SelectedItem = "";
                }
                eWeDevicesBox.SelectionChanged += SelecteWeDevice;
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

                    OMGtabitem1.IsEnabled = false;
                    OMGtabitem2.IsEnabled = false;
                    OMGtabitem3.IsEnabled = false;
                    OMGtabitem4.IsEnabled = false;
                    
                    OMGconnect.Visibility = Visibility.Visible;
                    OMGconnect.Content = "Подключиться";
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
        #endregion

        #region Базовые настройки
        private void SelecteWeDevice(object sender, SelectionChangedEventArgs e)
        {
            int i = RigsListBox.SelectedIndex;
            if (eWeDevicesBox.SelectedIndex == -1)
            {
                eWeTumbler.IsEnabled = false;
                Settings.Rigs[i].eWeDevice = "";
            }
            else if ((string)eWeDevicesBox.SelectedItem == "")
            {
                eWeTumbler.IsEnabled = false;
                Settings.Rigs[i].eWeDevice = "";
            }
            else
            {
                eWeTumbler.IsEnabled = true;
                eWeTumbler.IsChecked = false;
            }
        }
        #endregion
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
        private async void InitializeeWeLink()
        {
            if (Settings.GenSets.eWeLogin != "" && Settings.GenSets.eWeLogin != null)
            {
                eWeLinkClient.SetAuth(Settings.GenSets.eWeLogin, Settings.GenSets.eWePassword);
                eWeLoginBox.Text = Settings.GenSets.eWeLogin;
                eWePasswordBox.Password = Settings.GenSets.eWePassword;
                eWeAccountState.Text = "Аккаунт подключен";

                List<_eWelinkDevice> LE = await Task.Run(() => eWeLinkClient.GetDevices());
                List<string> LST = (from x in LE select x.name).ToList();
                LST.Insert(0, "");
                eWeDevicesBox.ItemsSource = LST;
            }
        }
        private void eWeTumbler_Checked(object sender, RoutedEventArgs e)
        {
            int i = RigsListBox.SelectedIndex;
            Settings.Rigs[i].eWeDevice = (string)eWeDevicesBox.SelectedItem;
        }
        private void eWeTumbler_Unchecked(object sender, RoutedEventArgs e)
        {
            int i = RigsListBox.SelectedIndex;
            Settings.Rigs[i].eWeDevice = "";
        }

        #endregion

        #endregion

        #region OMGcontrol
        private void InitializeOMGcontrol()
        {
            OMG.OMGcontrolLost += OMGcontrolLost;
            OMG.OMGcontrolReceived += OMGcontrolReceived;
            OMG.OMGsent += OMGsent;

            Digits.Text = Settings.GenSets.Digits.ToString();
            DigitsSlider.Value = Settings.GenSets.Digits;
            DigitsSlider.ValueChanged += DigitsSlider_ValueChanged;
        }
        private void OMGconnect_Click(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Content)
            {
                case "Подключиться":
                    OMG.ConnectToOMG(Settings.Rigs[RigsListBox.SelectedIndex].IP);
                    break;
                case "Отключиться":
                    OMG.OMGcontrolDisconnect();
                    break;
            }
        }
        private static void OMGcontrolReceived()
        {
            MainContext.Send((object o) =>
            {
                This.OMGtabitem1.IsEnabled = true;
                This.OMGtabitem2.IsEnabled = true;
                This.OMGtabitem3.IsEnabled = true;
                This.OMGtabitem4.IsEnabled = true;

                This.OMGconnect.Content = "Отключиться";

                OMGindication();
            }, 
            null);
        }
        private static void OMGcontrolLost()
        {
            MainContext.Send((object o) => 
            {
                This.OMGtabitem1.IsEnabled = false;
                This.OMGtabitem2.IsEnabled = false;
                This.OMGtabitem3.IsEnabled = false;
                This.OMGtabitem4.IsEnabled = false;

                This.RigSettings.SelectedIndex = 0;

                This.OMGconnect.Content = "Подключиться";

                OMGworking = null;

                This.MinerLog.Document.Blocks.Clear();
            }, 
            null);
        }
        private void OMGsent(OMG.RootObject RO)
        {
            if (RO.Hasrates != null)
            {
                Task.Run(() => 
                {
                    string str = "";
                    foreach (double d in RO.Hasrates)
                    {
                        str += ToNChar(d.ToString());
                    }

                    MainContext.Send((object o) =>
                    {
                        This.GPUsHashrate.Text = " " + str.TrimStart(',');
                        This.GPUsHashrate2.Text = " " + str.TrimStart(',');
                        This.TotalHashrate.Text = RO.Hasrates.Sum().ToString().Replace(',', '.');
                        This.TotalHashrate2.Text = RO.Hasrates.Sum().ToString().Replace(',', '.');
                    },
                    null);
                });
            }
            if (RO.Indication != null) OMGworking = RO.Indication;
            if (RO.Logging != null)
            { MainContext.Send((object o) => This.MinerLog.AppendText(RO.Logging), null); }    
            if (RO.Overclock != null)
            {
                Task.Run(() => 
                {
                    string[] MS = new string[5];

                    foreach (int x in RO.Overclock?.MSI_PowerLimits)
                    {
                        MS[0] += ToNChar(x.ToString() + "%");
                    }
                    foreach (int x in RO.Overclock?.MSI_CoreClocks)
                    {
                        MS[1] += ToNChar(x.ToString());
                    }
                    foreach (int x in RO.Overclock?.MSI_MemoryClocks)
                    {
                        MS[2] += ToNChar(x.ToString());
                    }
                    foreach (uint x in RO.Overclock?.MSI_FanSpeeds)
                    {
                        MS[3] += ToNChar(x.ToString());
                    }
                    foreach (float x in RO.Overclock?.OHM_Temperatures)
                    {
                        MS[4] += ToNChar(x.ToString() + "°C");
                    }

                    for (int i = 0; i < MS.Length; i++)
                    {
                        MS[i] = MS[i] ?? "null";
                    }

                    MainContext.Send((object o) =>
                    {
                        This.GPUsPowerLimit.Text = " " + MS[0].TrimStart(',');
                        This.GPUsCoreClock.Text = " " + MS[1].TrimStart(',');
                        This.GPUsMemoryClocks.Text = " " + MS[2].TrimStart(',');
                        This.GPUsFans.Text = " " + MS[3].TrimStart(',');
                        This.GPUsTemps.Text = " " + MS[4].TrimStart(',');
                        This.GPUsTemps2.Text = " " + MS[4].TrimStart(',');
                    }, 
                    null);
                });
            }
            if (RO.Profile != null)
            {
                MainContext.Send((object o) =>
                {
                    This.RigName.Text = RO.Profile.RigName;
                    This.AutoStart.IsChecked = RO.Profile.Autostart;
                },
                null);
                

            }
        }
        public static string ToNChar(string s)
        {
            string ext = "";
            char[] ch = s.ToCharArray();
            Queue<char> st = new Queue<char>();
            for (int i = 0; i < Settings.GenSets.Digits - 1; i++)
            { st.Enqueue(' '); }
            for (int i = 0; i < Settings.GenSets.Digits - 1 && i < ch.Length; i++)
            {
                st.Enqueue(ch[i]);
                st.Dequeue();
            }
            ch = st.ToArray();
            for (int i = 0; i < Settings.GenSets.Digits - 1; i++)
            { ext += ch[i]; }
            return "," + ext.Replace(',', '.');
        }
        private void DigitsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Digits.Text = DigitsSlider.Value.ToString();
            Settings.GenSets.Digits = Convert.ToInt32(DigitsSlider.Value);
        }

        private static bool? OMGworking;
        private static void OMGindication()
        {
            Task.Run(() => 
            {
                OMGworking = false;
                while (OMGworking != null)
                {
                    while (OMGworking == true)
                    {
                        MainContext.Send((object o) =>
                        {
                            This.IndicatorEl.Fill = Brushes.Lime;
                            This.IndicatorEl2.Fill = Brushes.Lime;
                        }, 
                        null);
                        Thread.Sleep(700);
                        MainContext.Send((object o) =>
                        {
                            This.IndicatorEl.Fill = null;
                            This.IndicatorEl2.Fill = null;
                        }, 
                        null);
                        Thread.Sleep(300);
                    }
                    while (OMGworking == false)
                    {
                        MainContext.Send((object o) =>
                        {
                            This.IndicatorEl.Fill = Brushes.Red;
                            This.IndicatorEl2.Fill = Brushes.Red;
                        }, 
                        null);
                        Thread.Sleep(200);
                    }
                }
            });
        }

        #endregion

        
    }
}