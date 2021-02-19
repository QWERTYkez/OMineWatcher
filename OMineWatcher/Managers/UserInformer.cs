using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Net;
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
            string message = $"{sender} >> {msg}";
            Task.Run(() => SendVKMessage(message));
            Task.Run(() => SendTelegramMessage(message));
        }
        private static void SendVKMessage(string message)
        {
            if (Settings.GenSets.VKuserID != null)
            {
                string user_id = Settings.GenSets.VKuserID.ToString();
                string access_token = "6e8b089ad4fa647f95cdf89f4b14d183dc65954485efbfe97fe2ca6aa2f65b1934c80fccf4424d9788929";
                string ver = "5.73";

                string BaseReq = $"https://api.vk.com/method/messages.send" +
                    $"?user_id={user_id}" +
                    $"&message={message}" +
                    $"&access_token={access_token}" +
                    $"&v={ver}";

                WebRequest.Create(BaseReq).GetResponse();
            }
        }
        private static void SendTelegramMessage(string message)
        {

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

        public static bool Alarm { get; private set; } = false;
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