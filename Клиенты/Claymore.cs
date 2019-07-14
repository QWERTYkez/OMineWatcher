using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OMineGuard
{
    public static class Claymore
    {
        private static SynchronizationContext context = SynchronizationContext.Current;

        private static void GetStat(Worker W)
        {
            TcpClient client = new TcpClient(W.IP, (int)W.Port);
            Byte[] data = Encoding.UTF8.GetBytes("{ \"id\":0,\"jsonrpc\":\"2.0\",\"method\":\"miner_getstat2\"}");
            NetworkStream stream = client.GetStream();

            try
            {
                // Отправка сообщения
                stream.Write(data, 0, data.Length);
                // Получение ответа
                Byte[] readingData = new Byte[256];
                String responseData = String.Empty;
                StringBuilder completeMessage = new StringBuilder();
                int numberOfBytesRead = 0;
                do
                {
                    numberOfBytesRead = stream.Read(readingData, 0, readingData.Length);
                    completeMessage.AppendFormat("{0}", Encoding.UTF8.GetString(readingData, 0, numberOfBytesRead));
                }
                while (stream.DataAvailable);
                context.Send(DataProcessing, new object[] { completeMessage.ToString(), W });
            }
            finally
            {
                stream.Close();
                client.Close();
            }
        }
        public static void DataProcessing(object o)
        {
            object[] obj = (object[])o;
            Worker W = (Worker)(obj[1]);
            RootObject RO = JsonConvert.DeserializeObject<RootObject>((string)(obj[0]));
            ClaymoreData CD = new ClaymoreData();
            string str = "{\"xxx\": [" + RO.result[3].Replace(";", ",") + "]}";
            CD._minutes = Convert.ToInt32(RO.result[1]);
            CD._hashrates = JsonConvert.DeserializeObject<RootObject2>("{\"xxx\": [" + RO.result[3].Replace(";", ",") + "]}").xxx;
            int lt = CD._hashrates.Length;
            int[] xx = JsonConvert.DeserializeObject<RootObject2>("{\"xxx\": [" + RO.result[6].Replace(";", ",") + "]}").xxx;
            CD._temperatures = new byte[lt];
            CD._fanspeeds = new byte[lt];
            for (int n = 0; n < xx.Length; n = n + 2)
            {
                CD._temperatures[n / 2] = (byte)xx[n];
                CD._fanspeeds[n / 2] = (byte)xx[n + 1];
            }
            CD._poolswitches = JsonConvert.DeserializeObject<RootObject2>("{\"xxx\": [" + RO.result[8].Replace(";", ",") + "]}").xxx[1];
            CD._accepted = JsonConvert.DeserializeObject<RootObject2>("{\"xxx\": [" + RO.result[9].Replace(";", ",") + "]}").xxx;
            CD._rejected = JsonConvert.DeserializeObject<RootObject2>("{\"xxx\": [" + RO.result[10].Replace(";", ",") + "]}").xxx;
            CD._invalid = JsonConvert.DeserializeObject<RootObject2>("{\"xxx\": [" + RO.result[11].Replace(";", ",") + "]}").xxx;

            // Вывод данных
        }   /////////
    }
    public class RootObject
    {
        public int id { get; set; }
        public object error { get; set; }
        public List<string> result { get; set; }
    }
    public class RootObject2
    {
        public int[] xxx { get; set; }
    }
    public struct ClaymoreData
    {
        public int _minutes;
        public int[] _hashrates;
        public byte[] _temperatures;
        public byte[] _fanspeeds;
        public int _poolswitches;
        public int[] _accepted;
        public int[] _rejected;
        public int[] _invalid;
    }
}