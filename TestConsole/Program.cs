using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            ConnectToOMG("127.0.0.1");

            Console.ReadLine();
        }

        #region OMGcontrol
        private static TcpClient OMGcontrolClient;
        private static NetworkStream OMGcontrolStream;
        private static void ConnectToOMG(string IP)
        {
            Task.Run(() => 
            {
                OMGcontrolClient = new TcpClient(IP, 2112);
                OMGcontrolStream = OMGcontrolClient.GetStream();

                while (OMGcontrolClient.Connected)
                {
                    try
                    {
                        OMGreadMSG(OMGcontrolStream);
                        OMGreadMSG(OMGcontrolStream);
                        OMGreadMSG(OMGcontrolStream);
                        OMGreadMSG(OMGcontrolStream);

                        //обработка лога
                    }
                    catch { }
                    Thread.Sleep(50);
                }



                try
                {
                    Profile profile = JsonConvert.DeserializeObject<RootObject>(OMGreadMSG(OMGcontrolStream)).Profile;
                    string Log = JsonConvert.DeserializeObject<RootObject>(OMGreadMSG(OMGcontrolStream)).Log;

                    //обработка лога
                }
                catch { }

                Task.Run(() =>
                {
                    using (TcpClient client = new TcpClient(IP, 2113))
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            while (client.Connected)
                            {
                                try
                                {
                                    RootObject RO = JsonConvert.DeserializeObject<RootObject>(OMGreadMSG(stream));

                                    //обработка лога
                                }
                                catch { }
                                Thread.Sleep(50);
                            }
                        }
                    }
                });
            });
        }
        private static string OMGreadMSG(NetworkStream stream)
        {
            byte[] msg = new byte[4];
            stream.Read(msg, 0, msg.Length);

            Console.WriteLine("||" + BitConverter.ToInt32(msg, 0).ToString() + "||" + Environment.NewLine);


            int MSGlength = BitConverter.ToInt32(msg, 0);

            stream.Write(new byte[] { 1 }, 0, 1);

            msg = new byte[MSGlength];
            int count = stream.Read(msg, 0, msg.Length);

            Console.WriteLine("||" + Encoding.Default.GetString(msg, 0, count) + "||" + Environment.NewLine);

            return Encoding.Default.GetString(msg, 0, count);
        }
        private static object key = new object();
        public static void SendAction(object body, msgType type)
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
                            case msgType.Hasrates: header = "Profile"; break;
                            case msgType.Overclock: header = "Log"; break;
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
        }
        public enum msgType
        {
            Hasrates,
            Overclock,
            Indication,
            Logging,
            SaveProfile
        }
        #endregion

        #region JSON classes
        public class RootObject
        {
            public string Log { get; set; }
            public Profile Profile { get; set; }
            public OC? Overclock { get; set; }
            public string Logging { get; set; }
            public double[] Hasrates { get; set; }
            public bool? Indication { get; set; }
        }

        #region Profile
        public class Profile
        {
            public string RigName;
            public bool Autostart;
            public long? StartedID;
            public string StartedProcess;
            public int Digits;
            public bool[] GPUsSwitch;
            public List<Config> ConfigsList;
            public List<Overclock> ClocksList;
            public InformManager Informer;
            public double LogTextSize;

            public int TimeoutWachdog;
            public int TimeoutIdle;
            public int TimeoutLH;
        }
        public class Config
        {
            public string Name;
            public string Algoritm;
            public Miners? Miner;
            public string Pool;
            public string Port;
            public string Wallet;
            public string Params;
            public long? ClockID;
            public double MinHashrate;
            public long ID;
        }
        public enum Miners
        {
            Claymore,
            Gminer,
            Bminer
        }
        public class Overclock
        {
            public string Name;
            public int[] PowLim;
            public int[] CoreClock;
            public int[] MemoryClock;
            public uint[] FanSpeed;
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
    }
}