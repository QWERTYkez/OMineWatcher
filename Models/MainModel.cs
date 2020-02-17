using OMineWatcher.Managers;
using OMineWatcher.Rigs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OMineWatcher.Models
{
    public class MainModel : INotifyPropertyChanged
    {
        public MainModel()
        {
            InternetConnectionWacher.InternetConnectionLost += NullAllStatuses;
            NullAllStatuses();
        }
        public void InitializeModel()
        {
            Rigs = Settings.Rigs;
            rigs = Rigs;
            StatusesReset();
            GenSettings = Settings.GenSets;
            PoolsSets = Settings.Pools;
        }
        public OMGcontroller controller = new OMGcontroller();
        public void StatusesReset()
        {
            for (int i = 0; i < Statuses.Count; i++)
            {
                rigs[i].StatusChangedClear();
                Statuses[i] = rigs[i].CurrentStatus;
                var x = i;
                rigs[i].StatusChanged += s => Task.Run(() => Statuses[x] = s);
            }
        }

        private void NullAllStatuses()
        {
            List<RigStatus?> lrs = new List<RigStatus?>();
            foreach (var _ in Settings.Rigs) lrs.Add(null);
            Statuses = new ObservableCollection<RigStatus?>(lrs);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public List<Settings.Rig> Rigs { get; set; }
        private List<Settings.Rig> rigs;
        public Settings._GenSettings GenSettings { get; set; }
        public ObservableCollection<RigStatus?> Statuses { get; set; }
        public List<PoolSet> PoolsSets { get; set; }
        #endregion

        #region Commands
        public void cmd_SaveRigs(List<Settings.Rig> rigs)
        {
            Settings.Rigs = rigs;
            this.rigs = rigs;

            while (rigs.Count != Statuses.Count)
            {
                if (rigs.Count > Statuses.Count)
                    Statuses.Add(RigStatus.offline);
                else
                {
                    Statuses.RemoveAt(Statuses.Count - 1);
                }
            }
            StatusesReset();

            Settings.SaveSettings();
        }
        public void cmd_SavePools(List<PoolSet> pools)
        {
            Settings.Pools = pools;
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