using OMineWatcher.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OMineWatcher.Models
{
    public class OmgModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private Profile CurrProf;

        public void IniModel()
        {
            OMGcontroller.SentInform += OMGsent;
        }

        public Profile Profile { get; set; }
        public List<string> Miners { get; set; }
        public DefClock DefClock { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }

        public string Loggong { get; set; }

        public int GPUs { get; set; }
        private static readonly object gpkey = new object();
        public void ResetGPUs()
        {
            lock (gpkey)
            {
                int[] l = new int[]
                {
                    (InfPowerLimits != null? InfPowerLimits.Length : 0),
                    (InfCoreClocks != null? InfCoreClocks.Length : 0),
                    (InfMemoryClocks != null? InfMemoryClocks.Length : 0),
                    (InfFanSpeeds != null? InfFanSpeeds.Length : 0),
                    (InfTemperatures != null? InfTemperatures.Length : 0),
                    CurrProf.GPUsSwitch.Count
                };
                int m = l.Max();
                if (GPUs != m)
                {
                    GPUs = m;
                }
            }
        }

        public int?[] InfPowerLimits { get; set; }
        public int?[] InfCoreClocks { get; set; }
        public int?[] InfMemoryClocks { get; set; }
        public int?[] InfOHMCoreClocks { get; set; }
        public int?[] InfOHMMemoryClocks { get; set; }
        public int?[] InfFanSpeeds { get; set; }
        public int?[] InfTemperatures { get; set; }
        public double?[] InfHashrates { get; set; }
        public double? TotalHashrate { get; set; }

        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }
        public bool Indicator { get; set; } = false;

        private void OMGsent(OMGRootObject RO)
        {
            if (RO.Algoritms != null) Algoritms = RO.Algoritms;
            if (RO.DefClock != null) DefClock = RO.DefClock;
            if (RO.GPUs != null) GPUs = RO.GPUs.Value;
            if (RO.IdleWachdog != null) IdleWachdog = RO.IdleWachdog;
            if (RO.Indication != null) Algoritms = RO.Algoritms;
            if (RO.InfCoreClocks != null) Algoritms = RO.Algoritms;
            if (RO.InfFanSpeeds != null) Algoritms = RO.Algoritms;
            if (RO.InfHashrates != null) Algoritms = RO.Algoritms;
            if (RO.InfMemoryClocks != null) Algoritms = RO.Algoritms;
            if (RO.InfOHMCoreClocks != null) Algoritms = RO.Algoritms;
            if (RO.InfOHMMemoryClocks != null) Algoritms = RO.Algoritms;
            if (RO.InfPowerLimits != null) Algoritms = RO.Algoritms;
            if (RO.InfTemperatures != null) Algoritms = RO.Algoritms;
            if (RO.Logging != null) Algoritms = RO.Algoritms;
            if (RO.LowHWachdog != null) Algoritms = RO.Algoritms;
            if (RO.Miners != null) Algoritms = RO.Algoritms;
            if (RO.Profile != null) Algoritms = RO.Algoritms;
            if (RO.TotalHashrate != null) Algoritms = RO.Algoritms;
            if (RO.WachdogInfo != null) Algoritms = RO.Algoritms;
        }

        #region Commands
        public void CMD_SaveProfile(Profile prof)
        {
            CurrProf = prof;
            OMGcontroller.SendSetting(CurrProf, MSGtype.Profile);
        }
        public void CMD_RunProfile(Profile prof, int index)
        {
            CurrProf = prof;
            OMGcontroller.SendSetting(new object[] { prof, index }, MSGtype.RunConfig);
        }
        public void CMD_ApplyClock(Profile prof, int index)
        {
            CurrProf = prof;
            OMGcontroller.SendSetting(new object[] { prof, index }, MSGtype.ApplyClock);
        }
        public void CMD_MinerLogShow()
        {
            OMGcontroller.SendSetting(true, MSGtype.ShowMinerLog);
        }
        public void CMD_MinerLogHide()
        {
            OMGcontroller.SendSetting(false, MSGtype.ShowMinerLog);
        }
        public void CMD_SwitchProcess()
        {
            OMGcontroller.SendSetting(true, MSGtype.SwitchProcess);
        }
        #endregion
    }
}
