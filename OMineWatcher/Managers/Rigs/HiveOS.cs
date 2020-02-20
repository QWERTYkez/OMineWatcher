using HiveOS.API;
using OMineWatcher.Managers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OMineWatcher.Rigs
{
    public abstract partial class Rig
    {
        private class HiveOS : Rig
        {
            public HiveOS(Settings.Rig Config) : base(Config) { }

            public override event Action<RigInform> InformReceived;

            private protected override int WachdogDelay => 15;

            private HiveOSWacher Wacher;
            private protected override void WachingStert()
            {
                if (Config.HiveFarmID.HasValue && Config.HiveWorkerID.HasValue)
                {
                    Wacher = new HiveOSWacher(this, Config);
                    Wacher.InformReceived += inf => Task.Run(() => this.InformReceived?.Invoke(inf));
                    Wacher.StartWach();
                }
            }
            private protected override void WachingStop()
            {
                Wacher?.RemoveLinks();
                Wacher = null;
            }
            private protected override void WachingReset()
            {
                Wacher?.RemoveLinks();
                Wacher = new HiveOSWacher(this, Config);
                Wacher.InformReceived += inf => Task.Run(() => this.InformReceived?.Invoke(inf));
                Wacher.StartWach();
            }

            private class HiveOSWacher
            {
                private const int HiveOSWachdogDelay = 150; //sec
                private const int HiveOSRequestDelay = 10; //sec

                public event Action<RigInform> InformReceived;

                private HiveOS Rig;
                private Settings.Rig Config;
                public HiveOSWacher(HiveOS Rig, Settings.Rig Config)
                {
                    this.Rig = Rig;
                    this.Config = Config;
                }
                public void RemoveLinks()
                {
                    InformReceived = null;
                    Rig = null; 
                    Config = null;
                    HiveWach = false;
                    HiveWachdog = false;
                }

                private bool HiveWach = false;
                private bool HiveWachdog = false;
                public void StartWach()
                {
                    if (!HiveWach)
                    {
                        HiveWach = true;

                        if (!HiveWachdog)
                        {
                            HiveWachdog = true;
                            Task.Run(() =>
                            {
                                Thread.Sleep(HiveOSWachdogDelay * 1000);
                                CurrentHiveStatus = HiveStatus.Normal;
                                UserInformer.SendMSG(Config.Name, "Start hive wach");
                            BackToLoop:
                                while (CurrentHiveStatus == HiveStatus.Normal && InternetConnection)
                                {
                                    Thread.Sleep(1000);
                                }
                                if (InternetConnection)
                                {
                                    //задержка
                                    for (int n = 0; n < 100; n++)
                                    {
                                        if (CurrentHiveStatus == HiveStatus.Normal) goto BackToLoop;
                                        Thread.Sleep(100);
                                    }

                                    string msg = "";
                                    switch (CurrentHiveStatus)
                                    {
                                        case HiveStatus.LowHashrate:
                                            {
                                                msg = "Hive: Low hashrate";
                                            }
                                            break;
                                        case HiveStatus.WorkerOffline:
                                            {
                                                msg = "Hive: Worker offline";
                                            }
                                            break;
                                    }
                                    UserInformer.SendMSG(Config.Name, msg);
                                    Rig.eWeReboot();
                                }
                                HiveWachdog = false;
                            });
                        }
                        Task.Run(() =>
                        {
                            while (HiveWach && InternetConnection)
                            {
                                var mi = HiveClient.GetWorkerInfo(Config.HiveFarmID.Value,
                                                                Config.HiveWorkerID.Value);

                                if (mi != null)
                                {
                                    InformReceived?.Invoke(new RigInform
                                    {
                                        Indication = true,
                                        InfHashrates = mi.Value.Hashrates,
                                        InfTemperatures = mi.Value.Temperatures
                                    });

                                    // low hashrate wachdog
                                    if (Config.WachdogMinHashrate != null)
                                    {
                                        if (mi.Value.Hashrates.Length > 0)
                                        {
                                            CurrentHiveStatus =
                                            mi.Value.Hashrates.Sum() < Config.WachdogMinHashrate.Value ?
                                            HiveStatus.LowHashrate : HiveStatus.Normal;
                                        }
                                        else
                                        {
                                            CurrentHiveStatus = HiveStatus.LowHashrate;
                                        }
                                    }
                                    else { CurrentHiveStatus = HiveStatus.Normal; }
                                }
                                else
                                {
                                    InformReceived?.Invoke(new RigInform { Indication = false });

                                    CurrentHiveStatus = HiveStatus.WorkerOffline;

                                    goto EndHiveWach;
                                }

                                Thread.Sleep(new TimeSpan(0, 0, HiveOSRequestDelay));
                            }
                        EndHiveWach:
                            HiveWach = false;
                        });
                    }
                }

                private HiveStatus CurrentHiveStatus;
                private enum HiveStatus
                {
                    Normal,
                    LowHashrate,
                    WorkerOffline
                }
            }
        }
    }
}
