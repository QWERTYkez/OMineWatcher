﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
            public Rig(int index)
            {
                Name = "New";
                IP = null;
                Type = null;
                Waching = false;
                ID = DateTime.UtcNow.ToBinary();

                eWeDevice = null;

                Index = index;
            }

            public string Name;
            public string IP;
            public string Type;
            public bool Waching;
            public long ID { get; private set; }

            public string eWeDevice;

            public int Index;
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
            }

            public string eWeLogin;
            public string eWePassword;
            public int Digits;
            public int LogTextSize;
            public bool LogAutoscroll;
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
}