using OMineWatcher.Managers;
using System.Collections.Generic;
using System.ComponentModel;
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

        public OmgModel()
        {
            OMG_TCP.OMGsent += OMGsent;
        }

        private Profile CurrProf;
        public Profile Profile { get; set; }
        public List<string> Miners { get; set; }
        public DC DefClock { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }
        public string Loggong { get; set; }
        public OC OC { get; set; }
        public double[] Hashrates { get; set; }
        public int[] Temperatures { get; set; }
        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }
        public string ShowMLogTB { get; set; }
        public bool? Indicator { get; set; }
        private void OMGsent(OMGRootObject RO)
        {
            if (RO.Miners != null) Miners = RO.Miners;
            if (RO.Algoritms != null) Algoritms = RO.Algoritms;
            if (RO.DefClock != null) DefClock = (DC)RO.DefClock;
            if (RO.Hashrates != null) Hashrates = RO.Hashrates; 
            if (RO.Temperatures != null) Temperatures = RO.Temperatures; 
            if (RO.Indication != null) Indicator = RO.Indication; 
            if (RO.Logging != null) Loggong = RO.Logging; 
            if (RO.Overclock != null) OC = (OC)RO.Overclock; 
            if (RO.Profile != null) ProcessingProfile(RO.Profile); 
            if (RO.WachdogInfo != null) WachdogInfo = RO.WachdogInfo; 
            if (RO.LowHWachdog != null) LowHWachdog = RO.LowHWachdog; 
            if (RO.IdleWachdog != null) IdleWachdog = RO.IdleWachdog; 
            if (RO.ShowMLogTB != null) ShowMLogTB = RO.ShowMLogTB; 
        }

        private void ProcessingProfile(Profile prof)
        {
            if (CurrProf == null)
            {
                CurrProf = prof;
                Profile = prof;
            }
        }

        #region Commands
        public void cmd_SaveProfile(Profile prof)
        {
            CurrProf = prof;
            OMG_TCP.SendMSG(CurrProf, MSGtype.Profile);
        }
        public void cmd_RunProfile(Profile prof, int index)
        {
            CurrProf = prof;
            OMG_TCP.SendMSG(CurrProf, MSGtype.Profile);
            OMG_TCP.SendMSG(CurrProf.ConfigsList[index].ID, MSGtype.RunConfig);
        }
        public void cmd_ApplyClock(Profile prof, int index)
        {
            CurrProf = prof;
            OMG_TCP.SendMSG(CurrProf, MSGtype.Profile);
            OMG_TCP.SendMSG(CurrProf.ClocksList[index].ID, MSGtype.ApplyClock);
        }
        public void cmd_ShowMinerLog()
        {
            OMG_TCP.SendMSG(true, MSGtype.ShowMinerLog);
        }
        public void cmd_SwitchProcess()
        {
            if ((bool)Indicator)
            {
                OMG_TCP.SendMSG(true, MSGtype.KillProcess);
            }
            else
            {
                OMG_TCP.SendMSG(true, MSGtype.StartProcess);
            }
        }
        #endregion
    }
}
