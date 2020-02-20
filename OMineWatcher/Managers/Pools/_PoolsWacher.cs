using System;
using OMineWatcher.Managers;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OMineWatcher.Managers.Pools
{
    public static class PoolsWacher
    {
        public static PoolType[] PoolTypes = new PoolType[] { PoolType.Bitfly };
        public static Dictionary<PoolType, CoinType[]> Coins = new Dictionary<PoolType, CoinType[]>
        {
            { 
                PoolType.Bitfly, 
                
                new CoinType[] 
                { 
                    CoinType.BEAM, 
                    CoinType.ETC,
                    CoinType.ETH,
                    CoinType.RVN,
                    CoinType.YEC,
                    CoinType.ZEC
                } 
            }
        };

        public static IPool StartWach(PoolSet ps)
        {
            if (ps != null)
            {
                if (ps.Pool != null && ps.Coin != null)
                {
                    switch (ps.Pool)
                    {
                        case PoolType.Bitfly: return new Bitfly(ps.Coin.Value, ps.Wallet);
                        default: return null;
                    }
                }
                return null;
            }
            else return null;
        }

        static PoolsWacher()
        {
            Task.Run(() => 
            {
                var min = new TimeSpan(0, 1, 0).TotalSeconds;
                while (App.Live)
                {
                    if (InternetConnectionWacher.InternetConnectedState)
                    { try { GetUSD(); } catch { } }
                    for (int i = 0; i < min; i++) if (App.Live) Thread.Sleep(1000);
                }
            });
            var x = Task.Run(() =>
            {
                var ts = new TimeSpan(0, 5, 0).TotalSeconds;
                while (App.Live)
                {
                    if (InternetConnectionWacher.InternetConnectedState)
                    {
                        try
                        {
                            var request = WebRequest.Create($"https://www.coincalculators.io/api?hashrate=1");
                            var response = request.GetResponse();
                            string req;
                            using (var stream = response.GetResponseStream())
                            {
                                using (var reader = new System.IO.StreamReader(stream))
                                {
                                    req = reader.ReadToEnd();
                                }
                            }

                            Profits = JsonConvert.DeserializeObject<List<RootObject>>(req);
                        }
                        catch { }
                    }
                    for (int i = 0; i < ts; i++) if (App.Live) Thread.Sleep(1000);
                }
            });
        }
        private static double USDcost { get; set; } = 0;
        private static List<RootObject> Profits { get; set; } = new List<RootObject>();

        public static (double? coin, double? USD, double? RUB) GetProfit(CoinType ct)
        {
            string name = "";
            switch (ct)
            {
                case CoinType.ETC: name = "EthereumClassic"; break;
            }

            RootObject ro = null;
            try { ro = Profits.Where(x => x.name == name).First(); } catch { return (null, null, null); }

            return (ro.rewardsInDay, ro.revenueInDayUSD, ro.revenueInDayUSD * USDcost);
        }
        private static void GetUSD()
        {
            var request = WebRequest.Create("https://www.cbr-xml-daily.ru/daily_json.js");
            var response = request.GetResponse();
            string req;
            using (var stream = response.GetResponseStream())
            {
                using (var reader = new System.IO.StreamReader(stream))
                {
                    req = reader.ReadToEnd();
                }
            }

            USDcost = JsonConvert.DeserializeObject<RootObject2>(req).Valute.USD.Value;
        }
        private class RootObject
        {
            public string name { get; set; }
            public double rewardsInDay { get; set; }
            public double revenueInDayUSD { get; set; }
        }
        private class RootObject2
        {
            public Val Valute { get; set; }

            public class Val
            {
                public USDT USD { get; set; }

                public class USDT
                {
                    public double Value { get; set; }
                }
            }
        }
    }

    public enum PoolType
    {
        Bitfly
    }
    public enum CoinType
    {
        BEAM, ETC, ETH, RVN, YEC, ZEC,
    }
    public interface IPool
    {
        event Action<CurrStats> ReceivedСurrentStats;
        event Action<List<MiningStats>> ReceivedMiningHistory;
        event Action<List<WorkerStats>> ReceivedWorkersStats;
        event Action NoInformationReceived;
        event Action WrongWallet;
        void StopMonitoring();
        bool Alive { get; }
    }
    
    public struct WorkerStats
    {
        public string Name;
        public double Rep;
        public double Curr;
        public int ShInvalid;
        public int ShValid;
        public int ShStale;
        public DateTime LastSeen;
    }
    public struct CurrStats
    {
        public double Curr;
        public double Rep;
        public int ShInvalid;
        public int ShValid;
        public int ShStale;
        public DateTime LastSeen;
        public double Unpaid;
        public int ActiveWorkers;
        public double MinPayout;
    }
    public struct MiningStats
    {
        public double Curr;
        public double Rep;
        public int ShInvalid;
        public int ShValid;
        public int ShStale;
        public DateTime Time;
        public int ActiveWorkers;
    }
}
