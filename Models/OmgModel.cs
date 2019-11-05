using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using OMG = OMineWatcher.Managers.OMG_TCP;

namespace OMineWatcher.Models
{
    public class OmgModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public OmgModel()
        {
            OMG.OMGsent += OMGsent;
        }

        private OMG.Profile CurrProf;
        public OMG.Profile Profile { get; set; }
        public List<string> Miners { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }
        private void OMGsent(OMG.RootObject RO)
        {
            if (RO.Miners != null) Miners = RO.Miners;
            if (RO.Algoritms != null) Algoritms = RO.Algoritms;
            //if (RO.Hasrates != null)
            //{
            //    Task.Run(() =>
            //    {
            //        string str = "";
            //        foreach (double d in RO.Hasrates)
            //        {
            //            str += ToNChar(d.ToString());
            //        }

            //        MainContext.Send((object o) =>
            //        {
            //            This.GPUsHashrate.Content = " " + str.TrimStart(',');
            //            This.GPUsHashrate2.Content = " " + str.TrimStart(',');
            //            This.TotalHashrate.Content = RO.Hasrates.Sum().ToString().Replace(',', '.');
            //            This.TotalHashrate2.Content = RO.Hasrates.Sum().ToString().Replace(',', '.');
            //        },
            //        null);
            //    });
            //}
            //if (RO.Indication != null)
            //{
            //    OMGworking = RO.Indication;
            //    string s = (bool)RO.Indication ? "Завершить процесс" : "Запустить процесс";
            //    {
            //        MainContext.Send((object o) =>
            //        {
            //            This.KillProcess.Content = s;
            //            This.KillProcess2.Content = s;
            //        },
            //      null);
            //    }
            //}
            //if (RO.Logging != null)
            //{ MainContext.Send((object o) => This.MinerLog.AppendText(RO.Logging), null); }
            //if (RO.Overclock != null)
            //{
            //    Task.Run(() =>
            //    {
            //        string[] MS = new string[5];

            //        foreach (int x in RO.Overclock?.MSI_PowerLimits)
            //        {
            //            MS[0] += ToNChar(x.ToString() + "%");
            //        }
            //        foreach (int x in RO.Overclock?.MSI_CoreClocks)
            //        {
            //            MS[1] += ToNChar(x.ToString());
            //        }
            //        foreach (int x in RO.Overclock?.MSI_MemoryClocks)
            //        {
            //            MS[2] += ToNChar(x.ToString());
            //        }
            //        foreach (uint x in RO.Overclock?.MSI_FanSpeeds)
            //        {
            //            MS[3] += ToNChar(x.ToString());
            //        }
            //        if (RO.Overclock?.OHM_Temperatures[0] != null)
            //        {
            //            foreach (float x in RO.Overclock?.OHM_Temperatures)
            //            {
            //                MS[4] += ToNChar(x.ToString() + "°C");
            //            }
            //        }
            //        for (int i = 0; i < MS.Length; i++)
            //        {
            //            MS[i] = MS[i] ?? "null";
            //        }

            //        MainContext.Send((object o) =>
            //        {
            //            This.GPUsPowerLimit.Content = " " + MS[0].TrimStart(',');
            //            This.GPUsCoreClock.Content = " " + MS[1].TrimStart(',');
            //            This.GPUsMemoryClocks.Content = " " + MS[2].TrimStart(',');
            //            This.GPUsFans.Content = " " + MS[3].TrimStart(',');
            //            This.GPUsTemps.Content = " " + MS[4].TrimStart(',');
            //            This.GPUsTemps2.Content = " " + MS[4].TrimStart(',');
            //        },
            //        null);
            //    });
            //}
            if (RO.Profile != null)
            {
                ProcessingProfile(RO.Profile);
            }
            //if (RO.WachdogInfo != null) InformMSG(This.WachdogINFO, RO.WachdogInfo);
            //if (RO.LowHWachdog != null) InformMSG(This.LowHWachdog, RO.LowHWachdog);
            //if (RO.IdleWachdog != null) InformMSG(This.IdleWachdog, RO.IdleWachdog);
            //if (RO.ShowMLogTB != null) InformMSG(This.ShowMLogTB, RO.ShowMLogTB);
        }

        
        private void ProcessingProfile(OMG.Profile prof)
        {
            if (CurrProf == null)
            {
                CurrProf = prof;
                Profile = prof;
                //MainContext.Send((object o) =>
                //{
                //    This.RigName.Text = prof.RigName;
                //    This.RigName.TextChanged += RigNameChange;

                //    This.AutoStart.IsChecked = prof.Autostart;
                //    This.AutoStart.Checked += AutoStartSwitch;
                //    This.AutoStart.Unchecked += AutoStartSwitch;

                //    if (prof.GPUsSwitch != null)
                //    { This.GPUsCB.SelectedIndex = prof.GPUsSwitch.Length; }
                //    else { This.GPUsCB.SelectedIndex = 0; }

                //    This.ConfigsList.ItemsSource = from i in prof.ConfigsList select i.Name;
                //    This.ConfigsList.SelectionChanged += ConfigSelected;

                //    This.Overclock.ItemsSource = from i in prof.ClocksList select i.Name;
                //    This.Algotitm.ItemsSource = CurrAlgoritms.Keys;
                //    This.Miner.ItemsSource = CurrMiners;

                //    This.ClocksList.ItemsSource = from i in prof.ClocksList select i.Name;
                //    This.ClocksList.SelectionChanged += ClocklistSelected;

                //    This.WachdogTimerSlider.Value = (double)prof.TimeoutWachdog;
                //    This.WachdogTimerSec.Content = ((double)prof.TimeoutWachdog).ToString();
                //    This.WachdogTimerSlider.ValueChanged += WachdogTimerSlider_ValueChanged;

                //    This.IdleTimeoutSlider.Value = (double)prof.TimeoutIdle;
                //    This.IdleTimeoutSec.Content = ((double)prof.TimeoutIdle).ToString();
                //    This.IdleTimeoutSlider.ValueChanged += IdleTimeoutSlider_ValueChanged;

                //    This.LHTimeoutSlider.Value = (double)prof.TimeoutLH;
                //    This.LHTimeoutSec.Content = ((double)prof.TimeoutLH).ToString();
                //    This.LHTimeoutSlider.ValueChanged += LHTimeoutSlider_ValueChanged;

                //    This.VKInformerToggle.IsChecked = prof.Informer.VkInform;
                //    This.VKInformerToggle.Checked += VKInformerToggle_Click;
                //    This.VKInformerToggle.Unchecked += VKInformerToggle_Click;

                //    This.VKuserID.IsEnabled = prof.Informer.VkInform;
                //    This.VKuserID.Text = prof.Informer.VKuserID;
                //    This.VKuserID.TextChanged += VKuserID_TextChanged;

                //    This.Algotitm.SelectionChanged += Algotitm_SelectionChanged;
                //    This.PlusConfig.Click += PlusConfig_Click;
                //    This.MinusConfig.Click += MinusConfig_Click;
                //    This.ApplyConfig.Click += ApplyConfig_Click;
                //    This.StartConfig.Click += StartConfig_Click;

                //    This.PlusClock.Click += PlusClock_Click;
                //    This.MinusClock.Click += MinusClock_Click;
                //    This.SaveClock.Click += SaveClock_Click;
                //    This.ApplyClock.Click += ApplyClock_Click;

                //    This.SwitcherPL.Checked += OCswitch_Checked;
                //    This.SwitcherPL.Unchecked += OCswitch_Checked;
                //    This.SwitcherCC.Checked += OCswitch_Checked;
                //    This.SwitcherCC.Unchecked += OCswitch_Checked;
                //    This.SwitcherMC.Checked += OCswitch_Checked;
                //    This.SwitcherMC.Unchecked += OCswitch_Checked;
                //    This.SwitcherFS.Checked += OCswitch_Checked;
                //    This.SwitcherFS.Unchecked += OCswitch_Checked;

                //    This.KillProcess.Click += KillProcess_Click;
                //    This.KillProcess2.Click += KillProcess_Click;
                //    This.ShowMinerLog.Click += ShowMinerLog_Click;
                //},
                //null);
            }
        }

        #region Commands
        public void cmd_SaveProfile(OMG.Profile prof)
        {
            CurrProf = prof;
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
        }
        public void cmd_RunProfile(OMG.Profile prof, int index)
        {
            CurrProf = prof;
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
            OMG.SendMSG(CurrProf.ConfigsList[index].ID, OMG.MSGtype.RunConfig);
        }
        #endregion
    }
}
