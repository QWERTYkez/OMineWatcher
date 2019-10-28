using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using OMineWatcher.Managers;
using OMG = OMineWatcher.Managers.OMG_TCP;

namespace OMineWatcher
{
    public partial class MainWindow : Window
    {
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

                This.RigSettings.SelectedIndex = 1;

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

                //очистка пространства
                CurrProf = null;
                This.MinerLog.Document.Blocks.Clear();

                This.RigName.TextChanged -= RigNameChange;
                This.AutoStart.Checked -= AutoStartSwitch;
                This.AutoStart.Unchecked -= AutoStartSwitch;
                This.ConfigsList.SelectionChanged -= ConfigSelected;
                This.ClocksList.SelectionChanged -= ClocklistSelected;
                This.WachdogTimerSlider.ValueChanged -= WachdogTimerSlider_ValueChanged;
                This.IdleTimeoutSlider.ValueChanged -= IdleTimeoutSlider_ValueChanged;
                This.LHTimeoutSlider.ValueChanged -= LHTimeoutSlider_ValueChanged;
                This.VKInformerToggle.Checked -= VKInformerToggle_Click;
                This.VKInformerToggle.Unchecked -= VKInformerToggle_Click;
                This.VKuserID.TextChanged -= VKuserID_TextChanged;
            },
            null);
        }
        private void OMGsent(OMG.RootObject RO)
        {
            if (RO.Miners != null) CurrMiners = RO.Miners;
            if (RO.Algoritms != null) CurrAlgoritms = RO.Algoritms;
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
            if (RO.Profile != null) ProcessingProfile(RO.Profile);
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

        #region Profile
        private static string[] CurrMiners;
        private static Dictionary<string, int[]> CurrAlgoritms;
        private static OMG.Profile CurrProf;

        private static void ProcessingProfile(OMG.Profile prof)
        {
            CurrProf = prof;
            MainContext.Send((object o) =>
            {
                This.RigName.Text = prof.RigName;
                This.RigName.TextChanged += RigNameChange;

                This.AutoStart.IsChecked = prof.Autostart;
                This.AutoStart.Checked += AutoStartSwitch;
                This.AutoStart.Unchecked += AutoStartSwitch;

                if (prof.GPUsSwitch != null)
                { This.GPUsCB.SelectedIndex = prof.GPUsSwitch.Length; }
                else { This.GPUsCB.SelectedIndex = 0; }

                This.ConfigsList.ItemsSource = prof.ConfigsList;
                This.ConfigsList.SelectionChanged += ConfigSelected;

                This.Overclock.ItemsSource = prof.ClocksList;
                This.Algotitm.ItemsSource = CurrAlgoritms.Keys;
                This.Miner.ItemsSource = CurrMiners;

                This.ClocksList.ItemsSource = prof.ClocksList;
                This.ClocksList.SelectionChanged += ClocklistSelected;

                This.WachdogTimerSlider.Value = (double)prof.TimeoutWachdog;
                This.WachdogTimerSec.Text = ((double)prof.TimeoutWachdog).ToString();
                This.WachdogTimerSlider.ValueChanged += WachdogTimerSlider_ValueChanged;

                This.IdleTimeoutSlider.Value = (double)prof.TimeoutIdle;
                This.IdleTimeoutSec.Text = ((double)prof.TimeoutIdle).ToString();
                This.IdleTimeoutSlider.ValueChanged += IdleTimeoutSlider_ValueChanged;

                This.LHTimeoutSlider.Value = (double)prof.TimeoutLH;
                This.LHTimeoutSec.Text = ((double)prof.TimeoutLH).ToString();
                This.LHTimeoutSlider.ValueChanged += LHTimeoutSlider_ValueChanged;

                This.VKInformerToggle.IsChecked = prof.Informer.VkInform;
                This.VKInformerToggle.Checked += VKInformerToggle_Click;
                This.VKInformerToggle.Unchecked += VKInformerToggle_Click;

                This.VKuserID.IsEnabled = prof.Informer.VkInform;
                This.VKuserID.Text = prof.Informer.VKuserID;
                This.VKuserID.TextChanged += VKuserID_TextChanged;
            },
            null);
        }

        #region Manipulations
        private static void ConfigSelected(object sender, SelectionChangedEventArgs e)
        {
            int n = This.ConfigsList.SelectedIndex;
            if (n == -1)
            {
                This.MiningConfigName.Text = "";
                This.Algotitm.SelectedIndex = -1;
                This.Miner.SelectedIndex = -1;
                This.Overclock.SelectedIndex = -1;
                This.Pool.Text = "";
                This.Port.Text = "";
                This.Wallet.Text = "";
                This.Params.Text = "";
                This.MinHashrate.Text = "";
            }
            else
            {
                if (PM.Profile.ConfigsList[n].Algoritm != "")
                {
                    Algotitm.SelectedItem = PM.Profile.ConfigsList[n].Algoritm;
                }
                else Algotitm.SelectedIndex = -1;

                if (PM.Profile.ConfigsList[n].Miner != null)
                {
                    Miner.SelectedItem = PM.Profile.ConfigsList[n].Miner;
                }
                else Miner.SelectedIndex = -1;

                if (PM.Profile.ConfigsList[n].ClockID != null)
                {
                    Overclock.SelectedItem = PM.GetClock(PM.Profile.ConfigsList[n].ClockID).Name;
                }
                else Overclock.SelectedIndex = -1;

                This.MiningConfigName.Text = PM.Profile.ConfigsList[n].Name;
                This.Pool.Text = PM.Profile.ConfigsList[n].Pool;
                This.Port.Text = PM.Profile.ConfigsList[n].Port;
                This.Wallet.Text = PM.Profile.ConfigsList[n].Wallet;
                This.Params.Text = PM.Profile.ConfigsList[n].Params;
                This.MinHashrate.Text = PM.Profile.ConfigsList[n].MinHashrate.ToString();
            }
        }
        private static void ClocklistSelected(object sender, SelectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region SendActions
        private static void RigNameChange(object sender, TextChangedEventArgs e)
        {
            CurrProf.RigName = ((TextBox)sender).Text;
            OMG.SendAction(new OMG.Profile { RigName = ((TextBox)sender).Text });
        }
        private static void AutoStartSwitch(object sender, RoutedEventArgs e)
        {
            CurrProf.Autostart = ((Tumbler)sender).IsChecked;
            OMG.SendAction(new OMG.Profile { Autostart = ((Tumbler)sender).IsChecked });
        }
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

                bool[] x = new bool[k];
                if (CurrProf.GPUsSwitch.Length == k)
                { x = CurrProf.GPUsSwitch; }
                else
                {
                    for (int i = 0; i < k; i++)
                    {
                        x[i] = true;
                    }
                    CurrProf.GPUsSwitch = x;
                }
                for (byte n = 0; n < k; n++)
                {
                    Grid GR = new Grid { Width = 60 };
                    GR.Children.Add(new TextBlock { Text = "GPU" + n, Effect = null, Foreground = Brushes.White });
                    CheckBox CB = new CheckBox
                    {
                        Name = "g" + n.ToString(),
                        Margin = new Thickness(0, 0, 7, 0),
                        IsChecked = x[n],
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    CB.Checked += SwitchGPU;
                    CB.Unchecked += SwitchGPU;
                    GR.Children.Add(CB);
                    GPUswitchSP.Children.Add(GR);
                }
                CurrProf.GPUsSwitch = x;
                OMG.SendAction(new OMG.Profile { GPUsSwitch = x });
            }
        }
        private void SwitchGPU(object sender, RoutedEventArgs e)
        {
            int i = Convert.ToInt32(((CheckBox)sender).Name.Replace("g", ""));
            if (((CheckBox)sender).IsChecked == true)
            {
                CurrProf.GPUsSwitch[i] = true;
            }
            else
            {
                CurrProf.GPUsSwitch[i] = false;
            }
            OMG.SendAction(new OMG.Profile { GPUsSwitch = CurrProf.GPUsSwitch });
        }
        private static void WachdogTimerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CurrProf.TimeoutWachdog = Convert.ToInt32(e.NewValue);
            OMG.SendAction(new OMG.Profile { TimeoutWachdog = Convert.ToInt32(e.NewValue) });
        }
        private static void IdleTimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CurrProf.TimeoutIdle = Convert.ToInt32(e.NewValue);
            OMG.SendAction(new OMG.Profile { TimeoutIdle = Convert.ToInt32(e.NewValue) });
        }
        private static void LHTimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CurrProf.TimeoutLH = Convert.ToInt32(e.NewValue);
            OMG.SendAction(new OMG.Profile { TimeoutLH = Convert.ToInt32(e.NewValue) });
        }
        private static void VKInformerToggle_Click(object sender, RoutedEventArgs e)
        {
            This.VKuserID.IsEnabled = This.VKInformerToggle.IsChecked.GetValueOrDefault();
            CurrProf.Informer.VkInform = This.VKInformerToggle.IsChecked.GetValueOrDefault();
            OMG.SendAction(new OMG.Profile
            {
                Informer = new OMG.InformManager
                {
                    VkInform = This.VKInformerToggle.IsChecked.GetValueOrDefault()
                }
            });
        }
        private static void VKuserID_TextChanged(object sender, TextChangedEventArgs e)
        {
            CurrProf.Informer.VKuserID = This.VKuserID.Text;
            OMG.SendAction(new OMG.Profile
            {
                Informer = new OMG.InformManager
                {
                    VKuserID = This.VKuserID.Text
                }
            });
        }
        #endregion

        #endregion
    }
}
