using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OMineWatcher.Managers
{
    public abstract class OMGconnector
    {
        protected private static string ReadMessage(NetworkStream stream)
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
        protected private static void SendMessage(TcpClient client, NetworkStream stream, object body, MSGtype type)
        {
            Task.Run(() =>
            {
                if (client != null)
                {
                    if (client.Connected)
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

                            stream.Write(Header, 0, Header.Length);

                            byte[] b = new byte[1];
                            stream.Read(b, 0, b.Length);

                            stream.Write(Message, 0, Message.Length);
                        }
                    }
                }
            });
        }
    }
    public class OMGcontroller : OMGconnector
    {
        public static event Action ControlEnd;
        public static event Action ControlStart;
        public static event Action<OMGRootObject> SentInform;
        public static bool OMGconnection { get; private set; } = false;

        private static TcpClient OMGcontrolClient;
        private static NetworkStream OMGcontrolStream;
        public static void StartControl(string IP)
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

                if (OMGcontrolClient.Connected) ControlStart?.Invoke();
                OMGcontrolStream = OMGcontrolClient.GetStream();

                try
                {
                    string[] result = new string[6];
                    if (OMGconnection)
                    {
                        result[0] = ReadMessage(OMGcontrolStream); // Profile
                        result[1] = ReadMessage(OMGcontrolStream); // Algoritms
                        result[2] = ReadMessage(OMGcontrolStream); // Miners
                        result[3] = ReadMessage(OMGcontrolStream); // DefClock
                        result[4] = ReadMessage(OMGcontrolStream); // Indication
                        result[5] = ReadMessage(OMGcontrolStream); // Log

                        Task.Run(() =>
                        {
                            OMGRootObject RO;
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[2]);
                                SentInform?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = new OMGRootObject();
                                RO.Algoritms = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(result[1]);
                                SentInform?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[0]);
                                SentInform?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[3]);
                                SentInform?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[4]);
                                SentInform?.Invoke(RO);
                                SentInform?.Invoke(RO);
                            }
                            catch { }
                            try
                            {
                                RO = JsonConvert.DeserializeObject<OMGRootObject>(result[5]);
                                RO.Logging = RO.Logging.Replace("\r\n\r\n", "\r\n");
                                SentInform?.Invoke(RO);
                            }
                            catch { }
                        });
                    }
                    else { ControlEnd?.Invoke(); }
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
                                        RO = JsonConvert.DeserializeObject<OMGRootObject>(ReadMessage(stream));
                                        SentInform?.Invoke(RO);
                                    }
                                    catch { }
                                    Thread.Sleep(50);
                                }
                                ControlEnd?.Invoke();
                            }
                        }
                    }
                    catch { ControlEnd?.Invoke(); }
                });
            });
        }
        public static void SendSetting(object body, MSGtype type)
        {
            SendMessage(OMGcontrolClient, OMGcontrolStream, body, type);
        }
        public static void StopControl()
        {
            OMGconnection = false;
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
                                        RO = JsonConvert.DeserializeObject<OMGRootObject>(ReadMessage(stream));
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
    #region OMG support classes
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
}
