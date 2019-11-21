using eWeLink.API;
using HiveOS.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace OMineWatcher.Managers
{
    public static class RigsWacher
    {
        private const int PingCheckDelay = 3; //sec
        private const int PingCheckTimeout = 60 * 5; //msec
        private const int HiveOSWachdogDelay = 150; //sec
        private const int HiveOSRequestDelay = 10; //sec
        private const int WachdogDelay = 15; //sec

        public static event Action<RigInform> SendInform;
        public static event Action<int, RigStatus> RigStatusChanged;

        static RigsWacher()
        {
            InternetConnectionWacher.InternetConnectionLost += () => { InternetConnection = false; };
            InternetConnectionWacher.InternetConnectionRestored += () => { InternetConnection = true; };
            OMG_TCP.OMGInformSent += OMGInformSent;
            OMG_TCP.OMGInformStreamReceived += OMGRigWorks;
            OMG_TCP.OMGInformStreamLost += OMGRigInactive;
            RigStatusChanged += ChangeStatus;
            foreach (Settings.Rig r in Settings.Rigs) AddToAllStatuses();
            StartRigsScanning();
        }

        private static void OMGRigWorks(string IP)
        {
            List<string> IPs = (from r in Settings.Rigs select r.IP).ToList();
            if (IPs.Contains(IP))
            {
                int n = IPs.IndexOf(IP);
                RigStatusChanged?.Invoke(n, RigStatus.works);
                PingBlocks[n] = true;
            }
        }
        private static void OMGRigInactive(string IP)
        {
            List<string> IPs = (from r in Settings.Rigs select r.IP).ToList();
            if (IPs.Contains(IP))
            {
                int n = IPs.IndexOf(IP);
                PingBlocks[n] = false;

                OMG_TCP.RootObject ro = new OMG_TCP.RootObject();

                ro.RigInactive = true;

                OMGInformSent(IP, ro);
            }
        }
        private static void OMGInformSent(string IP, OMG_TCP.RootObject RO)
        {
            List<string> IPs = (from r in Settings.Rigs select r.IP).ToList();
            if (!IPs.Contains(IP))
            {
                OMG_TCP.StopInformStream(IP);
                return;
            }
            else
            {
                SendInform?.Invoke(new RigInform(IP, RO));
            }
        }

        private static void AddToAllStatuses()
        {
            Statuses.Add(RigStatus.offline);
            HiveWach.Add(false);
            WachdogStatuses.Add(false);
            PingBlocks.Add(false);
            HiveStatuses.Add(HiveStatus.WorkerOffline);
            HiveWachdogs.Add(false);
        }
        private static void RemoveInAllStatuses()
        {
            Statuses.RemoveAt(Statuses.Count - 1);
            HiveWach.RemoveAt(HiveWach.Count - 1);
            WachdogStatuses.RemoveAt(WachdogStatuses.Count - 1);
            PingBlocks.RemoveAt(PingBlocks.Count - 1);
            HiveStatuses.RemoveAt(HiveStatuses.Count - 1);
            HiveWachdogs.RemoveAt(HiveWachdogs.Count - 1);
        }
        private static int Scanned = 0;
        private static bool InternetConnection = InternetConnectionWacher.InternetConnectedState;
        private static void StartRigsScanning()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    while (Settings.Rigs.Count != Statuses.Count)
                    {
                        if (Settings.Rigs.Count > Statuses.Count)
                            AddToAllStatuses();
                        else
                            RemoveInAllStatuses();
                    }

                    if (Statuses.Count > Scanned)
                    {
                        Scanned++;
                        ScanningCurrentRig(Scanned);
                    }
                    await Task.Delay(100);
                }
            });
        }

        private static List<RigStatus> Statuses = new List<RigStatus>();
        private static void ChangeStatus(int i, RigStatus status)
        {
            Statuses[i] = status;
        }
        private static List<bool> PingBlocks = new List<bool>();
        private static void ScanningCurrentRig(int n)
        {
            Task.Run(async () =>
            {
                int i = n - 1;
                Ping ping = new Ping();
                IPStatus? status;

                string IP = Settings.Rigs[i].IP;
                string type = Settings.Rigs[i].Type;

                while (true)
                {
                    if (Statuses.Count < n) goto endWach;
                    if (type != Settings.Rigs[i].Type)
                    {
                        if (type == "HiveOS")
                        {
                            HiveWach[i] = false;
                            WachdogStatuses[i] = false;
                        }
                        if (type == "OMineGuard")
                        {
                            OMG_TCP.StopInformStream(Settings.Rigs[i].IP);
                            PingBlocks[i] = false;
                        }
                        type = Settings.Rigs[i].Type;
                    }
                    if (IP != Settings.Rigs[i].IP)
                    {
                        PingBlocks[i] = false;
                    }
                    if (!PingBlocks[i] && InternetConnection)
                    {
                        status = null;
                        IP = Settings.Rigs[i].IP;
                        try { status = ping.Send(IPAddress.Parse(IP), PingCheckTimeout).Status; }
                        catch { }

                        if (status == IPStatus.Success)
                        {
                            WachdogStatuses[i] = false;

                            switch (Settings.Rigs[i].Type)
                            {
                                case "OMineGuard":
                                    {
                                        RigStatusChanged?.Invoke(i, RigStatus.online);
                                        if (Settings.Rigs[i].Waching)
                                        {
                                            OMG_TCP.StartInformStream(IP);
                                        }
                                    }
                                    break;
                                case "HiveOS":
                                    {
                                        RigStatusChanged?.Invoke(i, HiveStatuses[i] == HiveStatus.Normal ? RigStatus.works : RigStatus.online);
                                        if (Settings.Rigs[i].Waching)
                                        {
                                            HiveStartWach(i);
                                        }
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            RigStatusChanged?.Invoke(i, RigStatus.offline);
                            if (Settings.Rigs[i].Waching)
                            {
                                ActivateWachdog(i);
                            }
                        }
                    }
                    await Task.Delay(PingCheckDelay * 1000);
                }
            endWach:
                Scanned--;
            });
        }

        #region HiveOS
        private static List<bool> HiveWach = new List<bool>();
        private static void HiveStartWach(int i)
        {
            if (!HiveWach[i] && Settings.Rigs[i].HiveFarmID != null)
            {
                HiveWach[i] = true;
                HiveWachdog(i);
                Task.Run(async () =>
                {
                    while (HiveWach[i] && InternetConnection)
                    {
                        MinerInfo? mi = HiveClient.GetWorkerInfo((int)Settings.Rigs[i].HiveFarmID,
                                                        (int)Settings.Rigs[i].HiveWorkerID);

                        if (mi != null)
                        {
                            SendInform?.Invoke(new RigInform(Settings.Rigs[i].IP,
                                new OMG_TCP.RootObject
                                {
                                    Indication = true,
                                    Hashrates = mi.Value.Hashrates,
                                    Temperatures = mi.Value.Temperatures
                                }));

                            // low hashrate wachdog
                            if (Settings.Rigs[i].WachdogMinHashrate != null)
                            {
                                if (mi.Value.Hashrates.Length > 0)
                                {
                                    HiveStatuses[i] =
                                    mi.Value.Hashrates.Sum() < Settings.Rigs[i].WachdogMinHashrate.Value ?
                                    HiveStatus.LowHashrate : HiveStatus.Normal;
                                }
                                else
                                {
                                    HiveStatuses[i] = HiveStatus.LowHashrate;
                                }
                            }
                            else { HiveStatuses[i] = HiveStatus.Normal; }
                        }
                        else
                        {
                            SendInform?.Invoke(new RigInform(Settings.Rigs[i].IP,
                                new OMG_TCP.RootObject
                                {
                                    Indication = false
                                }));

                            HiveStatuses[i] = HiveStatus.WorkerOffline;

                            goto EndHiveWach;
                        }

                        await Task.Delay(new TimeSpan(0, 0, HiveOSRequestDelay));
                    }
            EndHiveWach:
                    HiveWach[i] = false;
                });
            }
        }

        // HiveWachdog
        private static List<HiveStatus> HiveStatuses = new List<HiveStatus>();
        private enum HiveStatus
        {
            Normal,
            LowHashrate,
            WorkerOffline
        }
        private static List<bool> HiveWachdogs = new List<bool>();
        private static void HiveWachdog(int i)
        {
            if (HiveWachdogs[i]) return;
            Task.Run(async () =>
            {
                HiveWachdogs[i] = true;
                await Task.Delay(new TimeSpan(0, 0, HiveOSWachdogDelay));
                HiveStatuses[i] = HiveStatus.Normal;
                UserInformer.SendMSG(Settings.Rigs[i].Name, "Start hive wach");
            BackToLoop:
                while (HiveStatuses[i] == HiveStatus.Normal && InternetConnection)
                {
                    await Task.Delay(1000);
                }
                if (InternetConnection)
                {
                    //задержка
                    for (int n = 0; n < 100; n++)
                    {
                        if (HiveStatuses[i] == HiveStatus.Normal) goto BackToLoop;
                        await Task.Delay(100);
                    }

                    string msg = "";
                    switch (HiveStatuses[i])
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
                    UserInformer.SendMSG(Settings.Rigs[i].Name, msg);
                    eWeReboot(i);
                }
                HiveWachdogs[i] = false;
            });
        }
        #endregion

        #region eWeWachdog
        private static List<bool> WachdogStatuses = new List<bool>();
        private static void ActivateWachdog(int i)
        {
            if (!WachdogStatuses[i])
            {
                WachdogStatuses[i] = true;
                Task.Run(async () =>
                {
                    int n = 0;
                    while (n < WachdogDelay)
                    {
                        if (WachdogStatuses[i] && InternetConnection)
                        {
                            await Task.Delay(1000);
                            n++;
                        }
                        else
                        {
                            return;
                        }
                    }
                    if (WachdogStatuses[i] && InternetConnection)
                    {
                        UserInformer.SendMSG(Settings.Rigs[i].Name, "Ping: Worker offline");
                        EndWach(i);

                        while (WachdogStatuses[i] && InternetConnection)
                        {
                            eWeReboot(i);

                            await Task.Delay(1000 *
                                (Settings.Rigs[i].eWeDelayTimeout != null ?
                                Settings.Rigs[i].eWeDelayTimeout.Value :
                                Settings.GenSets.eWeDelayTimeout));
                        }
                    }
                });
            }
        }
        private static void EndWach(int i)
        {
            HiveWach[i] = false;
        }

        private static void eWeReboot(int i)
        {
            if (Settings.Rigs[i].eWeDevice != null && eWeLinkClient.ItCanAct)
            {
                UserInformer.SendMSG(Settings.Rigs[i].Name, "eWeReboot");
                eWeLinkClient.RebootDevice(Settings.Rigs[i].eWeDevice);
            }
        }
        #endregion
    }
    public struct RigInform
    {
        public RigInform(string ip, OMG_TCP.RootObject ro)
        {
            IP = ip;
            RO = ro;
        }

        public string IP;
        public OMG_TCP.RootObject RO;
    }
    public enum RigStatus
    {
        offline,
        online,
        works
    }
}