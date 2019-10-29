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
using Newtonsoft.Json;

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
            TextSizeTB.Text = Settings.GenSets.LogTextSize.ToString();
            TextSizeSlider.Value = Settings.GenSets.LogTextSize;
            TextSizeSlider.ValueChanged += TextSizeSlider_ValueChanged;
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
                This.Algotitm.SelectionChanged -= Algotitm_SelectionChanged;
                This.PlusConfig.Click -= PlusConfig_Click;
                This.MinusConfig.Click -= MinusConfig_Click;
                This.ApplyConfig.Click -= ApplyConfig_Click;
                This.StartConfig.Click -= StartConfig_Click;
                This.PlusClock.Click -= PlusClock_Click;
                This.MinusClock.Click -= MinusClock_Click;
                This.SaveClock.Click -= SaveClock_Click;
                This.ApplyClock.Click -= ApplyClock_Click;
                This.SwitcherPL.Checked -= OCswitch_Checked;
                This.SwitcherPL.Unchecked -= OCswitch_Checked;
                This.SwitcherCC.Checked -= OCswitch_Checked;
                This.SwitcherCC.Unchecked -= OCswitch_Checked;
                This.SwitcherMC.Checked -= OCswitch_Checked;
                This.SwitcherMC.Unchecked -= OCswitch_Checked;
                This.SwitcherFS.Checked -= OCswitch_Checked;
                This.SwitcherFS.Unchecked -= OCswitch_Checked;
                This.KillProcess.Click -= KillProcess_Click;
                This.KillProcess2.Click -= KillProcess_Click;
                This.ShowMinerLog.Click -= ShowMinerLog_Click;
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
            if (RO.Indication != null)
            {
                OMGworking = RO.Indication;
                string s = (bool)RO.Indication ? "Завершить процесс" : "Запустить процесс";
                { MainContext.Send((object o) => 
                {
                    This.KillProcess.Content = s;
                    This.KillProcess2.Content = s;
                }, 
                null); }
            }
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
            if (RO.WachdogInfo != null) InformMSG(This.WachdogINFO, RO.WachdogInfo);
            if (RO.LowHWachdog != null) InformMSG(This.LowHWachdog, RO.LowHWachdog);
            if (RO.IdleWachdog != null) InformMSG(This.IdleWachdog, RO.IdleWachdog);
            if (RO.ShowMLogTB != null) InformMSG(This.ShowMLogTB, RO.ShowMLogTB);
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
            Settings.SaveSettings();
        }
        private void TextSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MinerLog.FontSize = TextSizeSlider.Value;
            TextSizeTB.Text = TextSizeSlider.Value.ToString();
            Settings.GenSets.LogTextSize = Convert.ToInt32(TextSizeSlider.Value);
            Settings.SaveSettings();
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

        private static void InformMSG(TextBlock TB, string msg)
        {
            MainContext.Send((object o) =>
            {
                if (msg != "")
                {
                    TB.Text = msg;
                    TB.Visibility = Visibility.Visible;
                }
                else
                {
                    TB.Visibility = Visibility.Collapsed;
                }
            }, null);
        }
        private static List<string> CurrMiners;
        private static Dictionary<string, int[]> CurrAlgoritms;
        private static OMG.Profile CurrProf;
        private static void ProcessingProfile(OMG.Profile prof)
        {
            if (CurrProf == null)
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

                    This.ConfigsList.ItemsSource = from i in prof.ConfigsList select i.Name;
                    This.ConfigsList.SelectionChanged += ConfigSelected;

                    This.Overclock.ItemsSource = from i in prof.ClocksList select i.Name;
                    This.Algotitm.ItemsSource = CurrAlgoritms.Keys;
                    This.Miner.ItemsSource = CurrMiners;

                    This.ClocksList.ItemsSource = from i in prof.ClocksList select i.Name;
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

                    This.Algotitm.SelectionChanged += Algotitm_SelectionChanged;
                    This.PlusConfig.Click += PlusConfig_Click;
                    This.MinusConfig.Click += MinusConfig_Click;
                    This.ApplyConfig.Click += ApplyConfig_Click;
                    This.StartConfig.Click += StartConfig_Click;

                    This.PlusClock.Click += PlusClock_Click;
                    This.MinusClock.Click += MinusClock_Click;
                    This.SaveClock.Click += SaveClock_Click;
                    This.ApplyClock.Click += ApplyClock_Click;

                    This.SwitcherPL.Checked += OCswitch_Checked;
                    This.SwitcherPL.Unchecked += OCswitch_Checked;
                    This.SwitcherCC.Checked += OCswitch_Checked;
                    This.SwitcherCC.Unchecked += OCswitch_Checked;
                    This.SwitcherMC.Checked += OCswitch_Checked;
                    This.SwitcherMC.Unchecked += OCswitch_Checked;
                    This.SwitcherFS.Checked += OCswitch_Checked;
                    This.SwitcherFS.Unchecked += OCswitch_Checked;

                    This.KillProcess.Click += KillProcess_Click;
                    This.KillProcess2.Click += KillProcess_Click;
                    This.ShowMinerLog.Click += ShowMinerLog_Click;
                },
                null);
            }
        }

        #region Manipulations
        // Configs
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
                if (CurrProf.ConfigsList[n].Algoritm != "")
                {
                    This.Algotitm.SelectedItem = CurrProf.ConfigsList[n].Algoritm;
                }
                else This.Algotitm.SelectedIndex = -1;

                if (CurrProf.ConfigsList[n].Miner != null)
                {
                    This.Miner.SelectedIndex = CurrProf.ConfigsList[n].Miner.GetValueOrDefault();
                }
                else This.Miner.SelectedIndex = -1;

                if (CurrProf.ConfigsList[n].ClockID != null)
                {
                    foreach (OMG.Overclock c in CurrProf.ClocksList)
                    {
                        if (c.ID == CurrProf.ConfigsList[n].ClockID)
                        {
                            This.Overclock.SelectedItem = c.Name;
                        }
                    }
                }
                else This.Overclock.SelectedIndex = -1;

                This.MiningConfigName.Text = CurrProf.ConfigsList[n].Name;
                This.Pool.Text = CurrProf.ConfigsList[n].Pool;
                This.Port.Text = CurrProf.ConfigsList[n].Port;
                This.Wallet.Text = CurrProf.ConfigsList[n].Wallet;
                This.Params.Text = CurrProf.ConfigsList[n].Params;
                This.MinHashrate.Text = CurrProf.ConfigsList[n].MinHashrate.ToString();
            }
        }
        private static void Algotitm_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (This.Algotitm.SelectedIndex == -1)
            {
                This.Miner.SelectedIndex = -1;
                This.Miner.IsEnabled = false;
            }
            else
            {
                This.Miner.IsEnabled = true;
                int[] x = CurrAlgoritms[(string)This.Algotitm.SelectedItem];
                This.Miner.ItemsSource = from m in CurrMiners where x.Contains(CurrMiners.IndexOf(m)) select m;
                This.Miner.SelectedIndex = -1;
            }
        }
        
        // Overclocks
        private static void ClocklistSelected(object sender, SelectionChangedEventArgs e)
        {
            int n = This.ClocksList.SelectedIndex;
            if (n == -1)
            {
                This.ClockName.IsEnabled = false;
                This.SwitcherPL.IsEnabled = false;
                This.SwitcherCC.IsEnabled = false;
                This.SwitcherMC.IsEnabled = false;
                This.SwitcherFS.IsEnabled = false;
                SetParam(This.SwitcherPL, This.PowLim);
                SetParam(This.SwitcherCC, This.CoreClock);
                SetParam(This.SwitcherMC, This.MemoryClock);
                SetParam(This.SwitcherFS, This.FanSpeed);
                This.ClockName.Text = "";
            }
            else
            {
                This.ClockName.Text = CurrProf.ClocksList[n].Name;
                This.ClockName.IsEnabled = true;
                This.SwitcherPL.IsEnabled = true;
                This.SwitcherCC.IsEnabled = true;
                This.SwitcherMC.IsEnabled = true;
                This.SwitcherFS.IsEnabled = true;
                SetParam(This.SwitcherPL, This.PowLim, CurrProf.ClocksList[n].PowLim);
                SetParam(This.SwitcherCC, This.CoreClock, CurrProf.ClocksList[n].CoreClock);
                SetParam(This.SwitcherMC, This.MemoryClock, CurrProf.ClocksList[n].MemoryClock);
                SetParam(This.SwitcherFS, This.FanSpeed, CurrProf.ClocksList[n].FanSpeed);
            }
        }
        private static void OCswitch_Checked(object sender, RoutedEventArgs e)
        {
            string nm = ((CheckBox)sender).Name;
            bool b = !((bool)((CheckBox)sender).IsChecked);
            switch (nm)
            {
                case "SwitcherPL":
                    This.PowLim.IsEnabled = b;
                    break;
                case "SwitcherCC":
                    This.CoreClock.IsEnabled = b;
                    break;
                case "SwitcherMC":
                    This.MemoryClock.IsEnabled = b;
                    break;
                case "SwitcherFS":
                    This.FanSpeed.IsEnabled = b;
                    break;
            }
        }
        private static void SetParam(CheckBox CB, TextBox TB, int[] prams)
        {
            string str = "";
            if (prams == null)
            {
                CB.IsChecked = true;
                TB.Text = "";
            }
            else
            {
                CB.IsChecked = false;
                foreach (int x in prams)
                {
                    str += ToNChar(x.ToString());
                }
                TB.Text = " " + str.TrimStart(',');
            }
        }
        private static void SetParam(CheckBox CB, TextBox TB)
        {
            CB.IsChecked = true;
            TB.Text = "";
        }
        #endregion

        #region SendProfile
        private static void RigNameChange(object sender, TextChangedEventArgs e)
        {
            CurrProf.RigName = ((TextBox)sender).Text;
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
        }
        private static void AutoStartSwitch(object sender, RoutedEventArgs e)
        {
            CurrProf.Autostart = ((Tumbler)sender).IsChecked;
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
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
                OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
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
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
        }
        private static void WachdogTimerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int x = Convert.ToInt32(e.NewValue);
            CurrProf.TimeoutWachdog = x;
            if (This.IdleTimeoutSlider.Value < x)
            {
                This.IdleTimeoutSlider.Value = x;
                This.IdleTimeoutSlider.Minimum = x;
            }
            else
            {
                This.IdleTimeoutSlider.Minimum = x;
            }
            This.WachdogTimerSec.Text = This.WachdogTimerSlider.Value.ToString();
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
        }
        private static void IdleTimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CurrProf.TimeoutIdle = Convert.ToInt32(e.NewValue);
            This.IdleTimeoutSec.Text = e.NewValue.ToString();
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
        }
        private static void LHTimeoutSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CurrProf.TimeoutLH = Convert.ToInt32(e.NewValue);
            This.LHTimeoutSec.Text = e.NewValue.ToString();
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
        }
        private static void VKInformerToggle_Click(object sender, RoutedEventArgs e)
        {
            This.VKuserID.IsEnabled = This.VKInformerToggle.IsChecked.GetValueOrDefault();
            CurrProf.Informer.VkInform = This.VKInformerToggle.IsChecked.GetValueOrDefault();
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
        }
        private static void VKuserID_TextChanged(object sender, TextChangedEventArgs e)
        {
            CurrProf.Informer.VKuserID = This.VKuserID.Text;
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
        }

        private static void PlusConfig_Click(object sender, RoutedEventArgs e)
        {
            CurrProf.ConfigsList.Add(new OMG.Config()
            {
                Name = "Новый конфиг",
                Algoritm = "",
                Pool = "",
                Wallet = "",
                Params = "",
                MinHashrate = 0,
                ID = DateTime.UtcNow.ToBinary()
            });
            This.ConfigsList.ItemsSource = from i in CurrProf.ConfigsList select i.Name;
            This.ConfigsList.SelectedIndex = CurrProf.ConfigsList.Count - 1;
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
        }
        private static void MinusConfig_Click(object sender, RoutedEventArgs e)
        {
            int n = This.ConfigsList.SelectedIndex;
            if (n != -1)
            {
                if (CurrProf.ConfigsList[n].ID == CurrProf.StartedID)
                {
                    CurrProf.StartedID = null;
                }
                CurrProf.ConfigsList.RemoveAt(n);
                This.ConfigsList.ItemsSource = from i in CurrProf.ConfigsList select i.Name;
                This.ConfigsList.SelectedIndex = -1;
                OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
            }
        }
        private static void ApplyConfig_Click(object sender, RoutedEventArgs e) { ApplyConfigM(); }
        private static bool ApplyConfigM()
        {
            int n = This.ConfigsList.SelectedIndex;

            if (n != -1)
            {
                double x;

                CurrProf.ConfigsList[n].Name = This.MiningConfigName.Text;
                CurrProf.ConfigsList[n].Algoritm = This.Algotitm.Text;
                CurrProf.ConfigsList[n].Miner = 
                    (This.Miner.SelectedIndex >= 0 ? This.Miner.SelectedIndex : (int?)null);
                CurrProf.ConfigsList[n].Pool = This.Pool.Text;
                CurrProf.ConfigsList[n].Port = This.Port.Text;
                CurrProf.ConfigsList[n].Wallet = This.Wallet.Text;
                CurrProf.ConfigsList[n].Params = This.Params.Text;
                CurrProf.ConfigsList[n].ClockID = 
                    (This.Overclock.SelectedIndex >= 0 ? CurrProf.ClocksList[n].ID : (long?)null);
                CurrProf.ConfigsList[n].MinHashrate = 
                    (double.TryParse(This.MinHashrate.Text, out x) ? x : 0);

                OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
                This.ConfigsList.ItemsSource = from i in CurrProf.ConfigsList select i.Name;
                This.ConfigsList.SelectedIndex = n;
                return true;
            }
            else { return false; }
        }

        private static void MinusClock_Click(object sender, RoutedEventArgs e)
        {
            int n = This.ClocksList.SelectedIndex;

            if (n != -1)
            {
                long id = CurrProf.ClocksList[n].ID;
                foreach (OMG.Config c in CurrProf.ConfigsList)
                {
                    if (c.ClockID == id)
                    {
                        c.ClockID = null;
                    }
                }

                CurrProf.ClocksList.RemoveAt(n);
                This.ClocksList.ItemsSource = from i in CurrProf.ClocksList select i.Name;
                This.ClocksList.SelectedIndex = -1;
                OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
                This.Overclock.ItemsSource = from i in CurrProf.ClocksList select i.Name;
            }
        }
        private static void PlusClock_Click(object sender, RoutedEventArgs e)
        {
            CurrProf.ClocksList.Add(new OMG.Overclock()
            {
                Name = "Новый разгон",
                ID = DateTime.UtcNow.ToBinary()
            });
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
            This.ClocksList.ItemsSource = from i in CurrProf.ClocksList select i.Name;
            This.ClocksList.SelectedIndex = CurrProf.ClocksList.Count - 1;
            This.Overclock.ItemsSource = from i in CurrProf.ClocksList select i.Name;
            This.Overclock.SelectedIndex = CurrProf.ClocksList.Count - 1;
        }
        private static void SaveClock_Click(object sender, RoutedEventArgs e) { SaveClockM(); }
        private static bool SaveClockM()
        {
            int n = This.ClocksList.SelectedIndex;
            if (n != -1)
            {
                CurrProf.ClocksList[n].Name = This.ClockName.Text;
                if (!(NewClocks(n, ref CurrProf.ClocksList[n].PowLim, This.PowLim, This.SwitcherPL))) return false;
                if (!(NewClocks(n, ref CurrProf.ClocksList[n].CoreClock, This.CoreClock, This.SwitcherCC))) return false;
                if (!(NewClocks(n, ref CurrProf.ClocksList[n].MemoryClock, This.MemoryClock, This.SwitcherMC))) return false;
                if (!(NewClocks(n, ref CurrProf.ClocksList[n].FanSpeed, This.FanSpeed, This.SwitcherFS))) return false;
                OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
                This.ClocksList.ItemsSource = from i in CurrProf.ClocksList select i.Name;
                This.ClocksList.SelectedIndex = n;
                This.Overclock.ItemsSource = from i in CurrProf.ClocksList select i.Name;

                int k = This.ConfigsList.SelectedIndex;
                if (k != -1)
                {
                    This.ConfigsList.SelectedIndex = -1;
                    This.ConfigsList.SelectedIndex = k;
                }
                return true;
            }
            else { return false; }
        }
        private static bool NewClocks(int n, ref int[] Clock, TextBox source, CheckBox switcher)
        {
            string X = source.Text;
            try
            {
                Clock =
                (
                    (source.Text != "" && !(bool)switcher.IsChecked) ?
                    JsonConvert.DeserializeObject<int[]>($"[{X}]") :
                    null
                );
            }
            catch
            {
                Task.Run(() =>
                {
                    MainContext.Send((object o) => { source.Text = "Неправильный формат"; }, null);
                    Thread.Sleep(2000);
                    MainContext.Send((object o) => { source.Text = X; }, null);
                });
                return false;
            }
            return true;
        }
        #endregion

        #region Actions
        private static void StartConfig_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyConfigM())
            {
                OMG.SendMSG(CurrProf.ConfigsList[This.ConfigsList.SelectedIndex].ID, OMG.MSGtype.RunConfig);
                MainContext.Send((object o) => { This.RigSettings.SelectedIndex = 3; }, null);
            }
        }
        private static void ApplyClock_Click(object sender, RoutedEventArgs e)
        {
            if (SaveClockM())
            {
                OMG.SendMSG(CurrProf.ClocksList[This.ClocksList.SelectedIndex].ID, OMG.MSGtype.ApplyClock);
            }
        }
        private static void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Content == "Запустить процесс")
            {
                OMG.SendMSG(true, OMG.MSGtype.StartProcess);
            }
            if (((Button)sender).Content == "Завершить процесс")
            {
                OMG.SendMSG(true, OMG.MSGtype.KillProcess);
            }
        }
        private static void ShowMinerLog_Click(object sender, RoutedEventArgs e)
        {
            OMG.SendMSG(true, OMG.MSGtype.ShowMinerLog);
        }
        #endregion
    }
}
