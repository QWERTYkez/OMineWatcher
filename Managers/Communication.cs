using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OMineGuard.Managers
{
    class Communication
    {


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
