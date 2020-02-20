﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OMineGuardControlLibrary;
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
        private class ConfigConverter : CustomCreationConverter<IConfig>
        {
            public override IConfig Create(Type objectType)
            {
                return new Config();
            }
        }
        private class OverclockConverter : CustomCreationConverter<IOverclock>
        {
            public override IOverclock Create(Type objectType)
            {
                return new Overclock();
            }
        }
        private static readonly JsonConverter[] Convs = new JsonConverter[]
        {
            new ConfigConverter(),
            new OverclockConverter()
        };

        private static readonly object readkey = new object();
        protected private RigInform ReadRootObject(NetworkStream stream)
        {
            //lock (readkey)
            {
                byte[] msg = new byte[4];
                stream.Read(msg, 0, msg.Length);
                int MSGlength = BitConverter.ToInt32(msg, 0);

                stream.Write(new byte[] { 1 }, 0, 1);

                msg = new byte[MSGlength];
                int count = stream.Read(msg, 0, msg.Length);
                string message = Encoding.Default.GetString(msg, 0, count);

                return JsonConvert.DeserializeObject<RigInform>(message, Convs);
            }
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
        public event Action ControlEnd;
        public event Action ControlStart;
        public event Action<RigInform> SentInform;

        private TcpClient OMGcontrolClient;
        private NetworkStream OMGcontrolStream;
        public void StartControl(string IP)
        {
            Task.Run(() =>
            {
                try
                {
                    OMGcontrolClient = new TcpClient(IP, 2112);
                }
                catch { return; }

                if (OMGcontrolClient.Connected) ControlStart?.Invoke();
                OMGcontrolStream = OMGcontrolClient.GetStream();

                try
                {
                    var RO = ReadRootObject(OMGcontrolStream);
                    Task.Run(() => SentInform?.Invoke(RO));
                }
                catch { ControlEnd?.Invoke(); }

                try
                {
                    using (ControlClient = new TcpClient(IP, 2113))
                    {
                        using (NetworkStream stream = ControlClient.GetStream())
                        {
                            RigInform RO;
                            while (ControlClient.Connected)
                            {
                                RO = ReadRootObject(stream);
                                Task.Run(() => SentInform?.Invoke(RO));
                            }
                            ControlEnd?.Invoke();
                        }
                    }
                }
                catch { ControlEnd?.Invoke(); }
            });
        }
        public void SendSetting(object body, MSGtype type)
        {
            SendMessage(OMGcontrolClient, OMGcontrolStream, body, type);
        }
        private TcpClient ControlClient;
        public void StopControl()
        {
            if (OMGcontrolClient != null)
            {
                OMGcontrolClient.Close();
                OMGcontrolClient.Dispose();
                OMGcontrolClient = null;
            }
            if (ControlClient != null)
            {
                ControlClient.Close();
                ControlClient.Dispose();
                ControlClient = null;
            }
        }
    }
    public class OMGinformer : OMGconnector
    {
        public event Action StreamEnd;
        public event Action StreamStart;
        public event Action<RigInform> SentInform;
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
                                RigInform RO;
                                while (client.Connected && Streaming)
                                {
                                    RO = ReadRootObject(stream);
                                    Task.Run(() => SentInform?.Invoke(RO));
                                    Thread.Sleep(50);
                                }
                                Task.Run(() => StreamEnd?.Invoke());
                                Task.Run(() => SentInform?.Invoke(new RigInform { RigInactive = true }));
                            }
                        }
                    }
                    catch 
                    { 
                        Task.Run(() => StreamEnd?.Invoke());
                        Task.Run(() => SentInform?.Invoke(new RigInform { RigInactive = true }));
                    }
                });
            }
        }
        public void StopInformStream()
        {
            Streaming = false;
        }
        public void ClearEvents()
        {
            StreamStart = null;
            StreamEnd = null;
            SentInform = null;
        }
    }
    #region OMG support classes
    public class RigInform
    {
        public RigInform ControlStruct { get; set; }

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
    public class Profile : IProfile
    {
        public string RigName { get; set; }
        public bool Autostart { get; set; }
        public long? StartedID { get; set; }
        public string StartedProcess { get; set; }
        public int Digits { get; set; }
        public List<bool> GPUsSwitch { get; set; } = new List<bool>();

        public List<IConfig> ConfigsList { get; set; } = new List<IConfig>();
        public List<IOverclock> ClocksList { get; set; } = new List<IOverclock>();
        public int LogTextSize { get; set; }

        public bool VkInform { get; set; }
        public string VKuserID { get; set; }

        public int TimeoutWachdog { get; set; }
        public int TimeoutIdle { get; set; }
        public int TimeoutLH { get; set; }
    }
    public class Config : IConfig
    {
        public string Name { get; set; }
        public string Algoritm { get; set; }
        public int? Miner { get; set; }
        public string Pool { get; set; }
        public string Port { get; set; }
        public string Wallet { get; set; }
        public string Params { get; set; }
        public long? ClockID { get; set; }
        public double MinHashrate { get; set; }
        public long ID { get; set; }
    }
    public class Overclock : IOverclock
    {
        public string Name { get; set; }
        public int[] PowLim { get; set; }
        public int[] CoreClock { get; set; }
        public int[] MemoryClock { get; set; }
        public int[] FanSpeed { get; set; }
        public long ID { get; set; }
    }
    public class InformManager
    {
        public bool VkInform;
        public string VKuserID;
    }
    #endregion
    public class DefClock : IDefClock
    {
        public int[] PowerLimits { get; set; }
        public int[] CoreClocks { get; set; }
        public int[] MemoryClocks { get; set; }
        public int[] FanSpeeds { get; set; }
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