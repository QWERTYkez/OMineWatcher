using OMineWatcher.Managers.Pools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OMineWatcher.MVVM.ViewModels
{
    public class PoolViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public int Index { get; set; }
        public Dispatcher Dispatcher { get; set; }
        private IPool Pool { get; set; }
        private double? AVG { get; set; }
        private Managers.PoolSet Settings { get; set; }
        public PoolViewModel() { }
        public PoolViewModel(Managers.PoolSet settings, int index)
        {
            Index = index;
            Settings = settings;

            WVisibility = Visibility.Collapsed;

            Name = Settings.Name;
            PoolType = Settings.Pool;
            CoinType = Settings.Coin;

            Settings.NameChanged += name => Name = name;
            Settings.PoolChanged += pool => { PoolType = pool; StartWach(); };
            Settings.CoinChanged += coin => { CoinType = coin; StartWach(); };
            Settings.WalletChanged += wallet => StartWach();
            Settings.WachChanged += wach => { if (!wach) { Pool?.StopMonitoring(); Pool = null; } };
        }
        public void StartWach()
        {
            if (Pool != null) { Pool.StopMonitoring(); Pool = null; }
            {//очистка
                MinPayout = null;

                CoinD = null;
                CoinM = null;
                СurrencyD = null;
                СurrencyM = null;
                СurrencyType = null;

                ActiveWorkers = null;
                LastSeen = null;
                Percent = null;
                Progress = null;
                HashrateReported = null;
                HashrateCurrent = null;
                HashrateAverage = null;
                Shares = null;
                Unpaid = null;

                LastHist = null;
                ReportedHashes = null;
                CurrentHashes = null;
                AverageHashes = null;

                WorkersNames = new List<string>();
                WorkersReported = new List<string>();
                WorkersCurrent = new List<string>();
                WorkersShares = new List<string>();
                WorkersLS = new List<string>();
            }
            Pool = PoolsWacher.StartWach(Settings);
            if (Pool != null)
            {
                Pool.NoInformationReceived += () => { NameColor = Brushes.DarkRed; Waching = false; };
                Pool.ReceivedMiningHistory += hist =>
                {
                    if (NameColor == Brushes.DarkRed) NameColor = Brushes.White;
                    Waching = true;
                    Error = null;

                    if (LastHist != null)
                    {
                        var histT = hist.Select(h => h.Time).Max();
                        if (LastHist == histT) return;
                        LastHist = histT;
                    }

                    var reps = hist.Select(h => h.Rep).ToList();
                    var curs = hist.Select(h => h.Curr).ToList();

                    var all = reps.Concat(curs);
                    var max = all.Max();
                    var min = all.Min();
                    var delta = max - min;
                    max += delta * 0.1;
                    min -= delta * 0.1;
                    delta = max - min;
                    var average = HCheight * (curs.Average() - min) / delta;

                    var CurrPoints = new List<Point>();
                    var RepPoints = new List<Point>();
                    for (int i = 0; i < hist.Count; i++)
                    {
                        RepPoints.Add(new Point(i * HCwidth / (hist.Count - 1), HCheight * (reps[i] - min) / delta));
                        CurrPoints.Add(new Point(i * HCwidth / (hist.Count - 1), HCheight * (curs[i] - min) / delta));
                    }
                    Dispatcher.InvokeAsync(() => 
                    {
                        ReportedHashes = new PointCollection(RepPoints);
                        CurrentHashes = new PointCollection(CurrPoints);
                    });
                    AverageHashes = average;
                    AVG = hist.Select(h => h.Curr).Average();
                    HashrateAverage = HashrateConvert(AVG.Value, 4);

                    UpdateProfitRate();
                };
                Pool.ReceivedWorkersStats += workers =>
                {
                    if (NameColor == Brushes.DarkRed) NameColor = Brushes.White;
                    Waching = true;
                    Error = null;

                    WorkersNames = workers.Select(w => w.Name).ToList();
                    WorkersReported = workers.Select(w => HashrateConvert(w.Rep, 4)).ToList();
                    WorkersCurrent = workers.Select(w => HashrateConvert(w.Curr, 4)).ToList();
                    WorkersShares = workers.Select(w => $"{w.ShValid} ({RoundDouble(((double)w.ShValid * 100 / (double)(w.ShValid + w.ShInvalid + w.ShStale)), 3)}%)").ToList();
                    WorkersLS = workers.Select(w => w.LastSeen.ToString("HH:mm:ss")).ToList();

                    ChangeWisability();
                };
                Pool.ReceivedСurrentStats += stats =>
                {
                    if (NameColor == Brushes.DarkRed) NameColor = Brushes.White;
                    Waching = true;
                    Error = null;
                    LastSeen = stats.LastSeen;
                    var percent = (stats.Unpaid / stats.MinPayout);
                    if (Percent != null)
                    {
                        if (percent < Percent)
                        {
                            Managers.UserInformer.PayoutPlay();
                        }
                    }
                    Percent = percent;
                    Progress = Math.Round(Percent.Value * 1000);
                    ActiveWorkers = stats.ActiveWorkers;
                    HashrateReported = HashrateConvert(stats.Rep, 4);
                    HashrateCurrent = HashrateConvert(stats.Curr, 4);
                    Unpaid = RoundDouble(stats.Unpaid, 4);
                    var shpercent = (double)stats.ShValid * 100 /
                        (double)(stats.ShValid + stats.ShInvalid + stats.ShStale);
                    Shares = $"{stats.ShValid} ({RoundDouble(shpercent, 3)}%)";
                    MinPayout = stats.MinPayout;
                };
                Pool.WrongWallet += () => { NameColor = Brushes.DarkRed; Error = "Wrong Wallet"; Pool = null; };

                Task.Run(() => 
                {
                    try
                    {
                        while (Pool.Monitoring)
                        {
                            rate = PoolsWacher.GetProfit(Settings.Coin.Value);
                            if (AVG != null) UpdateProfitRate();
                            Thread.Sleep(5000);
                        }
                    }
                    catch { }
                });
            }
            else Error = "Wrong Settings";
        }
        private void UpdateProfitRate()
        {
            if (rate.coin != null)
            {
                CoinD = RoundDouble(rate.coin.Value * AVG.Value, 4);
                CoinM = RoundDouble(rate.coin.Value * AVG.Value * 30, 4);
            }
            if (rate.RUB != null)
            {
                СurrencyD = RoundDouble(rate.RUB.Value * AVG.Value, 4);
                СurrencyM = RoundDouble(rate.RUB.Value * AVG.Value * 30, 4);
                СurrencyType = "RUB";
            }
            else if (rate.USD != null)
            {
                СurrencyD = RoundDouble(rate.USD.Value * AVG.Value, 4);
                СurrencyM = RoundDouble(rate.USD.Value * AVG.Value * 30, 4);
                СurrencyType = "USD";
            }
        }
        public static string HashrateConvert(double hash, int decimals)
        {
            int n = 0;
            while (hash > 999) { hash /= 1000; n++; }
            string x = "";
            switch (n)
            {
                case 0: x = ""; break;
                case 1: x = "K"; break;
                case 2: x = "M"; break;
                case 3: x = "G"; break;
                case 4: x = "T"; break;
                case 5: x = "P"; break;
            }
            if (hash > 99)
            {
                hash = Math.Round(hash, decimals - 3);
                return $"{hash} {x}h/s";
            }
            if (hash > 9)
            {
                hash = Math.Round(hash, decimals - 2);
                return $"{hash} {x}h/s";
            }
            hash = Math.Round(hash, decimals - 1);
            return $"{hash} {x}h/s";
        }
        public static double RoundDouble(double d, int decimals) 
        {
            int x = decimals - 1;
            if (d >= 10) { x = decimals - 2; }
            if (d >= 100) { x = decimals - 3; }
            if (d >= 1000) { x = decimals - 4; }
            if (d >= 10000) { x = decimals - 5; }
            if (x > 0) { return Math.Round(d, x); }
            else { return Math.Round(d, 0); }
        }

        public string Name { get; set; }
        public Brush NameColor { get; set; } = Brushes.White;
        public string Error { get; set; }
        public PoolType? PoolType { get; set; }
        public CoinType? CoinType { get; set; }
        public bool Waching { get; set; } = false;
        public double? MinPayout { get; set; }

        //current
        private (double? coin, double? USD, double? RUB) rate;
        public double? CoinD { get; set; }
        public double? CoinM { get; set; }
        public double? СurrencyD { get; set; }
        public double? СurrencyM { get; set; }
        public string СurrencyType { get; set; }

        public int? ActiveWorkers { get; set; }
        public DateTime? LastSeen { get; set; }
        public double? Percent { get; set; }
        public double? Progress { get; set; }
        public string HashrateReported { get; set; }
        public string HashrateCurrent { get; set; }
        public string HashrateAverage { get; set; }
        public string Shares { get; set; }
        public double? Unpaid { get; set; }

        //history
        private DateTime? LastHist { get; set; }
        public double Thickness { get; set; } = 4;
        public PointCollection ReportedHashes { get; set; }
        public PointCollection CurrentHashes { get; set; }
        public double? AverageHashes { get; set; } = -50;
        public double HCwidth { get; set; } = 1000;
        public double HCheight { get; set; } = 100;

        //workers
        public List<string> WorkersNames { get; set; } = new List<string>();
        public List<string> WorkersReported { get; set; } = new List<string>();
        public List<string> WorkersCurrent { get; set; } = new List<string>();
        public List<string> WorkersShares { get; set; } = new List<string>();
        public List<string> WorkersLS { get; set; } = new List<string>();
        public Visibility WVisibility { get; set; } = Visibility.Visible;
        public bool WorkersVisability { get; set; } = false;
        internal void Click(object sender, MouseButtonEventArgs e)
        {
            WorkersVisability = !WorkersVisability;
            ChangeWisability();
        }
        private void ChangeWisability()
        {
            if (WorkersNames.Count > 0 && WorkersVisability)
                WVisibility = Visibility.Visible;
            else WVisibility = Visibility.Collapsed;
        }

        //fonts
        public double FontSizeBig { get; set; } = 16;
        public double FontSizeSmall { get; set; } = 14;
        internal void SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FontSizeBig = e.NewSize.Width / 32;
            FontSizeSmall = FontSizeBig * 14 / 16;
        }
    }
}
