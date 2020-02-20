using Newtonsoft.Json;
using OMineGuardControlLibrary;
using OMineWatcher.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OMineWatcher.MVVM.Models
{
    public class OmgModel : IModel
    {
        public event Action Autostarted;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        public void IniModel() { }
        public OmgModel(OMGcontroller controller)
        {
            this.controller = controller;
            this.controller.SentInform += OMGsent;
        }
        public OMGcontroller controller;

        public IProfile Profile { get; set; }
        public List<string> Miners { get; set; }
        public IDefClock DefClock { get; set; }
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
                    (InfOHMCoreClocks != null? InfOHMCoreClocks.Length : 0),
                    (InfOHMMemoryClocks != null? InfOHMMemoryClocks.Length : 0),
                    (InfHashrates != null? InfHashrates.Length : 0),
                    Profile.GPUsSwitch.Count
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

        public int?[] ShAccepted { get; set; }
        public int? ShTotalAccepted { get; set; }
        public int?[] ShRejected { get; set; }
        public int? ShTotalRejected { get; set; }
        public int?[] ShInvalid { get; set; }
        public int? ShTotalInvalid { get; set; }

        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }
        public bool Indicator { get; set; } = false;

        private static string CurrentJSON = "";
        private void OMGsent(RigInform RO)
        {
            if(RO.ControlStruct != null) OMGsent(RO.ControlStruct);

            if (RO.GPUs != null) GPUs = RO.GPUs.Value;

            if (RO.Profile != null) 
            {
                CurrentJSON = JsonConvert.SerializeObject(RO.Profile);
                Profile = RO.Profile; 
            }
            if (RO.Logging != null) Loggong = RO.Logging;
            if (RO.InfPowerLimits != null) InfPowerLimits = RO.InfPowerLimits;
            if (RO.InfCoreClocks != null) InfCoreClocks = RO.InfCoreClocks;
            if (RO.InfMemoryClocks != null) InfMemoryClocks = RO.InfMemoryClocks;
            if (RO.InfOHMCoreClocks != null) InfOHMCoreClocks = RO.InfOHMCoreClocks;
            if (RO.InfOHMMemoryClocks != null) InfOHMMemoryClocks = RO.InfOHMMemoryClocks;
            if (RO.InfFanSpeeds != null) InfFanSpeeds = RO.InfFanSpeeds;
            if (RO.InfTemperatures != null) InfTemperatures = RO.InfTemperatures;
            if (RO.InfHashrates != null) InfHashrates = RO.InfHashrates;
            if (RO.TotalHashrate != null) TotalHashrate = RO.TotalHashrate;
            if (RO.WachdogInfo != null) WachdogInfo = RO.WachdogInfo;
            if (RO.LowHWachdog != null) LowHWachdog = RO.LowHWachdog;
            if (RO.IdleWachdog != null) IdleWachdog = RO.IdleWachdog;
            if (RO.Indication != null) Indicator = RO.Indication.Value;
            if (RO.Algoritms != null) Algoritms = RO.Algoritms;
            if (RO.Miners != null) Miners = RO.Miners;
            if (RO.DefClock != null) DefClock = RO.DefClock;
        }

        #region Commands
        public void CMD_SaveProfile(IProfile prof)
        {
            string newJSON = JsonConvert.SerializeObject(prof);
            if (newJSON != CurrentJSON)
            {
                CurrentJSON = newJSON;
                Profile = prof;
                controller.SendSetting(Profile, MSGtype.Profile);
            }
        }
        public void CMD_RunProfile(IProfile prof, int index)
        {
            Profile = prof;
            controller.SendSetting(new object[] { prof, index }, MSGtype.RunConfig);
        }
        public void CMD_ApplyClock(IProfile prof, int index)
        {
            Profile = prof;
            controller.SendSetting(new object[] { prof, index }, MSGtype.ApplyClock);
        }
        public void CMD_MinerLogShow()
        {
            controller.SendSetting(true, MSGtype.ShowMinerLog);
        }
        public void CMD_MinerLogHide()
        {
            controller.SendSetting(false, MSGtype.ShowMinerLog);
        }
        public void CMD_SwitchProcess()
        {
            controller.SendSetting(true, MSGtype.SwitchProcess);
        }
        #endregion
    }
}
