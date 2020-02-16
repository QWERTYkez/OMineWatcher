using Newtonsoft.Json;
using OMineWatcher.Pools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OMineWatcher.Managers
{
    public static class Settings
    {
        static Settings()
        {
            ReadSettings();
        }

        public static List<Rig> Rigs
        {
            get { return profile.Rigs; }
            set
            {
                profile.Rigs = value;
            }
        }
        public static List<PoolSet> Pools
        {
            get { return profile.Pools; }
            set
            {
                profile.Pools = value;
            }
        }
        public static _GenSettings GenSets
        {
            get { return profile.GenSets; }
            set
            {
                profile.GenSets = value;
            }
        }

        private static Profile profile;
        private class Profile
        {
            public List<Rig> Rigs { get; set; } = new List<Rig>();
            public List<PoolSet> Pools { get; set; } = new List<PoolSet>();
            public _GenSettings GenSets { get; set; } = new _GenSettings();
        }

        #region Rig
        public static Rig GetRig(long id)
        {
            foreach (Rig x in Rigs)
            {
                if (x.ID == id)
                {
                    return x;
                }
            }
            return null;
        }
        public class Rig
        {
            public event Action WachStart;
            public event Action WachStop;
            public event Action IPChanged;

            public Rig(int index)
            {
                Name = "New";
                IP = null;
                Type = null;
                Waching = false;
                ID = DateTime.UtcNow.ToBinary();

                eWeDevice = null;

                Index = index;
                MaxTemp = null;
                MinTemp = null;
            }

            private bool waching;
            public bool Waching
            {
                get => waching;
                set
                {
                    if (waching != value)
                    {
                        waching = value;
                        if (value) WachStart?.Invoke();
                        else WachStop?.Invoke();
                    }
                }
            }
            private string iP;
            public string IP 
            { 
                get => iP; 
                set
                {
                    if (iP != value)
                    {
                        iP = value;
                        IPChanged?.Invoke();
                    }
                } 
            }

            public string Name;
            public string Type;
            public long ID { get; private set; }

            public string eWeDevice;
            public int? eWeDelayTimeout;

            public int? HiveFarmID;
            public int? HiveWorkerID;

            public double? WachdogMinHashrate;
            public int? WachdogMaxTemp;

            public int Index;
            public int? MaxTemp;
            public int? MinTemp;
            
        }
        #endregion
        #region GenSettings
        public class _GenSettings
        {
            public _GenSettings()
            {
                Digits = 5;
                LogTextSize = 12;
                LogAutoscroll = true;

                TotalMaxTemp = 75;
                TotalMinTemp = 20;

                eWeDelayTimeout = 60; //sec
            }

            public string eWeLogin;
            public string eWePassword;
            public int eWeDelayTimeout;

            public string HiveLogin;
            public string HivePassword;

            public int Digits;
            public int LogTextSize;
            public bool LogAutoscroll;
            public int TotalMaxTemp;
            public int TotalMinTemp;
            public int? VKuserID;
        }
        #endregion

        #region FileStream Methods
        private static object key = new object();
        public static void SaveSettings()
        {
            Task.Run(() => 
            {
                lock (key)
                {
                    string JSON = JsonConvert.SerializeObject(profile);

                    using (FileStream fstream = new FileStream("Settings.json", FileMode.Create))
                    {
                        byte[] array = System.Text.Encoding.Default.GetBytes(JSON);
                        fstream.Write(array, 0, array.Length);
                    }
                }
            });
        }
        public static void ReadSettings()
        {
            lock (key)
            {
                try
                {
                    using (FileStream fstream = File.OpenRead("Settings.json"))
                    {
                        byte[] array = new byte[fstream.Length];
                        fstream.Read(array, 0, array.Length);
                        string json = System.Text.Encoding.Default.GetString(array);
                        profile = JsonConvert.DeserializeObject<Profile>(json);
                        return;
                    }
                }
                catch
                {
                    profile = new Profile();
                    return;
                }
            }
        }
        #endregion

        public static string[] RigTypes = new string[] { "---", "OMineGuard", "HiveOS" };
    }

    public class PoolSet
    {
        private string name;
        private PoolType? pool;
        private CoinType? coin;
        private string wallet;
        private bool wach = false;

        public event Action<string> NameChanged;
        public event Action<PoolType?> PoolChanged;
        public event Action<CoinType?> CoinChanged;
        public event Action<string> WalletChanged;
        public event Action<bool> WachChanged;

        public string Name { get => name; 
            set 
            {
                name = value;
                Task.Run(() => NameChanged?.Invoke(value));
            } 
        }
        public PoolType? Pool { get => pool;
            set 
            {
                pool = value;
                Task.Run(() => PoolChanged?.Invoke(value));
            }
        }
        public CoinType? Coin
        {
            get => coin;
            set
            {
                coin = value;
                Task.Run(() => CoinChanged?.Invoke(value));
            }
        }
        public string Wallet { get => wallet;
            set 
            {
                wallet = value;
                Task.Run(() => WalletChanged?.Invoke(value));
            } 
        }
        public bool Wach { get => wach;
            set
            {
                wach = value;
                Task.Run(() => WachChanged?.Invoke(value));
            }
        }
    }
}