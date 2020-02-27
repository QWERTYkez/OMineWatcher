using EwelinkNet;
using OMineWatcher.Managers;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace OMineWatcher.Rigs
{
    public abstract partial class Rig
    {
        //-------static
        private static bool InternetConnection => InternetConnectionWacher.InternetConnectedState;
        public static Rig GetRig(Settings.Rig Config)
        {
            if (Config == null) return null;
            switch (Config.Type)
            {
                case "OMineGuard": return new OMineGuard(Config);
                case "HiveOS": return new HiveOS(Config);
                default: return null;
            }
        }

        //-------const
        private const int PingCheckDelay = 3; //sec
        private const int PingCheckTimeout = 60 * 5; //msec

        //-------instance
        public virtual event Action<RigInform> InformReceived;
        public event Action<RigStatus> StatusChanged;

        private protected abstract int WachdogDelay { get; }
        private protected Rig(Settings.Rig Config)
        {
            this.Config = Config;
            Config.WachStop += () => 
                Task.Run(() => WachingStop());
            Config.IPChanged += () => 
                Task.Run(() => WachingReset());
        }
        public void BreakLinks()
        {
            InformReceived = null;
            StatusChanged = null;
            ScanningStop();
            WachingStop();
            Config = null;
        } 

        private bool Waching = false;
        private bool Scanning = false;
        private void ScanningStop() => Scanning = false;
        private IPStatus Status;
        private RigStatus currentStatus;
        private RigStatus CurrentStatus 
        { 
            set
            {
                if (currentStatus != value)
                {
                    currentStatus = value;
                    StatusChanged?.Invoke(value);
                }
            }
        }
        public void ScanningStart()
        {
            if (Scanning) return;
            Scanning = true;
            Task.Run(() =>
            {
                while (App.Live && Scanning)
                {
                    if (InternetConnection)
                    {
                        GetPing();
                        if (Status == IPStatus.Success)
                        {
                            CurrentStatus = RigStatus.online;
                            if (Config.Waching && !Waching)
                            {
                                Waching = true;
                                WachingStert();
                            } 
                        }
                        else
                        {
                            CurrentStatus = RigStatus.offline;
                            if (Config.Waching)
                                eWeLinkWachdog();
                        }
                    }
                    Thread.Sleep(PingCheckDelay * 1000);
                }
            });
        }
        private Settings.Rig Config;
        private void GetPing()
        {
            using (var ping = new Ping())
            {
                try { Status = ping.Send(IPAddress.Parse(Config.IP), PingCheckTimeout).Status; }
                catch { Status = IPStatus.Unknown; }
            }
        }

        private protected abstract void WachingStert();
        private protected abstract void WachingStop();
        private protected abstract void WachingReset();

        private bool Wachdog = false;
        private void eWeLinkWachdog()
        {
            if (Wachdog || eWeRebootiong) return;
            Wachdog = true;
            Task.Run(() =>
            {
                for (int i = 0; i < WachdogDelay; i++)
                {
                    if (Status == IPStatus.Success || !InternetConnection) goto EndWachdog;
                    Thread.Sleep(1000);
                }
                if (Status == IPStatus.Success || !InternetConnection) goto EndWachdog;
                UserInformer.SendMSG(Config.Name, "Ping: Worker offline");
                WachingStop();
                eWeReboot();
            EndWachdog:
                Wachdog = false;
            });
        }
        private bool eWeRebootiong = false;
        private void eWeReboot()
        {
            if (Config.eWeDevice != null && App.Ewelink != null)
            {
                if (eWeRebootiong) return;
                eWeRebootiong = true;
                while (App.Live && eWeRebootiong)
                {
                    Task.Run(() => 
                    {
                        UserInformer.SendMSG(Config.Name, "eWeReboot");

                        EwelinkNet.Classes.SwitchDevice dev = null;
                        try
                        {
                            dev = App.Ewelink.Devices.
                                Where(d => d.name == Config.eWeDevice).First()
                                as EwelinkNet.Classes.SwitchDevice;
                        }
                        catch 
                        { 
                            eWeRebootiong = false; 
                            UserInformer.SendMSG(Config.Name, "eWeReboot error");
                            return;
                        }

                        if (dev != null)
                        {
                            dev.TurnOff();
                            Thread.Sleep(5000);
                            dev.TurnOn();
                        }
                        else
                        {
                            eWeRebootiong = false; 
                            UserInformer.SendMSG(Config.Name, "eWeReboot error");
                            return;
                        }
                    });

                    for (int i = 0; i < Config.eWeDelayTimeout; i++)
                    {
                        Thread.Sleep(1000);
                        if (Status == IPStatus.Success || !InternetConnection)
                        {
                            eWeRebootiong = false; return;
                        }  
                    }
                }

                

                
            }
        }
    }
    public enum RigStatus
    {
        offline,
        online,
        works
    }
}