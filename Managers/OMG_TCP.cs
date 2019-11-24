using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OMineWatcher.Managers
{
    public class OMG_TCP
    {
        #region OMGcontrol
        public static event Action OMGcontrolLost;
        public static event Action OMGcontrolReceived;
        public static event Action<OMGRootObject> OMGsent;

        public static bool OMGconnection;
        public static void OMGcontrolDisconnect()
        {
            OMGconnection = false;
        }
        private static TcpClient OMGcontrolClient;
        private static NetworkStream OMGcontrolStream;
        public static void ConnectToOMG(string IP)
        {
            if (OMGconnection) return;
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
                    string[] result = new string[6];
                    if (OMGconnection)
                    {
                        result[0] = OMGreadMSG(OMGcontrolStream); // Profile
                        result[1] = OMGreadMSG(OMGcontrolStream); // Algoritms
                        result[2] = OMGreadMSG(OMGcontrolStream); // Miners
                        result[3] = OMGreadMSG(OMGcontrolStream); // DefClock
                        result[4] = OMGreadMSG(OMGcontrolStream); // Indication
                        result[5] = OMGreadMSG(OMGcontrolStream); // Log

                        Task.Run(() =>
                        {
                            OMGRootObject RO;
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[2]);
                                OMGsent?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = new OMGRootObject();
                                RO.Algoritms = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(result[1]);
                                OMGsent?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[0]);
                                OMGsent?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[3]);
                                OMGsent?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[4]);
                                OMGsent?.Invoke(RO);
                                OMGsent?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[5]);
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
                                OMGRootObject RO;
                                while (client.Connected && OMGconnection)
                                {
                                    try
                                    {
                                        RO = JsonConvert.DeserializeObject<OMGRootObject>(OMGreadMSG(stream));
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


        // обратная связь
        private static object key = new object();
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
        
        #endregion
    }

    #region OMG classes
    public class OMGRootObject
    {
        public Profile Profile { get; set; }
        public OC? Overclock { get; set; }
        public DC? DefClock { get; set; }
        public string Logging { get; set; }
        public double[] Hashrates { get; set; }
        public int[] Temperatures { get; set; }
        public bool? Indication { get; set; }
        public List<string> Miners { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }
        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }
        public string ShowMLogTB { get; set; }

        public bool? RigInactive;
    }

    #region Profile
    public class Profile
    {
        public string RigName;
        public bool Autostart;
        public long? StartedID;
        public string StartedProcess;
        public int? Digits;
        public List<bool> GPUsSwitch;
        public List<Config> ConfigsList;
        public List<Overclock> ClocksList;
        public InformManager Informer;
        public double? LogTextSize;

        public int TimeoutWachdog;
        public int TimeoutIdle;
        public int TimeoutLH;
    }
    public class Config
    {
        public Config()
        {
            Name = "Новый конфиг";
            Algoritm = "";
            Pool = "";
            Wallet = "";
            Params = "";
            MinHashrate = 0;
            ID = DateTime.UtcNow.ToBinary();
        }

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
        public Overclock()
        {
            Name = "Новый разгон";
            ID = DateTime.UtcNow.ToBinary();
        }

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
        public int[] MSI_FanSpeeds;
    }
    public struct DC
    {
        public int[] PowerLimits;
        public int[] CoreClocks;
        public int[] MemoryClocks;
        public int[] FanSpeeds;
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
    public abstract class OMGconnector
    {
        public static string OMGreadMSG(NetworkStream stream)
        {
            byte[] msg = new byte[4];
            stream.Read(msg, 0, msg.Length);
            int MSGlength = BitConverter.ToInt32(msg, 0);

            stream.Write(new byte[] { 1 }, 0, 1);

            msg = new byte[MSGlength];
            int count = stream.Read(msg, 0, msg.Length);
            return Encoding.Default.GetString(msg, 0, count);
        }
    }
    public class OMGinformer : OMGconnector
    {
        public event Action StreamEnd;
        public event Action StreamStart;
        public event Action<OMGRootObject> SentInform;
        public bool Streaming { get; private set; } = false;

        public void StartInformStream(string IP)
        {
            if (!Streaming)
            {
                Streaming = true;
                Task.Run(() =>
                {
                    try
                    {
                        using (TcpClient client = new TcpClient(IP, 2111))
                        {
                            if (client.Connected) StreamStart?.Invoke();
                            using (NetworkStream stream = client.GetStream())
                            {
                                OMGRootObject RO;
                                while (client.Connected && Streaming)
                                {
                                    try
                                    {
                                        RO = JsonConvert.DeserializeObject<OMGRootObject>(OMGreadMSG(stream));
                                        SentInform?.Invoke(RO);
                                    }
                                    catch { }
                                    Thread.Sleep(50);
                                }
                                StreamEnd?.Invoke();
                            }
                        }
                    }
                    catch { StreamEnd?.Invoke(); }
                });
            }
        }
        public void StopInformStream()
        {
            Streaming = false;
        }
    }
}
