using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OMineWatcher.Managers;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Collections.ObjectModel;
using System.Threading;

namespace OMineWatcher.Models
{
    public class MainModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public MainModel()
        {
            Statuses = new ObservableCollection<RigStatus>(from r in Settings.Rigs select RigStatus.offline);
            OMG_TCP.OMGInformSent += OMGInformSent;
            OMG_TCP.OMGInformStreamReceived += RigWorks;
            OMG_TCP.OMGInformStreamLost += RigInactive;
        }
        public void InitializeModel()
        {
            Rigs = Settings.Rigs;
            StartRigsScanning();
        }
        private void RigWorks(string IP)
        {
            List<string> IPs = (from r in Settings.Rigs select r.IP).ToList();
            if (IPs.Contains(IP))
            {
                int n = IPs.IndexOf(IP);
                Statuses[n] = RigStatus.works;
            }
        }
        private void RigInactive(string IP)
        {
            List<string> IPs = (from r in Settings.Rigs select r.IP).ToList();
            if (IPs.Contains(IP))
            {
                int n = IPs.IndexOf(IP);
                Statuses[n] = RigStatus.online;
            }
        }
        
        public List<Settings.Rig> Rigs { get; set; }

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

        public enum RigStatus
        {
            offline,
            online,
            works
        }
        public ObservableCollection<RigStatus> Statuses { get; set; }
        private int Scanned = 0;
        private const int PingCheckDelay = 3; //sec
        private const int PingCheckTimeout = 300; //msec
        private void StartRigsScanning()
        {
            Task.Run(() => 
            {
                while (true)
                {
                    if (Statuses.Count > Scanned)
                    {
                        Scanned++;
                        int n = Scanned;
                        int i = Scanned - 1;
                        string IP = Rigs[i].IP;
                        Task.Run(() =>
                        {
                            Ping ping = new Ping();
                            IPStatus? status;
                            while (true)
                            {
                                if (Statuses.Count < n) { Scanned--; return; }
                                if (Statuses[i] != RigStatus.works || IP != Rigs[i].IP)
                                {
                                    status = null;
                                    IP = Rigs[i].IP;

                                    try { status = ping.Send(IPAddress.Parse(IP), PingCheckTimeout).Status; }
                                    catch { }
                                    if (status == IPStatus.Success)
                                    {
                                        if (Statuses.Count < n) { Scanned--; return; }
                                        Statuses[i] = RigStatus.online;
                                        if (Settings.Rigs[i].Waching)
                                        {
                                            switch (Rigs[i].Type)
                                            {
                                                case "OMineGuard":
                                                    {
                                                        OMG_TCP.StartInformStream(IP);
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (Statuses.Count < n) { Scanned--; return; }
                                        Statuses[i] = RigStatus.offline;
                                    }
                                }
                                Thread.Sleep(PingCheckDelay * 1000);
                            }
                        });
                    }
                }
            });
        }

        private void OMGInformSent(string IP, OMG_TCP.RootObject RO)
        {
            List<string> IPs = (from r in Settings.Rigs select r.IP).ToList();
            if (!IPs.Contains(IP))
            {
                OMG_TCP.StopInformStream(IP);
                return;
            }
            //Settings.Rigs[IPs.IndexOf(IP)]


            if (RO.Indication != null)
            {
                Debug.WriteLine($"{IP} -> {RO.Indication.ToString()}");
            }
            if (RO.Hashrates != null)
            {
                Debug.WriteLine($"{IP} -> {RO.Hashrates.ToString()}");
            }
            if (RO.Temperatures != null)
            {
                string str = "";
                foreach (int i in RO.Temperatures) str += $"{i} ";
                Debug.WriteLine($"{IP} -> {str}");
            }
        }
    }
}
