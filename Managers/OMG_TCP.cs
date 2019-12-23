using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private static readonly object key = new object();
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
                                case MSGtype.SwitchProcess: header = "SwitchProcess"; break;
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
                                RO = new OMGRootObject
                                {
                                    Algoritms = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(result[1])
                                };
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
                                        Task.Run(() => SentInform?.Invoke(RO));
                                    }
                                    catch { }
                                    Thread.Sleep(50);
                                }
                                Task.Run(() => StreamEnd?.Invoke());
                                Task.Run(() => SentInform?.Invoke(new OMGRootObject { RigInactive = true }));
                            }
                        }
                    }
                    catch 
                    { 
                        Task.Run(() => StreamEnd?.Invoke());
                        Task.Run(() => SentInform?.Invoke(new OMGRootObject { RigInactive = true }));
                    }
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
        public DefClock DefClock { get; set; }
        public string Logging { get; set; }
        public bool? Indication { get; set; }
        public List<string> Miners { get; set; }
        public Dictionary<string, int[]> Algoritms { get; set; }
        public string WachdogInfo { get; set; }
        public string LowHWachdog { get; set; }
        public string IdleWachdog { get; set; }

        public int? GPUs { get; set; }
        public int?[] InfPowerLimits { get; set; }
        public int?[] InfCoreClocks { get; set; }
        public int?[] InfMemoryClocks { get; set; }
        public int?[] InfOHMCoreClocks { get; set; }
        public int?[] InfOHMMemoryClocks { get; set; }
        public int?[] InfFanSpeeds { get; set; }
        public int?[] InfTemperatures { get; set; }
        public double?[] InfHashrates { get; set; }
        public double? TotalHashrate { get; set; }

        public bool? RigInactive { get; set; }
    }

    #region Profile
    public class Profile
    {
        public string RigName;
        public bool Autostart;
        public long? StartedID;
        public string StartedProcess;
        public int Digits;
        public List<bool> GPUsSwitch;
        public List<Config> ConfigsList;
        public List<Overclock> ClocksList;
        public InformManager Informer;
        public int LogTextSize;

        public int TimeoutWachdog;
        public int TimeoutIdle;
        public int TimeoutLH;
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
        public double? MinHashrate;
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
    public class DefClock
    {
        public int[] PowerLimits;
        public int[] CoreClocks;
        public int[] MemoryClocks;
        public int[] FanSpeeds;
    }
    public struct MSIinfo
    {
        public int?[] PowerLimits;
        public int?[] CoreClocks;
        public int?[] MemoryClocks;
        public int?[] FanSpeeds;
    }
    public struct OHMinfo
    {
        public int?[] Temperatures;
        public int?[] FanSpeeds;
        public int?[] CoreClocks;
        public int?[] MemoryClocks;
    }
    public struct MinerInfo
    {
        public DateTime TimeStamp { get; set; }
        public double?[] Hashrates;
        public int?[] Temperatures;
        public int?[] Fanspeeds;
        public int?[] ShAccepted;
        public int? ShTotalAccepted;
        public int?[] ShRejected;
        public int? ShTotalRejected;
        public int?[] ShInvalid;
        public int? ShTotalInvalid;
    }
    public enum MSGtype
    {
        Profile,
        RunConfig,
        ApplyClock,
        SwitchProcess,
        ShowMinerLog
    }
    #endregion
}
