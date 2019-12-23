using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace HiveOS.API
{
    public static class HiveClient
    {
        /// <summary>Hive authentication to use other methods</summary>
        /// <param name="Login">Hive login</param>
        /// <param name="Password">Hive password</param>
        /// <returns>A return value indicates whether authentication was successful or not</returns>
        public static AuthenticationStatus HiveAuthentication(string Login, string Password)
        {
            string json = JsonConvert.SerializeObject(new LoginObject
            {
                login = Login,
                password = Password,
                twofa_code = "",
                remember = true,
            });

            byte[] body = Encoding.UTF8.GetBytes(json);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api2.hiveos.farm/api/v2/auth/login");

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = body.Length;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
                stream.Close();
            }

            string req = "";

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream str = response.GetResponseStream())
                    {
                        int count = 0;

                        byte[] msg = new byte[1000];
                        count = str.Read(msg, 0, msg.Length);
                        req = Encoding.Default.GetString(msg, 0, count);
                    }

                    response.Close();
                }
            }
            catch (WebException we)
            {
                if (we.Message.Contains("422"))
                {
                    return new AuthenticationStatus(false, "422 UNPROCESSABLE ENTITY");
                }
                // too many requests
                if (we.Message.Contains("429"))
                {
                    return new AuthenticationStatus(false, "429 TOO MANY REQUESTS");
                }
            }

            try
            {
                AccessToken = JsonConvert.DeserializeObject<AceesObject>(req).access_token;
            }
            catch { return new AuthenticationStatus(false, "JsonConvert error"); }

            return new AuthenticationStatus(true, "Success");
        }

        /// <summary>Get all workers from Hive account</summary>
        /// <returns>Method return all workers or null if catch exception</returns>
        public static List<Worker> GetWorkers()
        {
            if (AccessToken == null) { return null; }

            string uri = $"https://api2.hiveos.farm/api/v2/farms?token={AccessToken}";
            string response = "";
            try
            {
                using (var webClient = new WebClient())
                {
                    response = webClient.DownloadString(uri);
                }
            }
            catch { return null; }

            List<Farm> Farms = new List<Farm>();
            try
            {
                Farms = JsonConvert.DeserializeObject<FarmsRO>(response).data;
            }
            catch { }

            List<Worker> AllWorkers = new List<Worker>();
            foreach (Farm f in Farms)
            {
                uri = $"https://api2.hiveos.farm/api/v2/farms/{f.id}/workers?token={AccessToken}";
                response = "";
                using (var webClient = new WebClient())
                {
                    response = webClient.DownloadString(uri);
                }

                try
                {
                    List<Worker> wl = JsonConvert.DeserializeObject<WorkersRO>(response).data;
                    foreach (Worker w in wl)
                    {
                        w.name = $"{f.name} >> {w.name}";
                    }
                    AllWorkers.AddRange(wl);
                }
                catch { }
            }

            return AllWorkers;
        }

        /// <summary>Get worker hashrates and temperatures</summary>
        /// <param name="farmID">Farm ID</param>
        /// <param name="workerID">Worker ID</param>
        /// <returns>Return worker info or null if catch exception</returns>
        public static MinerInfo? GetWorkerInfo(int farmID, int workerID)
        {
            if (AccessToken == null) { return null; }

            string uri = $"https://api2.hiveos.farm/api/v2/farms/{farmID}/workers/{workerID}?token={AccessToken}";
            string response = "";
            try
            {
                using (var webClient = new WebClient())
                {
                    response = webClient.DownloadString(uri);
                }
            }
            catch { return null; }

            WorkerInfoRO1 ro = JsonConvert.DeserializeObject<WorkerInfoRO1>(response);
            int?[] Hashrates = new int?[] { };
            int?[] Temperatures = new int?[] { };
            if (ro.miners_stats != null) { Hashrates = ro.miners_stats.hashrates[0].hashes; }
            if (ro.gpu_stats != null) { Temperatures = (from g in ro.gpu_stats orderby g.bus_number select g.temp).ToArray(); }

            return new MinerInfo(Hashrates, Temperatures);
        }

        #region 
        private static string AccessToken;
        private class AceesObject
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
        }
        private class LoginObject
        {
            public string login { get; set; }
            public string password { get; set; }
            public string twofa_code { get; set; }
            public bool remember { get; set; }
        }
        #endregion
    }
    #region FarmsRO
    public class Farm
    {
        public int id { get; set; }
        public string name { get; set; }
    }
    public class FarmsRO
    {
        public List<Farm> data { get; set; }
    }
    #endregion
    #region WorkersRO
    public class Worker
    {
        public int id { get; set; }
        public string name { get; set; }
        public int farm_id { get; set; }
    }
    public class WorkersRO
    {
        public List<Worker> data { get; set; }
    }
    #endregion
    #region MinerInfo
    public class WorkerInfoRO1
    {
        public WorkerInfoRO2 miners_stats { get; set; }
        public List<GpuStat> gpu_stats { get; set; }
    }
    public class WorkerInfoRO2
    {
        public List<hashrates> hashrates { get; set; }
    }
    public class GpuStat
    {
        public int bus_number { get; set; }
        public int? temp { get; set; }
    }
    public class hashrates
    {
        public int?[] hashes { get; set; }
    }
    public struct MinerInfo
    {
        public MinerInfo(int?[] hashes, int?[] temps)
        {
            Hashrates = new double?[hashes.Length];
            for (int i = 0; i < hashes.Length; i++)
            {
                Hashrates[i] = Convert.ToDouble(hashes[i]) / 1000;
            }
            Temperatures = temps;
        }

        public double?[] Hashrates;
        public int?[] Temperatures;
    }
    #endregion
    public struct AuthenticationStatus
    {
        public AuthenticationStatus(bool status, string message)
        {
            Status = status;
            Message = message;
        }
        public bool Status;
        public string Message;
    }
}
