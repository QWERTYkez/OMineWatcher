using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        public OMG.DC DefClock { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }
        public string Loggong { get; set; }
        public OMG.OC OC { get; set; }
        public double[] Hashrates { get; set; }
        public int[] Temperatures { get; set; }
        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }
        public string ShowMLogTB { get; set; }
        public bool? Indicator { get; set; }
        private void OMGsent(OMG.RootObject RO)
        {
            if (RO.Miners != null) Miners = RO.Miners;
            if (RO.Algoritms != null) Algoritms = RO.Algoritms;
            if (RO.DefClock != null) DefClock = (OMG.DC)RO.DefClock;
            if (RO.Hashrates != null) Hashrates = RO.Hashrates; 
            if (RO.Temperatures != null) Temperatures = RO.Temperatures; 
            if (RO.Indication != null) Indicator = RO.Indication; 
            if (RO.Logging != null) Loggong = RO.Logging; 
            if (RO.Overclock != null) OC = (OMG.OC)RO.Overclock; 
            if (RO.Profile != null) ProcessingProfile(RO.Profile); 
            if (RO.WachdogInfo != null) WachdogInfo = RO.WachdogInfo; 
            if (RO.LowHWachdog != null) LowHWachdog = RO.LowHWachdog; 
            if (RO.IdleWachdog != null) IdleWachdog = RO.IdleWachdog; 
            if (RO.ShowMLogTB != null) ShowMLogTB = RO.ShowMLogTB; 
        }

        private void ProcessingProfile(OMG.Profile prof)
        {
            if (CurrProf == null)
            {
                CurrProf = prof;
                Profile = prof;
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
        public void cmd_ApplyClock(OMG.Profile prof, int index)
        {
            CurrProf = prof;
            OMG.SendMSG(CurrProf, OMG.MSGtype.Profile);
            OMG.SendMSG(CurrProf.ClocksList[index].ID, OMG.MSGtype.ApplyClock);
        }
        public void cmd_ShowMinerLog()
        {
            OMG.SendMSG(true, OMG.MSGtype.ShowMinerLog);
        }
        public void cmd_SwitchProcess()
        {
            if ((bool)Indicator)
            {
                OMG.SendMSG(true, OMG.MSGtype.KillProcess);
            }
            else
            {
                OMG.SendMSG(true, OMG.MSGtype.StartProcess);
            }
        }
        #endregion
    }
}
