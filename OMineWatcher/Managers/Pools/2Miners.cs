using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OMineWatcher.Managers.Pools
{
    public class _2Miners : IPool
    {
        private static bool InternetConnection;
        static _2Miners()
        {
            InternetConnection = InternetConnectionWacher.InternetConnectedState;
            InternetConnectionWacher.InternetConnectionLost += () => { InternetConnection = false; };
            InternetConnectionWacher.InternetConnectionRestored += () => { InternetConnection = true; };
        }

        private readonly string Wallet;
        private readonly string CoinName;
        public bool Monitoring { get; private set; } = false;

        public event Action<CurrStats> ReceivedСurrentStats;
        public event Action<List<MiningStats>> ReceivedMiningHistory;
        public event Action<List<WorkerStats>> ReceivedWorkersStats;
        public event Action NoInformationReceived;
        public event Action WrongWallet;

        private readonly long Divider;
        private readonly bool ReportedVisible;
        public _2Miners(CoinType coin, string wallet)
        {
            Wallet = wallet;
            Divider = coin switch
            {
                CoinType.BEAM => 1000000000,
                CoinType.ETC => 1000000000,
                CoinType.ETH => 1000000000,
                CoinType.RVN => 1000000000,
                CoinType.ZEC => 1000000000,
                _ => throw new NotImplementedException()
            };
            CoinName = coin switch
            {
                CoinType.BEAM => "BEAM",
                CoinType.ETC => "ETC",
                CoinType.ETH => "ETH",
                CoinType.RVN => "RVN",
                CoinType.ZEC => "ZEC",
                _ => throw new NotImplementedException()
            };
            ReportedVisible = coin switch
            {
                CoinType.BEAM => true,
                CoinType.ETC => true,
                CoinType.ETH => true,
                CoinType.RVN => true,
                CoinType.YEC => true,
                CoinType.ZEC => true,
                _ => throw new NotImplementedException()
            };
            if (!Monitoring)
            {
                Monitoring = true;
                Task.Run(() =>
                {
                    var min = new TimeSpan(0, 1, 0).TotalSeconds;
                    while (App.Live && Monitoring)
                    {
                        while (InternetConnection)
                        {
                            Task.Run(() =>
                            {
                                (var status,
                                var СurrentStats,
                                var MiningHistory,
                                var WorkersStats) = GetStats();
                                if (status == "OK")
                                {
                                    if (СurrentStats != null)
                                    {
                                        Task.Run(() => ReceivedСurrentStats?.Invoke(СurrentStats.Value));
                                    }
                                    if (MiningHistory != null)
                                    {
                                        Task.Run(() => ReceivedMiningHistory?.Invoke(MiningHistory));
                                    }
                                    if (WorkersStats != null)
                                    {
                                        Task.Run(() => ReceivedWorkersStats?.Invoke(WorkersStats));
                                    }
                                }
                                else if (status == "Invalid address")
                                {
                                    WrongWallet?.Invoke();
                                    return;
                                }
                                else Task.Run(() => NoInformationReceived?.Invoke());
                            });
                            for (int i = 0; i < min; i++)
                            {
                                if (App.Live && Monitoring) Thread.Sleep(1000);
                                else { return; }
                            }
                        }
                        Thread.Sleep(1000);
                    }
                });
            }
        }
        public void StopMonitoring()
        {
            ReceivedСurrentStats = null;
            ReceivedMiningHistory = null;
            ReceivedWorkersStats = null;
            NoInformationReceived = null;
            WrongWallet = null;
            Monitoring = false;
        }
        private (string status, CurrStats? СurrentStats,
            List<MiningStats> MiningHistory,
            List<WorkerStats> WorkersStats) GetStats()
        {
            try
            {
                var sssss = $"https://{CoinName}.2miners.com/api/accounts/{Wallet}";

                var request = System.Net.WebRequest.
                    Create(sssss);
                var response = request.GetResponse();
                string req;
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        req = reader.ReadToEnd();
                    }
                }

                var xxx = JsonConvert.DeserializeObject<Root>(req);
                xxx.WorkersRoot = GetWorkers(req);

                if (xxx.code == 999)
                {
                    var h = Math.Round((DateTime.Now - DateTime.UtcNow).TotalHours);

                    var Workers = xxx.WorkersRoot.Workers.Select(w => new WorkerStats
                    {
                        Name = w.Name,
                        Rep = GetValOrNull(ReportedVisible, w.rhr),
                        Curr = w.hr,
                        ShInvalid = w.sharesInvalid,
                        ShValid = w.sharesValid,
                        ShStale = w.sharesStale,
                        LastSeen = new DateTime(1970, 1, 1).AddSeconds(w.lastBeat).AddHours(h)
                    }).ToList();

                    var Current = new CurrStats
                    {
                        Curr = xxx.currentHashrate,
                        Rep = GetValOrNull(ReportedVisible, xxx.WorkersRoot.Workers.Sum(w => w.rhr)),
                        ShInvalid = xxx.sharesInvalid,
                        ShValid = xxx.sharesValid,
                        ShStale = xxx.sharesStale,
                        LastSeen = new DateTime(1970, 1, 1).
                            AddMilliseconds(xxx.updatedAt).AddHours(h),
                        Unpaid = Convert.ToDouble(xxx.stats.balance) / Divider,
                        ActiveWorkers = xxx.workersOnline,
                        MinPayout = Convert.ToDouble(xxx.config.minPayout) / Divider
                    };
                    xxx.rewards.Reverse();
                    var His = xxx.rewards.Select(st => new MiningStats
                    {
                        Curr = st.reward,
                        Rep = null,
                        Time = new DateTime(1970, 1, 1).
                            AddSeconds(st.timestamp).AddHours(h)
                    }).ToList();

                    DateTime Time;

                    var ttt = His.Select(h => h.Time).ToList();

                    if (((His[0].Time.Minute / 10) + 1) * 10 > 55)
                    {
                        var nt = His[0].Time.AddMinutes(10);
                        Time = new DateTime(nt.Year, nt.Month, nt.Day, nt.Hour, 0, 0);
                    }
                    else
                    {
                        Time = new DateTime(His[0].Time.Year, His[0].Time.Month, His[0].Time.Day, His[0].Time.Hour, ((His[0].Time.Minute / 10) + 1) * 10, 0);
                    }
                    double Curr = 0;
                    var Hist = new List<MiningStats>();
                    foreach (var hs in His)
                    {
                        if ((Time - hs.Time).TotalMinutes > 0)
                        {
                            Curr += hs.Curr;
                        }
                        else
                        {
                            Hist.Add(new MiningStats 
                            { 
                                Time = Time,
                                Curr = Curr
                            });
                            Curr = hs.Curr;
                            Time = Time.AddMinutes(10);
                        }
                    }
                    Hist.Add(new MiningStats
                    {
                        Time = Time,
                        Curr = Curr
                    });

                    var n = DateTime.Now.AddDays(-1);
                    Hist = Hist.Where(h => h.Time > n).ToList();
                    var sdt = Hist[0].Time;
                    for (int i = 1; i < Hist.Count; i++)
                    {
                        while (Hist[i].Time != sdt.AddMinutes(10))
                        {
                            Hist.Insert(i, new MiningStats
                            {
                                Curr = 0,
                                Rep = 0,
                                Time = Hist[i].Time.AddMinutes(-10),
                            });
                        }
                        sdt = sdt.AddMinutes(10);
                    }

                    return (xxx.message, Current, Hist, Workers);
                }
                else return (xxx.message, null, null, null);
            }
            catch (Exception e) { return (e.Message, null, null, null); }
        }
        private double? GetValOrNull(bool condition, double value)
        {
            if (condition) return value;
            else return null;
        }
        private WRoot GetWorkers(string message)
        {
            var chs = message.ToArray();
            int i = chs.Length - 1;
            int j = 0;
            int k = 0;
            while (true)
            {
                if (j < 3)
                {
                    if (chs[i] is ',')
                        j++;
                }
                else
                {
                    j = i;
                    break;
                }
                i--;
            }
            while (true)
            {
                if (chs[i] is '"')
                {
                    k = i;
                    i--;
                    if (chs[i] is 's')
                    {
                        i--;
                        if (chs[i] is 'r')
                        {
                            i--;
                            if (chs[i] is 'e')
                            {
                                i--;
                                if (chs[i] is 'k')
                                {
                                    i--;
                                    if (chs[i] is 'r')
                                    {
                                        i--;
                                        if (chs[i] is 'o')
                                        {
                                            i--;
                                            if (chs[i] is 'w')
                                            {
                                                i--;
                                                if (chs[i] is '"')
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                i--;
            }
            k = k + 2; j++;
            string Workers = $"{{\"Workers\":[{new string(message.Take(j).Skip(k).ToArray())}]}}".Replace("\":{", "\",").Replace("},\"", "},{\"Name\":\"").Replace("\":[{\"", "\":[{\"Name\":\"").Replace("}}]", "}]");
            return JsonConvert.DeserializeObject<WRoot>(Workers);
        }

        #region JsonClasses
#pragma warning disable IDE1006 // Стили именования
        private class Config
        {
            public long allowedMaxPayout { get; set; }
            public int allowedMinPayout { get; set; }
            public int defaultMinPayout { get; set; }
            public string ipHint { get; set; }
            public string ipWorkerName { get; set; }
            public long minPayout { get; set; }
        }

        private class Payment
        {
            public object amount { get; set; }
            public int timestamp { get; set; }
            public string tx { get; set; }
        }

        private class Reward
        {
            public int blockheight { get; set; }
            public int timestamp { get; set; }
            public int reward { get; set; }
            public double percent { get; set; }
            public bool immature { get; set; }
            public bool orphan { get; set; }
            public bool uncle { get; set; }
        }
        private class Stats
        {
            public long balance { get; set; }
            public int blocksFound { get; set; }
            public int immature { get; set; }
            public int lastShare { get; set; }
            public long paid { get; set; }
            public int pending { get; set; }
        }
        private class Sumreward
        {
            public int inverval { get; set; }
            public object reward { get; set; }
            public int numreward { get; set; }
            public string name { get; set; }
            public int offset { get; set; }
        }
        private class Worker
        {
            public string Name { get; set; }
            public int lastBeat { get; set; }
            public int hr { get; set; }
            public bool offline { get; set; }
            public int hr2 { get; set; }
            public int rhr { get; set; }
            public int sharesValid { get; set; }
            public int sharesInvalid { get; set; }
            public int sharesStale { get; set; }
        }
        private class WRoot
        {
            public List<Worker> Workers { get; set; }
        }
        private class Root
        {
            public int _24hnumreward { get; set; }
            public long _24hreward { get; set; }
            public int apiVersion { get; set; }
            public Config config { get; set; }
            public long currentHashrate { get; set; }
            public string currentLuck { get; set; }
            public long hashrate { get; set; }
            public int pageSize { get; set; }
            public List<Payment> payments { get; set; }
            public int paymentsTotal { get; set; }
            public List<Reward> rewards { get; set; }
            public int roundShares { get; set; }
            public int sharesInvalid { get; set; }
            public int sharesStale { get; set; }
            public int sharesValid { get; set; }
            public Stats stats { get; set; }
            public List<Sumreward> sumrewards { get; set; }
            public long updatedAt { get; set; }
            public WRoot WorkersRoot { get; set; }
            public int workersOffline { get; set; }
            public int workersOnline { get; set; }
            public int workersTotal { get; set; }
            public int code { get; set; } = 999;
            public string message { get; set; } = "OK";
            public string description { get; set; }
        }
#pragma warning restore IDE1006 // Стили именования
        #endregion
    }
}
