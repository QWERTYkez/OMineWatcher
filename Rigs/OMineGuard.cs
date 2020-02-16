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

            private protected override int WachdogDelay => 30;
            private OMGinformer OMG;
            private void SetEvents()
            {
                OMG.StreamStart += () =>
                {
                    ScanningStop();
                    CurrentStatus = RigStatus.works;
                };
                OMG.StreamEnd += () =>
                {
                    ScanningStart();
                    InformReceived?.Invoke(new RigInform { RigInactive = true });
                    OMG = null;
                };
                OMG.SentInform += RO => InformReceived.Invoke(RO);
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
            private protected override void WachingStop() => OMG?.StopInformStream();
            private protected override void WachingReset()
            {
                OMG?.ClearEvents();
                OMG?.StopInformStream();
                SetEvents();
                OMG?.StartInformStream(Config.IP);
            }
        }
    }
}