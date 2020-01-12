using eWeLink.API;
using HiveOS.API;
using OMineWatcher.Managers;
using OMineWatcher.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OMineWatcher.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public MainModel _model;
        public void InitializeMainViewModel()
        {
            _model = new MainModel();
            _model.PropertyChanged += ModelChanged;
            _model.Statuses.CollectionChanged += (sender, e) => Indicators = _model.Statuses.ToList();
            _model.InitializeModel();

            InitializeRigsControlCommands();
            InitializeIndicatorsAndTumblers();
            InitializeOMGSwitchConnectCommand();
            InitializeCurrentRigSettingsCommand();
            InitializeHiveCommands();
            InitializeeWeCommands();
            InitializeCurrentScaleCommand();
            InitializeBaseScaleCommand();
            InitializeInformerCommands();
            InitializeHiveWorkerCommands();
            IniWachdogSettingsCommands();
        }

        public ObservableCollection<Settings.Rig> Rigs { get; set; }
        private Settings._GenSettings GenSettings;

        private void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Rigs": {
                        Rigs = new ObservableCollection<Settings.Rig>(_model.Rigs);
                        RigsNames = from r in Rigs orderby r.Index select r.Name;
                        for (int i = 0; i < RigsNames.Count(); i++)
                        {
                            Indicators.Add(RigStatus.offline);
                            Watch.Add(Rigs[i].Waching);
                            {
                                if (Rigs[i].Waching)
                                    AddRigPanel(i);
                                else
                                    RemoveRigPanel(i);
                            }
                        }
                    } break;
                case "GenSettings": {
                        GenSettings = _model.GenSettings;

                        StartOfRange = GenSettings.TotalMinTemp;
                        StartOfRangeD = GenSettings.TotalMinTemp;
                        EndOfRange = GenSettings.TotalMaxTemp;
                        EndOfRangeD = GenSettings.TotalMaxTemp;

                        Task.Run(() => 
                        {
                            if (GenSettings.eWeLogin != "" && GenSettings.eWeLogin != null)
                            {
                                eWeLinkClient.SetAuth(GenSettings.eWeLogin, GenSettings.eWePassword);
                                eWeLogin = GenSettings.eWeLogin;
                                eWePasswordSend = GenSettings.eWePassword;
                                eWeAccountState = "Аккаунт подключен";
                                List<_eWelinkDevice> LE = eWeLinkClient.GetDevices();
                                if (LE != null)
                                {
                                    List<string> LST = (from x in LE select x.name).ToList();
                                    LST.Insert(0, "---");
                                    eWeDevicesNames = LST;
                                }
                            }
                        });

                        Task.Run(() =>
                        {
                            if (GenSettings.HiveLogin != "" && GenSettings.HiveLogin != null)
                            {
                                AuthenticationStatus st = HiveClient.HiveAuthentication(
                                    GenSettings.HiveLogin, GenSettings.HivePassword);
                                if (st.Status)
                                {
                                    HiveLogin = GenSettings.HiveLogin;
                                    HivePasswordSend = GenSettings.HivePassword;
                                    HiveConnection = true;
                                    HiveAccountState = "Подключение к Hive установлено";
                                    HiveConnectionState = "Подключение к Hive установлено";

                                    HiveWorkers = HiveClient.GetWorkers();

                                    if (HiveWorkers != null)
                                    {
                                        List<string> LST = (from w in HiveWorkers select w.name).ToList();
                                        LST.Insert(0, "---");
                                        HiveWorkersNames = LST;
                                    }
                                }
                                else { HiveAccountState = st.Message; }
                            }
                            else
                            {
                                HiveAccountState = "Подключение к Hive отсутствует";
                                HiveConnectionState = "Подключение к Hive отсутствует";
                            }
                        });
                        
                        VKuserID = GenSettings.VKuserID;
                    } break;
            }
        }

        #region RigsControl
        public IEnumerable<string> RigsNames { get; set; }
        public string SelectedRigString { get; set; }
        public int SelectedRigIndex { get; set; } = -1;
        public RelayCommand SelectRig { get; set; }

        public string RigName { get; set; }
        public string RigIP { get; set; }
        public IEnumerable<string> RigTypes { get; set; } = Settings.RigTypes;
        public string SelectedRigTypeString { get; set; }
        public int SelectedRigTypeIndex { get; set; } = -1;

        public bool MinusButtonEnable { get; set; } = false;
        public RelayCommand RigMinus { get; set; }
        public RelayCommand RigPlus { get; set; }
        public bool UPButtonEnable { get; set; } = false;
        public RelayCommand RigUp { get; set; }
        public bool DownButtonEnable { get; set; } = false;
        public RelayCommand RigDown { get; set; }
        public bool SaveButtonEnable { get; set; } = false;
        public RelayCommand RigSave { get; set; }
         
        private void InitializeRigsControlCommands()
        {
            SelectRig = new RelayCommand(obj =>
            {
                if (SelectedRigIndex > -1)
                {
                    RigName = Rigs[SelectedRigIndex].Name;
                    RigIP = Rigs[SelectedRigIndex].IP;

                    if (Rigs[SelectedRigIndex].Type != null)
                        SelectedRigTypeString = Rigs[SelectedRigIndex].Type;
                    else
                        SelectedRigTypeIndex = -1;

                    OmgConnectButtonVisibility = SelectedRigTypeString == "OMineGuard" ? true : false;
                    HiveWorkerSettingsVisibility = SelectedRigTypeString == "HiveOS" ? true : false;
                    if (HiveWorkerSettingsVisibility)
                    {
                        if (Rigs[SelectedRigIndex].HiveFarmID != null 
                        && Rigs[SelectedRigIndex].HiveWorkerID != null)
                        {
                            HiveFarmID = Rigs[SelectedRigIndex].HiveFarmID;
                            HiveWorkerID = Rigs[SelectedRigIndex].HiveWorkerID;

                            for (int i = 0; i < HiveWorkers.Count; i++)
                            {
                                if (HiveWorkers[i].farm_id == HiveFarmID && HiveWorkers[i].id == HiveWorkerID)
                                {
                                    SelectedHiveWorkerIndex = i + 1; ;
                                    break;
                                }
                            }

                            HiveWorkerActive = true;
                        }
                        else
                        {
                            HiveWorkerActive = null;
                            SelectedHiveWorkerIndex = -1;
                        }
                    }

                    if (Rigs[SelectedRigIndex].eWeDevice != null)
                    {
                        SelectedeWeDevice = Rigs[SelectedRigIndex].eWeDevice;
                        eWeDeviceActive = true;
                    }
                    else
                        SelectedeWeDeviceIndex = -1;

                    CurrentStartOfRange = Rigs[SelectedRigIndex].MinTemp;
                    CurrentEndOfRange = Rigs[SelectedRigIndex].MaxTemp;
                    WachdogMinHashrate = Rigs[SelectedRigIndex].WachdogMinHashrate;
                    WachdogMaxTemp = Rigs[SelectedRigIndex].WachdogMaxTemp;
                    eWeDelayTimeout = Rigs[SelectedRigIndex].eWeDelayTimeout;

                    // wol
                }
                else
                {
                    RigName = null;
                    RigIP = null;
                    SelectedRigTypeString = null;
                }
                SetButtonsEnable();
            });

            RigMinus = new RelayCommand(obj => 
            {
                Rigs.RemoveAt(SelectedRigIndex);
                for (int i = SelectedRigIndex; i < RigsNames.Count(); i++) Rigs[i].Index--;
                RigsNames = from r in Rigs orderby r.Index select r.Name;
                _model.cmd_SaveRigs(Rigs.ToList());
                SetButtonsEnable();

                Watch.RemoveAt(Watch.Count - 1);

            });
            RigPlus = new RelayCommand(obj =>
            {
                Rigs.Add(new Settings.Rig(RigsNames.Count()));
                RigsNames = from r in Rigs orderby r.Index select r.Name;
                SelectedRigIndex = Rigs.Count - 1;
                _model.cmd_SaveRigs(Rigs.ToList());
                SetButtonsEnable();

                Watch.Add(false);
            });
            RigUp = new RelayCommand(obj =>
            {
                int i = SelectedRigIndex;

                for (int n = 0; i < RVMs.Count;)
                {
                    if (RVMs[n].Index == Rigs[i].Index)
                    {
                        RVMs[n].Index--;
                        RVs = (from r in RVs orderby r.Index select r).ToList();
                        RVMs = (from r in RVMs orderby r.Index select r).ToList();
                        break;
                    }
                }
                Rigs[i].Index--;

                for (int n = 0; i < RVMs.Count;)
                {
                    if (RVMs[n].Index == Rigs[i - 1].Index)
                    {
                        RVMs[n].Index++;
                        RVs = (from r in RVs orderby r.Index select r).ToList();
                        RVMs = (from r in RVMs orderby r.Index select r).ToList();
                        break;
                    }
                }
                Rigs[i - 1].Index++;

                Rigs = new ObservableCollection<Settings.Rig>(Rigs.OrderBy(r => r.Index));
                RigsNames = from r in Rigs orderby r.Index select r.Name;
                SelectedRigIndex = i - 1;
                _model.cmd_SaveRigs(Rigs.ToList());
                SetButtonsEnable();
            });
            RigDown = new RelayCommand(obj =>
            {
                int i = SelectedRigIndex;

                for (int n = 0; i < RVMs.Count;)
                {
                    if (RVMs[n].Index == Rigs[i].Index)
                    {
                        RVMs[n].Index++;
                        RVs = null;
                        RVs = (from r in RVs orderby r.Index select r).ToList();
                        RVMs = (from r in RVMs orderby r.Index select r).ToList();
                        break;
                    }
                }
                Rigs[i].Index++;

                for (int n = 0; i < RVMs.Count;)
                {
                    if (RVMs[n].Index == Rigs[i + 1].Index)
                    {
                        RVMs[n].Index--;
                        RVs = null;
                        RVs = (from r in RVs orderby r.Index select r).ToList();
                        RVMs = (from r in RVMs orderby r.Index select r).ToList();
                        break;
                    }
                }
                Rigs[i + 1].Index--;
                Rigs = new ObservableCollection<Settings.Rig>(Rigs.OrderBy(r => r.Index));
                RigsNames = from r in Rigs orderby r.Index select r.Name;
                SelectedRigIndex = i + 1;
                _model.cmd_SaveRigs(Rigs.ToList());
                SetButtonsEnable();
            });
            RigSave = new RelayCommand(obj =>
            {
                List<Settings.Rig> rigs = Rigs.ToList();

                int i = SelectedRigIndex;
                rigs[i].Name = RigName;
                rigs[i].IP = RigIP;
                if (SelectedRigTypeIndex > 0)
                    rigs[i].Type = SelectedRigTypeString;
                else rigs[i].Type = null;
                RigsNames = from r in rigs orderby r.Index select r.Name;
                SelectedRigIndex = i;
                _model.cmd_SaveRigs(rigs.ToList());

                Rigs = new ObservableCollection<Settings.Rig>(rigs);

                SetButtonsEnable();
            });
        }
        private void SetButtonsEnable()
        {
            MinusButtonEnable = (SelectedRigIndex > -1) ? true : false;
            UPButtonEnable = (SelectedRigIndex > 0) ? true : false;
            DownButtonEnable = (SelectedRigIndex < (RigsNames.Count() - 1)) ? true : false;
            SaveButtonEnable = (SelectedRigIndex > -1) ? true : false;
        }
        #endregion
        #region Indicators & Tumblers
        public List<RigStatus?> Indicators { get; set; } = new List<RigStatus?>();

        public ObservableCollection<bool> Watch { get; set; } = new ObservableCollection<bool>();
        public RelayCommand SetWach { get; set; }

        private void InitializeIndicatorsAndTumblers()
        {
            SetWach = new RelayCommand(obj => 
            {
                int i = (int)obj;
                if (i != Watch.Count)
                {
                    Rigs[i].Waching = Watch[i];
                    _model.cmd_SaveRigs(Rigs.ToList());
                    {
                        if (Watch[i])
                            AddRigPanel(i);
                        else
                            RemoveRigPanel(i);
                    }
                }
            });
        }
        #endregion
        #region CurrentRigSettings
        public IEnumerable<string> eWeDevicesNames { get; set; }
        public string SelectedeWeDevice { get; set; }
        public int SelectedeWeDeviceIndex { get; set; } = -1;
        public bool eWeDeviceActive { get; set; }
        public int? eWeDelayTimeout { get; set; }

        public RelayCommand SelecteWeDevice { get; set; }
        public RelayCommand SwitchActiveDevice { get; set; }
        public RelayCommand SeteWeDelayTimeout { get; set; }

        private void InitializeCurrentRigSettingsCommand()
        {
            SelecteWeDevice = new RelayCommand(obj => 
            {
                eWeDeviceActive = false;
                Rigs[SelectedRigIndex].eWeDevice = null;
                _model.cmd_SaveRigs(Rigs.ToList());
            });
            SwitchActiveDevice = new RelayCommand(obj => 
            {
                if (eWeDeviceActive)
                    Rigs[SelectedRigIndex].eWeDevice = SelectedeWeDevice;
                else
                    Rigs[SelectedRigIndex].eWeDevice = null;
                _model.cmd_SaveRigs(Rigs.ToList());
            });
            SeteWeDelayTimeout = new RelayCommand(obj => 
            {
                if (eWeDelayTimeout != null)
                {
                    Rigs[SelectedRigIndex].eWeDelayTimeout = eWeDelayTimeout.Value;
                }
                else
                {
                    Rigs[SelectedRigIndex].eWeDelayTimeout = null;
                }
                _model.cmd_SaveRigs(Rigs.ToList());
            });
        }
        #endregion
        #region OMGControl
        public bool OmgConnectButtonVisibility { get; set; } = false;

        public RelayCommand OMGconnect { get; set; }
        public RelayCommand OMGdisconnect { get; set; }

        private void InitializeOMGSwitchConnectCommand()
        {
            OMGconnect = new RelayCommand(obj => OMGcontroller.StartControl(RigIP));
            OMGdisconnect = new RelayCommand(obj => OMGcontroller.StopControl());
        }
        public UserControl OmgControlView { get; set; }
        #endregion
        #region HiveWorkerSettings
        public bool HiveWorkerSettingsVisibility { get; set; } = false;
        public bool HiveConnection { get; set; } = false;
        public string HiveConnectionState { get; set; }

        private List<Worker> HiveWorkers;
        public IEnumerable<string> HiveWorkersNames { get; set; }
        public int SelectedHiveWorkerIndex { get; set; }
        public bool? HiveWorkerActive { get; set; }
        public int? HiveFarmID { get; set; }
        public int? HiveWorkerID { get; set; }

        public RelayCommand SelectHiveWorker { get; set; }
        public RelayCommand HiveWorkerEnable { get; set; }
        public RelayCommand HiveWorkerDisable { get; set; }

        private void InitializeHiveWorkerCommands()
        {
            SelectHiveWorker = new RelayCommand(obj => 
            {
                int i = SelectedHiveWorkerIndex - 1;
                if (i > -1)
                {
                    HiveWorkerActive = false;
                    HiveFarmID = HiveWorkers[i].farm_id;
                    HiveWorkerID = HiveWorkers[i].id;
                }
                else
                {
                    HiveWorkerActive = null;
                }
            });
            HiveWorkerEnable = new RelayCommand(obj =>
            {
                Rigs[SelectedRigIndex].HiveFarmID = HiveFarmID;
                Rigs[SelectedRigIndex].HiveWorkerID = HiveWorkerID;
                _model.cmd_SaveRigs(Rigs.ToList());
            });
            HiveWorkerDisable = new RelayCommand(obj =>
            {
                Rigs[SelectedRigIndex].HiveFarmID = null;
                Rigs[SelectedRigIndex].HiveWorkerID = null;
                _model.cmd_SaveRigs(Rigs.ToList());
                if (!HiveConnection) HiveWorkerActive = null;
            });
        }
        #endregion
        #region WachdogSettings
        public double? WachdogMinHashrate { get; set; }
        public int? WachdogMaxTemp { get; set; }

        public RelayCommand SetWachdogMinHashrate { get; set; }
        public RelayCommand SetWachdogMaxTemp { get; set; }

        private void IniWachdogSettingsCommands()
        {
            SetWachdogMinHashrate = new RelayCommand(obj => 
            {
                if (WachdogMinHashrate != null)
                {
                    Rigs[SelectedRigIndex].WachdogMinHashrate = WachdogMinHashrate.Value;
                }
                else
                {
                    Rigs[SelectedRigIndex].WachdogMinHashrate = null;
                }
                _model.cmd_SaveRigs(Rigs.ToList());
            });
            SetWachdogMaxTemp = new RelayCommand(obj =>
            {
                if (WachdogMaxTemp != null)
                {
                    Rigs[SelectedRigIndex].WachdogMaxTemp = WachdogMaxTemp.Value;
                }
                else
                {
                    Rigs[SelectedRigIndex].WachdogMaxTemp = null;
                }
                _model.cmd_SaveRigs(Rigs.ToList());
            });
        }
        #endregion
        #region CurrentScale
        public int? CurrentStartOfRange { get; set; }
        public int? CurrentStartOfRangeD { get; set; }
        public int? CurrentEndOfRange { get; set; }
        public int? CurrentEndOfRangeD { get; set; }


        public RelayCommand ChangeCurrentStartOfRange { get; set; }
        public RelayCommand ChangeCurrentEndOfRange { get; set; }

        private void InitializeCurrentScaleCommand()
        {
            ChangeCurrentStartOfRange = new RelayCommand(obj =>
            {
                if (CurrentStartOfRange != null)
                {
                    CurrentStartOfRange = Convert.ToInt32(Math.Round((double)CurrentStartOfRange / 5) * 5);
                    if (CurrentEndOfRange <= CurrentStartOfRange + 10)
                    {
                        CurrentEndOfRange = CurrentStartOfRange + 10;
                    }
                    CurrentStartOfRangeD = CurrentStartOfRange;
                    CurrentEndOfRangeD = CurrentEndOfRange;

                    if (Rigs[SelectedRigIndex].MinTemp != CurrentStartOfRange)
                    {
                        SetCurrentMinTemp(SelectedRigIndex, CurrentStartOfRange.Value);
                        Rigs[SelectedRigIndex].MinTemp = CurrentStartOfRange;
                        _model.cmd_SaveRigs(Rigs.ToList());
                    }
                }
                else
                {
                    CurrentStartOfRangeD = null;
                }
                
            });
            ChangeCurrentEndOfRange = new RelayCommand(obj =>
            {
                if (CurrentEndOfRange != null)
                {
                    CurrentEndOfRange = Convert.ToInt32(Math.Round((double)CurrentEndOfRange / 5) * 5);
                    if (CurrentStartOfRange >= CurrentEndOfRange - 10)
                    {
                        CurrentStartOfRange = CurrentEndOfRange - 10;
                    }
                    CurrentStartOfRangeD = CurrentStartOfRange;
                    CurrentEndOfRangeD = CurrentEndOfRange;

                    if (Rigs[SelectedRigIndex].MaxTemp != CurrentEndOfRange)
                    {
                        SetCurrentMaxTemp(SelectedRigIndex, CurrentEndOfRange.Value);
                        Rigs[SelectedRigIndex].MaxTemp = CurrentEndOfRange;
                        _model.cmd_SaveRigs(Rigs.ToList());
                    }
                }
                else
                {
                    CurrentEndOfRangeD = null;
                }
            });
        }
        #endregion

        //Общие настройки
        #region BaseScale
        public int StartOfRange { get; set; }
        public int StartOfRangeD { get; set; }
        public int EndOfRange { get; set; }
        public int EndOfRangeD { get; set; }


        public RelayCommand ChangeStartOfRange { get; set; }
        public RelayCommand ChangeEndOfRange { get; set; }

        private void InitializeBaseScaleCommand()
        {
            ChangeStartOfRange = new RelayCommand(obj =>
            {
                StartOfRange = Convert.ToInt32(Math.Round((double)StartOfRange / 5) * 5);
                if (EndOfRange <= StartOfRange + 10)
                {
                    EndOfRange = StartOfRange + 10;
                }
                StartOfRangeD = StartOfRange;
                EndOfRangeD = EndOfRange;

                if (GenSettings.TotalMinTemp != StartOfRange)
                {
                    SetBaseMinTemp(StartOfRange);
                    GenSettings.TotalMinTemp = StartOfRange;
                    _model.cmd_SaveGenSettings(GenSettings); ;
                }
            });
            ChangeEndOfRange = new RelayCommand(obj =>
            {
                EndOfRange = Convert.ToInt32(Math.Round((double)EndOfRange / 5) * 5);
                if (StartOfRange >= EndOfRange - 10)
                {
                    StartOfRange = EndOfRange - 10;
                }
                StartOfRangeD = StartOfRange;
                EndOfRangeD = EndOfRange;

                if (GenSettings.TotalMaxTemp != EndOfRange)
                {
                    SetBaseMaxTemp(EndOfRange);
                    GenSettings.TotalMaxTemp = EndOfRange;
                    _model.cmd_SaveGenSettings(GenSettings); ;
                }
            });
        }
        #endregion
        #region Hive
        public string HiveLogin { get; set; }
        public string HivePasswordSend { get; set; }
        public string HivePasswordReceive;
        public string HiveAccountState { get; set; }
        
        public RelayCommand HiveConnect { get; set; }
        public RelayCommand HiveDisonnect { get; set; }

        private void InitializeHiveCommands()
        {
            HiveConnect = new RelayCommand(async obj =>
            {
                HiveAccountState = "Подключение";
                AuthenticationStatus st = await Task.Run(() =>
                    HiveClient.HiveAuthentication(HiveLogin, HivePasswordReceive));
                if (st.Status)
                {
                    await Task.Run(async () =>
                    {
                        GenSettings.HiveLogin = HiveLogin;
                        GenSettings.HivePassword = HivePasswordReceive;
                        _model.cmd_SaveGenSettings(GenSettings); ;
                        HiveAccountState = "Аккаунт подключен";
                        HiveConnection = true;
                        HiveConnectionState = "Подключение к Hive установлено";

                        HiveWorkers = await Task.Run(() => HiveClient.GetWorkers());

                        List<string> LST = (from w in HiveWorkers select w.name).ToList();
                        LST.Insert(0, "---");
                        HiveWorkersNames = LST;
                    });
                }
                else
                {
                    HiveAccountState = st.Message;
                    HiveConnection = false;
                    HiveConnectionState = "Подключение к Hive отсутствует";
                }
            });
            HiveDisonnect = new RelayCommand(obj =>
            {
                GenSettings.HiveLogin = null;
                GenSettings.HivePassword = null;
                _model.cmd_SaveGenSettings(GenSettings); ;
                HiveLogin = "";
                HivePasswordSend = "";
                HiveAccountState = "Аккаунт не подключен";
                HiveWorkersNames = null;
                HiveConnection = false;
                HiveConnectionState = "Подключение к Hive отсутствует";
            });
        }
        #endregion
        #region eWe
        public string eWeLogin { get; set; }
        public string eWePasswordSend { get; set; }
        public string eWePasswordReceive;
        public string eWeAccountState { get; set; }

        public RelayCommand eweConnect { get; set; }
        public RelayCommand eweDisonnect { get; set; }

        private void InitializeeWeCommands()
        {
            eweConnect = new RelayCommand(async obj => 
            {
                eWeAccountState = "Подключение";
                if (await Task.Run(() => eWeLinkClient.AutheWeLink(eWeLogin, eWePasswordReceive)))
                {
                    _ = Task.Run(async() =>
                    {
                        GenSettings.eWeLogin = eWeLogin;
                        GenSettings.eWePassword = eWePasswordReceive;
                        _model.cmd_SaveGenSettings(GenSettings);;
                        eWeAccountState = "Аккаунт подключен";

                        List<_eWelinkDevice> LE = await Task.Run(() => eWeLinkClient.GetDevices());
                        List<string> LST = (from x in LE select x.name).ToList();
                        LST.Insert(0, "---");
                        eWeDevicesNames = LST;
                    });
                }
                else
                {
                    eWeAccountState = "Ошибка";
                    await Task.Delay(2000);
                    eWeAccountState = "Аккаунт не подключен";
                }
            });
            eweDisonnect = new RelayCommand(obj =>
            {
                GenSettings.eWeLogin = null;
                GenSettings.eWePassword = null;
                eWeLinkClient.RemoveAuth();
                _model.cmd_SaveGenSettings(GenSettings);;
                eWeLogin = "";
                eWePasswordSend = "";
                eWeAccountState = "Аккаунт не подключен";
                eWeDevicesNames = null;
            });
        }
        #endregion
        
        #region Informers
        public int? VKuserID { get; set; }
        public RelayCommand VKuserIDchanged { get; set; }

        private void InitializeInformerCommands()
        {
            VKuserIDchanged = new RelayCommand(obj => 
            {
                GenSettings.VKuserID = VKuserID;
                _model.cmd_SaveGenSettings(GenSettings);
            });
        }
        #endregion
    }
}