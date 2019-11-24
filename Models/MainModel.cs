using OMineWatcher.Managers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            RigsWacher.RigStatusChanged += (i, status) => { Statuses[i] = status; };
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
        public void cmd_SaveRigs(List<Settings.Rig> rigs)
        {
            Settings.Rigs = rigs;

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