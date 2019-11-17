﻿using System.ComponentModel;
using OMineWatcher.Models;
using System.Windows.Controls;
using OMineWatcher.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eWeLink.API;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System;

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

            OMG_TCP.OMGcontrolReceived += () => OMGcontrolReceived();
            OMG_TCP.OMGcontrolLost += () => OMGcontrolLost();

            InitializeRigsControlCommands();
            InitializeIndicatorsAndTumblers();
            InitializeOMGSwitchConnectCommand();
            InitializeCurrentRigSettingsCommand();
            InitializeeWeCommand();
            InitializeBaseScaleCommand();
        }

        public ObservableCollection<Settings.Rig> Rigs { get; set; }

        private void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Rigs":
                    {
                        Rigs = new ObservableCollection<Settings.Rig>(_model.Rigs);
                        RigsNames = from r in Rigs orderby r.Index select r.Name;
                        for (int i = 0; i < RigsNames.Count(); i++)
                        {
                            Indicators.Add(MainModel.RigStatus.offline);
                            Watch.Add(Rigs[i].Waching);
                            {
                                if (Rigs[i].Waching)
                                    AddRigPanel(i);
                                else
                                    RemoveRigPanel(i);
                            }
                        }
                    }
                    break;
                case "RigInform":
                    {
                        RigInform();
                    }
                    break;
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

                    if (Rigs[SelectedRigIndex].eWeDevice != null)
                    {
                        SelectedeWeDevice = Rigs[SelectedRigIndex].eWeDevice;
                        eWeDeviceActive = true;
                    }
                    else
                        SelectedeWeDeviceIndex = -1;

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
        public List<MainModel.RigStatus> Indicators { get; set; } = new List<MainModel.RigStatus>();

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
                    if (!Rigs[i].Waching) _model.cmd_StopWach(Rigs[i].IP);
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

        public RelayCommand SelecteWeDevice { get; set; }
        public RelayCommand SwitchActiveDevice { get; set; }
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
        }
        #endregion
        #region OMGControl
        public bool OmgConnectButtonVisibility { get; set; } = false;

        public RelayCommand OMGconnect { get; set; }
        public RelayCommand OMGdisconnect { get; set; }

        private void InitializeOMGSwitchConnectCommand()
        {
            OMGconnect = new RelayCommand(obj => OMG_TCP.ConnectToOMG(RigIP));
            OMGdisconnect = new RelayCommand(obj => OMG_TCP.OMGcontrolDisconnect());
        }
        public UserControl OmgControlView { get; set; }
        private void OMGcontrolLost()
        {
            OmgControlView = null;
        }
        private void OMGcontrolReceived()
        {
            Views.MainWindow.STAContext.Send(obj => { OmgControlView = new Views.OmgView(); }, null);
        }
        #endregion

        //Общие настройки
        #region eWe
        public string eWeLogin { get; set; }
        public string eWePasswordSend { get; set; }
        public string eWePasswordReceive;
        public string eWeAccountState { get; set; }

        public RelayCommand eweConnect { get; set; }
        public RelayCommand eweDisonnect { get; set; }

        private async void InitializeeWeCommand()
        {
            eweConnect = new RelayCommand(async obj => 
            {
                eWeAccountState = "Подключение";
                if (await Task.Run(() => eWeLinkClient.AutheWeLink(eWeLogin, eWePasswordReceive)))
                {
                    Task.Run(async() =>
                    {
                        Settings.GenSets.eWeLogin = eWeLogin;
                        Settings.GenSets.eWePassword = eWePasswordReceive;
                        Settings.SaveSettings();
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
                Settings.GenSets.eWeLogin = null;
                Settings.GenSets.eWePassword = null;
                Settings.SaveSettings();
                eWeLogin = "";
                eWePasswordSend = "";
                eWeAccountState = "Аккаунт не подключен";
                eWeDevicesNames = null;
            });

            //InitializeeWeLink
            {
                if (Settings.GenSets.eWeLogin != "" && Settings.GenSets.eWeLogin != null)
                {
                    eWeLinkClient.SetAuth(Settings.GenSets.eWeLogin, Settings.GenSets.eWePassword);
                    eWeLogin = Settings.GenSets.eWeLogin;
                    eWePasswordSend = Settings.GenSets.eWePassword;
                    eWeAccountState = "Аккаунт подключен";
                    List<_eWelinkDevice> LE = await Task.Run(() => eWeLinkClient.GetDevices());
                    List<string> LST = (from x in LE select x.name).ToList();
                    LST.Insert(0, "---");
                    eWeDevicesNames = LST;
                }
            }
        }
        #endregion
        #region BaseScale
        public int StartOfRange { get; set; }
        public int StartOfRangeD { get; set; }
        public int EndOfRange { get; set; }
        public int EndOfRangeD { get; set; }


        public RelayCommand ChangeStartOfRange { get; set; }
        public RelayCommand ChangeEndOfRange { get; set; }

        private void InitializeBaseScaleCommand()
        {
            StartOfRange = Settings.GenSets.TotalMinTemp;
            StartOfRangeD = Settings.GenSets.TotalMinTemp;
            EndOfRange = Settings.GenSets.TotalMaxTemp;
            EndOfRangeD = Settings.GenSets.TotalMaxTemp;


            ChangeStartOfRange = new RelayCommand(obj => 
            {
                StartOfRange = Convert.ToInt32(Math.Round((double)StartOfRange / 5) * 5);
                if (EndOfRange <= StartOfRange + 10)
                {
                    EndOfRange = StartOfRange + 10;
                }
                StartOfRangeD = StartOfRange;
                EndOfRangeD = EndOfRange;

                if (Settings.GenSets.TotalMinTemp != StartOfRange)
                {
                    SetBaseMinTemp(StartOfRange);
                    Settings.GenSets.TotalMinTemp = StartOfRange;
                    Settings.SaveSettings();
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

                if (Settings.GenSets.TotalMaxTemp != EndOfRange)
                {
                    SetBaseMaxTemp(EndOfRange);
                    Settings.GenSets.TotalMaxTemp = EndOfRange;
                    Settings.SaveSettings();
                }
            });
        }
        #endregion
    }
}