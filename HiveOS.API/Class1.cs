using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HiveOS.API
{
    public static class Class1
    {

        static void GGGGG()
        {
            string json = JsonConvert.SerializeObject(new LoginObject
            {
                login = "",   //ввести логин
                password = "",       // ввести пароль
                twofa_code = "",
                remember = true,
            });

            string req = HiveOSrequestPOST("https://api2.hiveos.farm/api/v2/auth/login", json);

            string access_token = JsonConvert.DeserializeObject<AceesObject>(req).access_token;

            string farmID = "";          //ввести ферму
            string workerID = "";         // ввести воркера

            string request = HiveOSrequestGET($"https://api2.hiveos.farm/api/v2/farms/{farmID}/workers/{workerID}", new RequestParam { name = "token", param = access_token });

            Console.WriteLine(request);
            Console.ReadLine();

            try
            {
                request = HiveOSrequestGET($"https://api2.hiveos.farm/api/v2/farms/{farmID}/workers/{workerID}", new RequestParam { name = "token", param = access_token });

                Console.WriteLine(request);
                Console.ReadLine();
            }
            catch (WebException) { }
        }








        static string HiveOSrequestPOST(string uri, string json)
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = body.Length;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
                stream.Close();
            }

            string req;

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

            return req;
        }
        static string HiveOSrequestGET(string uri)
        {
            string response = "";
            using (var webClient = new WebClient())
            {
                response = webClient.DownloadString(uri);
            }
            return response;
        }
        static string HiveOSrequestGET(string uri, RequestParam ReqParam)
        {
            uri = uri.TrimEnd('/');
            string param = $"{ReqParam.name}={ReqParam.param}";
            return HiveOSrequestGET($"{uri}?{param}");
        }
        static string HiveOSrequestGET(string uri, RequestParam[] ReqParams)
        {
            uri = uri.TrimEnd('/');
            string param = "";
            foreach (RequestParam p in ReqParams)
            { param += $"&{p.name}={p.param}"; }
            param = param.TrimStart('&');
            return HiveOSrequestGET($"{uri}?{param}");
        }

        class AceesObject
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
        }
        class LoginObject
        {
            public string login { get; set; }
            public string password { get; set; }
            public string twofa_code { get; set; }
            public bool remember { get; set; }
        }
    }

    public struct RequestParam
    {
        public string name;
        public string param;
    }
}
