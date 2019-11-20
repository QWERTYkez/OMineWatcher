using OMineWatcher.Managers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OMineWatcher.Models
{
    public class MainModel : INotifyPropertyChanged
    {
        public MainModel()
        {
            InternetConnectionWacher.InternetConnectionLost += NullAllStatuses;
            NullAllStatuses();
            RigsWacher.SendInform += inf => { RigInform = inf; };
            RigsWacher.RigStatusChanged += (n, status) => { Statuses[n] = status; };
        }
        public void InitializeModel()
        {
            Rigs = Settings.Rigs;
            GenSettings = Settings.GenSets;
        }
        private void NullAllStatuses()
        {
            List<RigStatus?> lrs = new List<RigStatus?>();
            foreach (Settings.Rig r in Settings.Rigs) lrs.Add(null);
            Statuses = new ObservableCollection<RigStatus?>(lrs);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public List<Settings.Rig> Rigs { get; set; }
        public Settings._GenSettings GenSettings { get; set; }
        public ObservableCollection<RigStatus?> Statuses { get; set; }
        public RigInform RigInform { get; set; }
        #endregion

        #region Commands
        public void cmd_StopWach(string ip)
        {
            OMG_TCP.StopInformStream(ip);
        }
        public void cmd_SaveRigs(List<Settings.Rig> rigs)
        {
            Settings.Rigs = rigs;

            List<string> IPs = (from r in Settings.Rigs select r.IP).ToList();
            List<string> newIPs = (from r in rigs select r.IP).ToList();
            foreach (string ip in (IPs.Except(newIPs))) // удаленные IPs
            {
                OMG_TCP.StopInformStream(ip);
            }

            while (rigs.Count != Statuses.Count)
            {
                if (rigs.Count > Statuses.Count)
                    Statuses.Add(RigStatus.offline);
                else
                {
                    Statuses.RemoveAt(Statuses.Count - 1);
                }
            }
            Settings.SaveSettings();
        }
        public void cmd_SaveGenSettings(Settings._GenSettings gs)
        {
            Settings.GenSets = gs;
            Settings.SaveSettings();
        }
        #endregion
    }
}