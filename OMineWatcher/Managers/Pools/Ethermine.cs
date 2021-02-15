using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OMineWatcher.Managers.Pools
{
    public class Bitfly : IPool
    {
        private static bool InternetConnection;
        static Bitfly()
        {
            InternetConnection = InternetConnectionWacher.InternetConnectedState;
            InternetConnectionWacher.InternetConnectionLost += () => { InternetConnection = false; };
            InternetConnectionWacher.InternetConnectionRestored += () => { InternetConnection = true; };
        }

        private readonly string Wallet;
        private readonly string Endpoint;
        public bool Monitoring { get; private set; } = false;

        public event Action<CurrStats> ReceivedСurrentStats;
        public event Action<List<MiningStats>> ReceivedMiningHistory;
        public event Action<List<WorkerStats>> ReceivedWorkersStats;
        public event Action NoInformationReceived;
        public event Action WrongWallet;

        private readonly long Divider;
        private readonly bool ReportedVisible;
        public Bitfly(CoinType coin, string wallet)
        {
            Wallet = wallet;
            Divider = coin switch
            {
                CoinType.BEAM => 1000000000000000000,
                CoinType.ETC => 1000000000000000000,
                CoinType.ETH => 1000000000000000000,
                CoinType.RVN => 100000000,
                CoinType.YEC => 1000000000000000000,
                CoinType.ZEC => 1000000000000000000,
                _ => throw new NotImplementedException()
            };
            Endpoint = coin switch
            {
                CoinType.BEAM => "https://api-beam.flypool.org",
                CoinType.ETC => "https://api-etc.ethermine.org",
                CoinType.ETH => "https://api.ethermine.org",
                CoinType.RVN => "https://api-ravencoin.flypool.org",
                CoinType.YEC => "https://api-ycash.flypool.org",
                CoinType.ZEC => "https://api-zcash.flypool.org",
                _ => throw new NotImplementedException()
            };
            ReportedVisible = coin switch
            {
                CoinType.BEAM => true,
                CoinType.ETC => true,
                CoinType.ETH => true,
                CoinType.RVN => false,
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
                var sssss = $"{Endpoint}/miner/{Wallet}/dashboard";

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

                var xxx = JsonConvert.DeserializeObject<RootObject>(req);

                if (xxx.status == "OK")
                {
                    var h = Math.Round((DateTime.Now - DateTime.UtcNow).TotalHours);
                    var Workers = xxx.data.workers.Select(w => new WorkerStats
                    {
                        Name = w.worker,
                        Rep = ReportedVisible ? w.reportedHashrate : null,
                        Curr = w.currentHashrate,
                        ShInvalid = w.invalidShares,
                        ShValid = w.validShares,
                        ShStale = w.staleShares,
                        LastSeen = new DateTime(1970, 1, 1).AddSeconds(w.lastSeen).AddHours(h)
                    }).ToList();
                    var Current = new CurrStats
                    {
                        Curr = xxx.data.currentStatistics.currentHashrate,
                        Rep = ReportedVisible ? xxx.data.currentStatistics.reportedHashrate : null,
                        ShInvalid = xxx.data.currentStatistics.invalidShares,
                        ShValid = xxx.data.currentStatistics.validShares,
                        ShStale = xxx.data.currentStatistics.staleShares,
                        LastSeen = new DateTime(1970, 1, 1).
                            AddSeconds(xxx.data.currentStatistics.lastSeen).AddHours(h),
                        Unpaid = xxx.data.currentStatistics.unpaid / Divider,
                        ActiveWorkers = xxx.data.currentStatistics.activeWorkers,
                        MinPayout = xxx.data.settings.minPayout / Divider
                    };
                    var His = xxx.data.statistics.Select(st => new MiningStats
                    {
                        Curr = st.currentHashrate,
                        Rep = ReportedVisible ? st.reportedHashrate : null,
                        ShInvalid = st.invalidShares,
                        ShValid = st.validShares,
                        ShStale = st.staleShares,
                        Time = new DateTime(1970, 1, 1).
                            AddSeconds(st.time).AddHours(h),
                        ActiveWorkers = st.activeWorkers
                    }).ToList();
                    return (xxx.status, Current, His, Workers);
                }
                else return (xxx.error, null, null, null);
            }
            catch (Exception e) { return (e.Message, null, null, null); }
        }

        #region JsonClasses
#pragma warning disable IDE1006 // Стили именования
        private class Statistic
        {
            public int time { get; set; }
            public double reportedHashrate { get; set; }
            public double currentHashrate { get; set; }
            public int validShares { get; set; }
            public int invalidShares { get; set; }
            public int staleShares { get; set; }
            public int activeWorkers { get; set; }
        }
        private class Worker
        {
            public string worker { get; set; }
            public int time { get; set; }
            public int lastSeen { get; set; }
            public double reportedHashrate { get; set; }
            public double currentHashrate { get; set; }
            public int validShares { get; set; }
            public int invalidShares { get; set; }
            public int staleShares { get; set; }
        }
        private class CurrentStatistics
        {
            public int time { get; set; }
            public int lastSeen { get; set; }
            public double reportedHashrate { get; set; }
            public double currentHashrate { get; set; }
            public int validShares { get; set; }
            public int invalidShares { get; set; }
            public int staleShares { get; set; }
            public int activeWorkers { get; set; }
            public double unpaid { get; set; }
        }
        private class Settings
        {
            public double minPayout { get; set; }
        }
        private class Data
        {
            public List<Statistic> statistics { get; set; }
            public List<Worker> workers { get; set; }
            public CurrentStatistics currentStatistics { get; set; }
            public Settings settings { get; set; }
        }
        private class RootObject
        {
            public string status { get; set; }
            public string error { get; set; }
            public Data data { get; set; }
        }
#pragma warning restore IDE1006 // Стили именования
        #endregion
    }
}
