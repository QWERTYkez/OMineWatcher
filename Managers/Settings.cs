using System;
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

        private static Profile profile;
        private class Profile
        {
            public Profile()
            {

            }

            public List<Rig> Rigs { get; set; } = new List<Rig>();
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
        public static void AddRig()
        {
            Rigs.Add(new Rig());
            SaveSettings();
        }
        public static void RemoveRig(int i)
        {
            Rigs.RemoveAt(i);
            SaveSettings();
        }
        public class Rig
        {
            public Rig()
            {
                Name = "New";
                IP = "";
                ID = DateTime.UtcNow.ToBinary();
                Waching = false;
            }

            public string Name;
            public string IP;
            public RigType? Type;
            public bool Waching;
            public long ID;
        }
        public enum RigType
        {
            OMineGuard, 
            HiveOS
        }
        #endregion

        #region FileStream Methods
        private static object key = new object();
        public static void SaveSettings()
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
    }
}