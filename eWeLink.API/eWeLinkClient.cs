using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using WebSocket4Net;

namespace eWeLink.API
{
    public static class eWeLinkClient
    {
        private static string APIkey = "";
        private static string AT = "";
        private static string payloadLogin = "";
        private static string payloadUpdate = "";
        private static WebSocket WS;

        private static string device;

        private static string HMAC(string str)
        {
            string skey = "6Nz4n0xA8s8qdxQf2GqurZj2Fs55FUvM";
            byte[] bkey = Encoding.UTF8.GetBytes(skey);

            byte[] source = Encoding.UTF8.GetBytes(str);

            byte[] hash;
            using (HMACSHA256 hmac = new HMACSHA256(bkey))
            {
                hash = hmac.ComputeHash(source);
            }
            return Convert.ToBase64String(hash);
        }
        private static bool AutheWeLink(string login, string password, ref string APIkey, ref string AT)
        {
            string uri = "https://eu-api.coolkit.cc:8080/api/user/login";
            string json = JsonConvert.SerializeObject(new _LoginToEwelink
            {
                email = login,
                password = password
            });

            byte[] body = Encoding.UTF8.GetBytes(json);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = body.Length;
            request.Headers.Add("Authorization", $"Sign {HMAC(json)}");

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
            if (req.Contains("\"at\"") && req.Contains("\"apikey\""))
            {
                _eWelinkAuth e = JsonConvert.DeserializeObject<_eWelinkAuth>(req);
                APIkey = e.user.apikey;
                AT = e.at;
                return true;
            }
            else return false;
        }
        public static bool AutheWeLink(string login, string password)
        {
            string uri = "https://eu-api.coolkit.cc:8080/api/user/login";
            string json = JsonConvert.SerializeObject(new _LoginToEwelink
            {
                email = login,
                password = password
            });

            byte[] body = Encoding.UTF8.GetBytes(json);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = body.Length;
            request.Headers.Add("Authorization", $"Sign {HMAC(json)}");

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
            if (req.Contains("\"at\"") && req.Contains("\"apikey\"")) return true;
            else return false;
        }



        #region JSON clases
        public class _eWelinkAuth
        {
            public string at { get; set; }
            public _eWelinkUser user { get; set; }
        }
        public class _eWelinkUser
        {
            public string apikey { get; set; }
        }
        public class _LoginToEwelink
        {
            public string email { get; set; }
            public string password { get; set; }
            public int version = 6;
            public string ts = $"{Math.Round((DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds) / 1000}";

            public string appid = "oeVkj2lYFGnJu5XUtWisfW4utiN4u9Mq";
            public string imei = $"DF7425A0-{new Random().Next(1000, 9999)}-{new Random().Next(1000, 9999)}-9F5E-3BC9179E48FB";
            public string os = "iOS";
            public string model = "iPhone10,6";
            public string romVersion = "11.1.2";
            public string appVersion = "3.5.3";
        }

        public class _eWelinkDevices
        {
            public List<_eWelinkDevice> Devices { get; set; }
        }
        public class _eWelinkDevice
        {
            public string _id { get; set; }
            public string name { get; set; }
            public string deviceid { get; set; }
            public string apikey { get; set; }
            public _Params @params { get; set; }
            public bool online { get; set; }
            public string devicekey { get; set; }
        }
        public class _Params
        {
            public string @switch { get; set; }
        }
        #endregion
    }
}
