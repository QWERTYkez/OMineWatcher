using OMineWatcher.Managers;
using System;
using System.Threading.Tasks;

namespace OMineWatcher.Rigs
{
    public abstract partial class Rig
    {
        private class OMineGuard : Rig
        {
            public OMineGuard(Settings.Rig Config) : base(Config) { }

            public override event Action<RigInform> InformReceived;

            private protected override int WachdogDelay => 10;
            private OMGinformer OMG;
            private void SetEvents()
            {
                OMG.StreamStart += () => Task.Run(() =>
                {
                    ScanningStop();
                    CurrentStatus = RigStatus.works;
                });
                OMG.StreamEnd += () => Task.Run(() =>
                {
                    OMG?.ClearEvents();
                    OMG = null;
                    CurrentStatus = RigStatus.offline;
                    ScanningStart();
                    InformReceived?.Invoke(new RigInform { RigInactive = true });
                    Waching = false;
                });
                OMG.SentInform += RO => 
                    InformReceived.Invoke(RO);
            }
            private protected override void WachingStert()
            {
                Task.Run(() =>
                {
                    OMG = new OMGinformer();
                    SetEvents();
                    OMG.StartInformStream(Config.IP);
                });
            }
            private protected override void WachingStop()
            {
                OMG?.StopInformStream();
                OMG = null;
                CurrentStatus = RigStatus.offline;
                ScanningStart();
                InformReceived?.Invoke(new RigInform { RigInactive = true });
                Waching = false;
            }
            private protected override void WachingReset()
            {
                OMG?.ClearEvents();
                OMG?.StopInformStream();
                if (OMG != null) SetEvents();
                OMG?.StartInformStream(Config.IP);
            }
        }
    }
}