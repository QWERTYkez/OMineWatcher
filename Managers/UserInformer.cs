using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OMineWatcher.Managers
{
    public static class UserInformer
    {
        public static event Action<bool> AlarmStatus;

        public static void SendMSG(string sender, string msg)
        {
            Debug.WriteLine($"{sender} >> {msg}");
            // vk informer
        }
        public static Task PayoutPlay()
        {
            return Task.Run(() => 
            {
                using (var sound = new SoundPlayer(Properties.Resources.payout))
                {
                    sound.PlaySync();
                }
            });
        }

        private static Thread Alarm;
        private static readonly object Key = new object();
        public static void AlarmStart()
        {
            lock (Key)
            {
                if (Alarm == null)
                {
                    Alarm = new Thread(() =>
                    {
                        while (App.Live)
                        {
                            using (var sound = new SoundPlayer(Properties.Resources.alarm))
                            {
                                sound.PlaySync();
                            }
                        }
                    });
                    Alarm.Start();
                    Task.Run(() => AlarmStatus?.Invoke(true));
                }
            }
        }
        public static void AlarmStop()
        {
            Alarm?.Abort(); Alarm = null;
            Task.Run(() => AlarmStatus?.Invoke(false));
        }
    }
}