using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OMineWatcher.Managers
{
    class OMG_TCP
    {
        #region OMGcontrol
        public static event Action OMGcontrolLost;
        public static event Action OMGcontrolReceived;
        public static event Action<RootObject> OMGsent;

        public static bool OMGconnection;
        public static void OMGcontrolDisconnect()
        {
            OMGconnection = false;
        }
        private static TcpClient OMGcontrolClient;
        private static NetworkStream OMGcontrolStream;
        public static void ConnectToOMG(string IP)
        {
            OMGconnection = true;
            Task.Run(() =>
            {
                try
                {
                    OMGcontrolClient = new TcpClient(IP, 2112);
                }
                catch { OMGconnection = false; return; }

                if (OMGcontrolClient.Connected) OMGcontrolReceived?.Invoke();
                OMGcontrolStream = OMGcontrolClient.GetStream();

                try
                {
                    string[] result = new string[4];
                    if (OMGconnection)
                    {
                        result[0] = OMGreadMSG(OMGcontrolStream); // Profile
                        result[1] = OMGreadMSG(OMGcontrolStream); // Algoritms
                        result[2] = OMGreadMSG(OMGcontrolStream); // Miners
                        result[3] = OMGreadMSG(OMGcontrolStream); // Log

                        Task.Run(() =>
                        {
                            RootObject RO;
                            try
                            {
                                RO = JsonConvert.DeserializeObject<RootObject>(result[2]);
                                OMGsent?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = new RootObject();
                                RO.Algoritms = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(result[1]);
                                OMGsent?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<RootObject>(result[0]);
                                OMGsent?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<RootObject>(result[3]);
                                RO.Logging = RO.Logging.Replace("\r\n\r\n", "\r\n");
                                OMGsent?.Invoke(RO);
                            }
                            catch { }
                        });
                    }
                    else { OMGcontrolLost?.Invoke(); }
                }
                catch { }

                Task.Run(() =>
                {
                    try
                    {
                        using (TcpClient client = new TcpClient(IP, 2113))
                        {
                            using (NetworkStream stream = client.GetStream())
                            {
                                RootObject RO;
                                while (client.Connected && OMGconnection)
                                {
                                    try
                                    {
                                        RO = JsonConvert.DeserializeObject<RootObject>(OMGreadMSG(stream));
                                        OMGsent?.Invoke(RO);
                                    }
                                    catch { }
                                    Thread.Sleep(50);
                                }
                                OMGcontrolLost?.Invoke();
                            }
                        }
                    }
                    catch { OMGcontrolLost?.Invoke(); }
                });
            });
        }
        private static string OMGreadMSG(NetworkStream stream)
        {
            byte[] msg = new byte[4];
            stream.Read(msg, 0, msg.Length);
            int MSGlength = BitConverter.ToInt32(msg, 0);

            stream.Write(new byte[] { 1 }, 0, 1);

            msg = new byte[MSGlength];
            int count = stream.Read(msg, 0, msg.Length);
            return Encoding.Default.GetString(msg, 0, count);
        }
        private static object key = new object();

        // обратная связь
        public static void SendMSG(object body, MSGtype type)
        {
            Task.Run(() =>
            {
                if (OMGcontrolClient != null)
                {
                    if (OMGcontrolClient.Connected)
                    {
                        lock (key)
                        {
                            string header = "";
                            switch (type)
                            {
                                case MSGtype.Profile: header = "Profile"; break;
                                case MSGtype.RunConfig: header = "RunConfig"; break;
                                case MSGtype.ApplyClock: header = "ApplyClock"; break;
                                case MSGtype.StartProcess: header = "StartProcess"; break;
                                case MSGtype.KillProcess: header = "KillProcess"; break;
                                case MSGtype.ShowMinerLog: header = "ShowMinerLog"; break;
                            }

                            string msg = $"{{\"{header}\":{JsonConvert.SerializeObject(body)}}}";

                            byte[] Message = Encoding.Default.GetBytes(msg);
                            byte[] Header = BitConverter.GetBytes(Message.Length);

                            OMGcontrolStream.Write(Header, 0, Header.Length);

                            byte[] b = new byte[1];
                            OMGcontrolStream.Read(b, 0, b.Length);

                            OMGcontrolStream.Write(Message, 0, Message.Length);
                        }
                    }
                }
            });
        }
        public enum MSGtype
        {
            Profile,
            RunConfig,
            ApplyClock,
            StartProcess,
            KillProcess,
            ShowMinerLog
        }
        #endregion

        #region JSON classes
        public class RootObject
        {
            public Profile Profile { get; set; }
            public OC? Overclock { get; set; }
            public string Logging { get; set; }
            public double[] Hasrates { get; set; }
            public bool? Indication { get; set; }
            public List<string> Miners { get; set; }
            public Dictionary<string, int[]> Algoritms { get; set; }
            public string WachdogInfo { get; set; }
            public string LowHWachdog { get; set; }
            public string IdleWachdog { get; set; }
            public string ShowMLogTB { get; set; }
        }

        #region Profile
        public class Profile
        {
            public string RigName;
            public bool? Autostart;
            public long? StartedID;
            public string StartedProcess;
            public int? Digits;
            public bool[] GPUsSwitch;
            public List<Config> ConfigsList;
            public List<Overclock> ClocksList;
            public InformManager Informer;
            public double? LogTextSize;

            public int? TimeoutWachdog;
            public int? TimeoutIdle;
            public int? TimeoutLH;
        }
        public class Config
        {
            public string Name;
            public string Algoritm;
            public int? Miner;
            public string Pool;
            public string Port;
            public string Wallet;
            public string Params;
            public long? ClockID;
            public double MinHashrate;
            public long ID;
        }
        public class Overclock
        {
            public string Name;
            public int[] PowLim;
            public int[] CoreClock;
            public int[] MemoryClock;
            public int[] FanSpeed;
            public long ID;
        }
        public class InformManager
        {
            public bool VkInform;
            public string VKuserID;
        }
        #endregion
        public struct OC
        {
            public int[] MSI_PowerLimits;
            public int[] MSI_CoreClocks;
            public int[] MSI_MemoryClocks;
            public uint[] MSI_FanSpeeds;
            public float?[] OHM_Temperatures;
        }
        #endregion










        static void GetInfoStream()
        {
            TcpClient client;
            try
            {
                using (client = new TcpClient("127.0.0.1", 2112))
                {
                    int MessageLength = 15;
                    string header;
                    byte[] msg;
                    string body;
                    MinerInfo MI;

                    using (NetworkStream stream = client.GetStream())
                    {
                        Console.WriteLine("старт цикла");
                        while (true)
                        {
                            msg = new byte[MessageLength];     // готовим место для принятия сообщения
                            int count = stream.Read(msg, 0, msg.Length);   // читаем сообщение от клиента
                            string request = Encoding.Default.GetString(msg, 0, count);
                            if (15 == request.Length)
                            {
                                continue;
                            }
                            string[] req = null;
                            req = JsonConvert.DeserializeObject<string[]>(request);
                            header = req[0];
                            body = req[1];

                            switch (header)
                            {
                                case "js":
                                    {
                                        MessageLength = Convert.ToInt32(body);
                                    }
                                    break;
                                case "info":
                                    {
                                        MI = JsonConvert.DeserializeObject<MinerInfo>(body);

                                        Console.WriteLine($"{MI.TimeStamp.ToShortTimeString()} | {MI.AVGHashrates[0]} | " +
                                            $"{MI.AVGTemperatures[0]} | {MI.AVGFanspeeds[0]} | {MI.ShAccepted[0]}");
                                        MessageLength = 15;
                                    }
                                    break;
                            }
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch { }
        }

        public class MinerInfo
        {
            public DateTime TimeStamp;
            public double[] AVGHashrates;
            public double[] AVGTemperatures;
            public double[] AVGFanspeeds;
            public int[] ShAccepted;
            public int[] ShRejected;
            public int[] ShInvalid;
        }
    }
}
