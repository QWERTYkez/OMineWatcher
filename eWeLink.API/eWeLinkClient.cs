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
        private static object key = new object();
        private static string Login = "";
        private static string Password = "";
        private static WebSocket WS;
        private static string APIkey = "";
        private static string AT = "";

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
            lock (key)
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
        }
        /// <summary>
        /// Метод проверяющй правильность данных для входа в аккаунт eWeLink
        /// и автоматически сохраняет прошедшие проверку логин и пароль
        /// для использования в других методах
        /// </summary>
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
            if (req.Contains("\"at\"") && req.Contains("\"apikey\""))
            {
                SetAuth(login, password);
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Метод регистрирующий правильные логин и пароль для использования в других методах
        /// </summary>
        public static void SetAuth(string login, string password)
        {
            Login = login;
            Password = password;

        }

        public static List<_eWelinkDevice> eWeLinkGetDevices()
        {
            if (Login == "" || Login == null) { return null; }
            if (Password == "" || Password == null) { return null; }

            if (AT == "") { AutheWeLink(Login, Password, ref APIkey, ref AT); }

            string uri = "https://eu-api.coolkit.cc:8080/api/user/device";

            string response = "";
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("Authorization", $"Bearer {AT}");
                response = webClient.DownloadString(uri);
            }
            if (response.Contains("\"error\":401"))
            {
                AutheWeLink(Login, Password, ref APIkey, ref AT);

                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("Authorization", $"Bearer {AT}");
                    response = webClient.DownloadString(uri);
                }
                if (response.Contains("\"error\":401"))
                {
                    return null;
                }
                else
                {
                    string str = $"{{\"Devices\":{response}}}";
                    return JsonConvert.DeserializeObject<_eWelinkDevices>(str).Devices;
                }
            }
            else
            {
                string str = $"{{\"Devices\":{response}}}";
                return JsonConvert.DeserializeObject<_eWelinkDevices>(str).Devices;
            }
        }

        #region OperationsList //AddOperation(string deviceID, Operations operation, DeviceState deviceState)
        private static object OPkey = new object();
        private static List<_Operation> OperationsList = new List<_Operation>();
        private class _Operation
        {
            public _Operation(string deviceID, Operations operation, DeviceState deviceState)
            {
                DeviceID = deviceID;
                Operation = operation;
                DevState = deviceState;
            }

            public string DeviceID;
            public Operations Operation;
            public DeviceState DevState;
        }
        public static void AddOperation(string deviceID, Operations operation, DeviceState deviceState)
        {
            _Operation OP = new _Operation(deviceID, operation, deviceState);
            bool x;
            lock (OPkey)
            {
                OperationsList.Add(OP);
                x = OperationsList.Count > 0;
            }
            if (x) OperationsHandle(OP);
        }
        private static void OperationRemove(_Operation OP)
        {
            bool x;
            lock (OPkey)
            {
                OperationsList.Remove(OP);
                x = OperationsList.Count > 0;
                if (x) CurrentOP = OperationsList[0];
            }
            if (x) OperationsHandle(CurrentOP);
        }
        private static _Operation CurrentOP;
        private static void OperationsHandle(_Operation OP)
        {
            CurrentOP = OP;
            switch (CurrentOP.Operation)
            {
                case Operations.RebootDevice:
                    OPRebootDevice(CurrentOP.DeviceID);
                    break;
                case Operations.SetDeviceState:
                    OPSetDeviceState(CurrentOP.DeviceID, CurrentOP.DevState);
                    break;
            }
        }
        #endregion
        #region RebootDevice  //OPRebootDevice(string deviceID)
        private static string payloadLoginR = "";
        private static string payloadUpdateR = "";
        private static int counterR;
        private static string deviceID_R;
        private static int SwichStateDelay = 10; // жка между переключениями, сек
        private static void OPRebootDevice(string deviceID)
        {
            deviceID_R = deviceID;
            counterR = 0;
            AutheWeLink(Login, Password, ref APIkey, ref AT);

            string uri = "wss://eu-pconnect3.coolkit.cc:8080/api/ws";

            payloadLoginR = JsonConvert.SerializeObject(new _EwelinkLoginPayload(AT, APIkey));
            payloadUpdateR = JsonConvert.SerializeObject(new _EwelinkUpdatePayload(deviceID, "off", APIkey));

            WS = new WebSocket(uri);
            WS.Opened += websocketopR;
            WS.MessageReceived += websocketreqR;

            WS.Open();
        }
        private static void websocketopR(object sender, EventArgs e)
        {
            WS.Send(payloadLoginR);
        }
        private static void websocketreqR(object sender, MessageReceivedEventArgs e)
        {
            counterR++;
            if (counterR == 1)
            {
                WS.Send(payloadUpdateR);
            }
            if (counterR == 2)
            {
                WS.Close();
                WS.Dispose();
                Thread.Sleep(1000 * SwichStateDelay);
                OPSetDeviceState(deviceID_R, DeviceState.on);
            }
        }
        #endregion
        #region SwichDevice  //OPSetDeviceState(string deviceID, DeviceState Dstate)
        private static string payloadLoginS = "";
        private static string payloadUpdateS = "";
        private static int counterS;
        private static void OPSetDeviceState(string deviceID, DeviceState Dstate)
        {
            counterS = 0;
            AutheWeLink(Login, Password, ref APIkey, ref AT);

            string uri = "wss://eu-pconnect3.coolkit.cc:8080/api/ws";

            string state = "";
            if (Dstate == DeviceState.on) { state = "on"; }
            else { state = "off"; }

            payloadLoginS = JsonConvert.SerializeObject(new _EwelinkLoginPayload(AT, APIkey));
            payloadUpdateS = JsonConvert.SerializeObject(new _EwelinkUpdatePayload(deviceID, state, APIkey));

            WS = new WebSocket(uri);
            WS.Opened += websocketopS;
            WS.MessageReceived += websocketreqS;

            WS.Open();
        }
        private static void websocketopS(object sender, EventArgs e)
        {
            WS.Send(payloadLoginS);
        }
        private static void websocketreqS(object sender, MessageReceivedEventArgs e)
        {
            counterS++;
            if (counterS == 1)
            {
                WS.Send(payloadUpdateS);
            }
            if (counterS == 2)
            {
                Thread.Sleep(1000);
                WS.Close();
                WS.Dispose();
                OperationRemove(CurrentOP);
            }
        }
        #endregion
    }
    public enum DeviceState
    {
        on,
        off
    }
    public enum Operations
    {
        RebootDevice,
        SetDeviceState
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

    public class _EwelinkLoginPayload
    {
        public _EwelinkLoginPayload(string AT, string APIkey)
        {
            double DT = (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            ts = $"{Math.Floor(DT / 1000)}";
            sequence = $"{Math.Floor(DT)}";

            at = AT;
            apikey = APIkey;
        }

        public string action = "userOnline";
        public string userAgent = "app";
        public int version = 6;
        public string apkVesrion = "1.8";
        public string os = "iOS";
        public string at { get; set; }
        public string apikey { get; set; }
        public string ts { get; set; }
        public string model = "iPhone10,6";
        public string romVersion = "11.1.2";
        public string sequence { get; set; }
    }
    public class _EwelinkUpdatePayload
    {
        public _EwelinkUpdatePayload(string deviceID, string state, string APIkey)
        {
            deviceid = deviceID;

            double DT = (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            sequence = $"{Math.Floor(DT)}";

            @params = new _Params();
            @params.@switch = state;

            apikey = APIkey;
        }

        public string action = "update";
        public string userAgent = "app";
        public string apikey { get; set; }
        public string deviceid { get; set; }
        public _Params @params { get; set; }
        public string sequence { get; set; }
    }
    #endregion
}