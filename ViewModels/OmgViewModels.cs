using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using OMineWatcher.Models;
using PropertyChanged;

namespace OMineWatcher.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class OmgViewModels
    {
        readonly OmgModel _model = new OmgModel();
        public OmgViewModels()
        {
            _model.PropertyChanged += ModelChanged;

            SetRigName = new RelayCommand(obj =>
            {
                Profile.RigName = RigName;
                _model.cmd_SaveProfile(Profile);
            });
            SetAutoRun = new RelayCommand(obj =>
            {
                Profile.Autostart = AutoRun;
                _model.cmd_SaveProfile(Profile);
            });
            SetGPUsSwitch = new RelayCommand(obj =>
            {
                Profile.GPUsSwitch = GPUsSwitch;
                _model.cmd_SaveProfile(Profile);
            });
            SelectConfig = new RelayCommand(obj => 
            {
                if (SelectedConfigIndex > -1)
                {
                    ConfigName = Profile.ConfigsList[SelectedConfigIndex].Name;
                    MinHashrate = Profile.ConfigsList[SelectedConfigIndex].MinHashrate;
                    Pool = Profile.ConfigsList[SelectedConfigIndex].Pool;
                    Port = Profile.ConfigsList[SelectedConfigIndex].Port;
                    Wallet = Profile.ConfigsList[SelectedConfigIndex].Wallet;
                    Params = Profile.ConfigsList[SelectedConfigIndex].Params;
                    if (Profile.ConfigsList[SelectedConfigIndex].ClockID != null)
                    {
                        foreach (Managers.OMG_TCP.Overclock c in Profile.ClocksList)
                        {
                            if (c.ID == Profile.ConfigsList[SelectedConfigIndex].ClockID)
                            {
                                ConfigSelectedOcerclockItem = c.Name;
                            }
                        }
                    }
                    else { ConfigSelectedOverclockIndex = -1; }
                    if (Profile.ConfigsList[SelectedConfigIndex].Algoritm != "")
                    {
                        foreach (string alg in Algoritms)
                        {
                            if (alg == Profile.ConfigsList[SelectedConfigIndex].Algoritm)
                            {
                                SelectedAlgoritmItem = alg;
                            }
                        }
                    }
                    else { SelectedAlgoritmIndex = -1; }
                    if (Profile.ConfigsList[SelectedConfigIndex].Miner != null)
                    {
                        SelectedMinerItem = MinersList[(int)Profile.ConfigsList[SelectedConfigIndex].Miner];
                    }
                    else
                    {
                        SelectedMinerIndex = 0;
                    }
                }
            });
            SelectAlgoritm = new RelayCommand(obj => 
            {
                if (SelectedAlgoritmIndex > -1)
                {
                    int[] x = Algs[Algs.Keys.ToList()[SelectedAlgoritmIndex]];
                    Miners = from m in MinersList where x.Contains(MinersList.IndexOf(m)) select m;
                    SelectedMinerIndex = -1;
                }
                else { Miners = null; }
            });
            PlusConfig = new RelayCommand(obj => 
            {
                Profile.ConfigsList.Add(new Managers.OMG_TCP.Config());
                ConfigsNames = from i in Profile.ConfigsList select i.Name;
                _model.cmd_SaveProfile(Profile);
            });
            MinusConfig = new RelayCommand(obj =>
            {
                if (SelectedConfigIndex > -1)
                {
                    Profile.ConfigsList.RemoveAt(SelectedConfigIndex);
                    ConfigsNames = from i in Profile.ConfigsList select i.Name;
                    _model.cmd_SaveProfile(Profile);
                }
            });
            SaveConfig = new RelayCommand(obj =>
            {
                if (SelectedConfigIndex > -1)
                {
                    SaveProfile();
                    _model.cmd_SaveProfile(Profile);
                }
            });
            StartConfig = new RelayCommand(obj =>
            {
                if (SelectedConfigIndex > -1)
                {
                    SaveProfile();
                    _model.cmd_RunProfile(Profile, SelectedConfigIndex);
                }
            });
        }
        private void SaveProfile()
        {
            Profile.ConfigsList[SelectedConfigIndex].Name = ConfigName;
            Profile.ConfigsList[SelectedConfigIndex].MinHashrate = MinHashrate;
            Profile.ConfigsList[SelectedConfigIndex].Pool = Pool;
            Profile.ConfigsList[SelectedConfigIndex].Port = Port;
            Profile.ConfigsList[SelectedConfigIndex].Wallet = Wallet;
            Profile.ConfigsList[SelectedConfigIndex].Params = Params;
            if (ConfigSelectedOverclockIndex > 0)
            {
                Profile.ConfigsList[SelectedConfigIndex].ClockID = Profile.ClocksList[ConfigSelectedOverclockIndex - 1].ID;
            }
            else { Profile.ConfigsList[SelectedConfigIndex].ClockID = null; }
            if (SelectedAlgoritmIndex > -1)
            {
                Profile.ConfigsList[SelectedConfigIndex].Algoritm = Algs.Keys.ToList()[SelectedAlgoritmIndex];
            }
            else { Profile.ConfigsList[SelectedConfigIndex].Algoritm = ""; }
            if (SelectedMinerIndex > -1)
            {
                Profile.ConfigsList[SelectedConfigIndex].Miner = MinersList.IndexOf((string)SelectedMinerItem);
            }
            else
            {
                Profile.ConfigsList[SelectedConfigIndex].Miner = null;
            }
            int n = SelectedConfigIndex;
            ConfigsNames = from i in Profile.ConfigsList select i.Name;
            SelectedConfigIndex = n;
        }

        private Managers.OMG_TCP.Profile Profile;
        private Dictionary<string, int[]> Algs;
        private List<string> MinersList;
        private void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Profile":
                    Profile = _model.Profile;
                    GPUsSwitch = Profile.GPUsSwitch;
                    if (Profile.GPUsSwitch != null)
                    {
                        GPUsCountSelected = _model.Profile.GPUsSwitch.Count;
                    }
                    else { GPUsCountSelected = 0; }
                    RigName = Profile.RigName;
                    AutoRun = Profile.Autostart;
                    ConfigsNames = from i in Profile.ConfigsList select i.Name;
                    List<string> CL = (from i in Profile.ClocksList select i.Name).ToList();
                    CL.Insert(0, "---");
                    ConfigOverclocks = CL;
                    break;
                case "Miners":
                    MinersList = _model.Miners;
                    break;
                case "Algoritms":
                    Algs = _model.Algoritms;
                    Algoritms = Algs.Keys;
                    break;
            }
        }

        #region Configs
        public string RigName { get; set; }
        public RelayCommand SetRigName { get; set; }

        public bool AutoRun { get; set; }
        public RelayCommand SetAutoRun { get; set; }

        public List<string> GPUsCounts { get; set; } = new List<string>() { "Auto", "1", "2",
            "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
        public bool GPUsCanSelect { get; set; } = false;
        public int GPUsCountSelected { get; set; } = -1;
        public List<bool> GPUsSwitch { get; set; }
        public RelayCommand SetGPUsSwitch { get; set; }

        #region Config
        public IEnumerable<string> ConfigsNames { get; set; }
        public int SelectedConfigIndex { get; set; } = -1;
        public RelayCommand SelectConfig { get; set; }

        public string ConfigName { get; set; }
        public IEnumerable<string> ConfigOverclocks { get; set; }
        public int ConfigSelectedOverclockIndex { get; set; } = -1;
        public object ConfigSelectedOcerclockItem { get; set; }
        public IEnumerable<string> Algoritms { get; set; }
        public int SelectedAlgoritmIndex { get; set; } = -1;
        public object SelectedAlgoritmItem { get; set; }
        public RelayCommand SelectAlgoritm { get; set; }
        public IEnumerable<string> Miners { get; set; }
        public int SelectedMinerIndex { get; set; } = -1;
        public object SelectedMinerItem { get; set; }
        public double MinHashrate { get; set; }
        public string Pool { get; set; }
        public string Port { get; set; }
        public string Wallet { get; set; }
        public string Params { get; set; }

        public RelayCommand PlusConfig { get; set; }
        public RelayCommand MinusConfig { get; set; }
        public RelayCommand SaveConfig { get; set; }
        public RelayCommand StartConfig { get; set; }
        #endregion
        #endregion
        #region Overclock

        #endregion
        #region Log

        #endregion
        #region BaseSettings

        #endregion
    }
}
