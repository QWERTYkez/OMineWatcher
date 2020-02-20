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

        private static bool Alarm;
        private static readonly object Key = new object();
        private static List<object> Annunciators = new List<object>();
        public static void AlarmStart(object o)
        {
            if(!Annunciators.Contains(o))
            {
                Annunciators.Add(o);

                lock (Key)
                {
                    if (!Alarm)
                    {
                        Alarm = true;
                        Task.Run(() =>
                        {
                            while (App.Live && Alarm)
                            {
                                using (var sound = new SoundPlayer(Properties.Resources.alarm))
                                {
                                    sound.PlaySync();
                                }
                            }
                        });
                        Task.Run(() => AlarmStatus?.Invoke(true));
                    }
                }
            }
        }
        public static void AlarmStop(object o = null)
        {
            if (o != null)
            {
                Annunciators = Annunciators.Where(a => a != o).ToList();
                if (Annunciators.Count > 0) return;
            }
            else Annunciators = new List<object>();

            Alarm = false;
            Task.Run(() => AlarmStatus?.Invoke(false));
        }
    }
}