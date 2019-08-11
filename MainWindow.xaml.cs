using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Net;
using PM = OMineWatcher.ProfileManager;

namespace OMineWatcher
{
    public partial class MainWindow : Window
    {
        static SynchronizationContext MainContext = SynchronizationContext.Current;

        public MainWindow()
        {
            InitializeComponent();
            BASE = BaseGrid;
            DETAL = DetalGrid;
            POOLSSET = PoolSet;
            BASESET = BaseSet;

            RigGPUs.ItemsSource = new List<String> {"", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" };
            RigMiner.ItemsSource = new List<Miners> { Miners.Bminer, Miners.Claymore, Miners.GMiner };

            PM.Initialize();
            RigsList.ItemsSource = PM.Profile.WorkersList.Select(W => W.Name);
            RigsList_SelectionChanged(null, null);
        }

        #region BASE
        static Grid BASE;

        #endregion
        #region DETAL
        static Grid DETAL;

        #endregion
        #region RIGSSET

        private void RigsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RigsList.SelectedIndex != -1)
            {
                RigName.IsEnabled = true;
                RigIP.IsEnabled = true;
                RigPort.IsEnabled = true;
                RigMiner.IsEnabled = true;
                RigGPUs.IsEnabled = true;
                RigHashrate.IsEnabled = true;
                RigSave.IsEnabled = true;
                RigName.Text = PM.Profile.WorkersList[RigsList.SelectedIndex].Name;
                if (PM.Profile.WorkersList[RigsList.SelectedIndex].IP != null)
                {
                    RigIP.Text = PM.Profile.WorkersList[RigsList.SelectedIndex].IP;
                }
                else
                {
                    RigIP.Text = "";
                }
                if (PM.Profile.WorkersList[RigsList.SelectedIndex].Port != null)
                {
                    RigPort.Text = PM.Profile.WorkersList[RigsList.SelectedIndex].Port.ToString();
                }
                else
                {
                    RigPort.Text = "";
                }
                if (PM.Profile.WorkersList[RigsList.SelectedIndex].GPUs == null)
                {
                    RigGPUs.SelectedIndex = 0;
                }
                else
                {
                    RigGPUs.SelectedItem = PM.Profile.WorkersList[RigsList.SelectedIndex].GPUs.ToString();
                }
                if (PM.Profile.WorkersList[RigsList.SelectedIndex].Miner != null)
                {
                    RigMiner.SelectedItem = PM.Profile.WorkersList[RigsList.SelectedIndex].Miner;
                }
                else
                {
                    RigMiner.SelectedIndex = -1;
                }

                if (PM.Profile.WorkersList[RigsList.SelectedIndex].Hashrate != null)
                {
                    RigHashrate.Text = PM.Profile.WorkersList[RigsList.SelectedIndex].Hashrate.ToString();
                }
                else
                {
                    RigHashrate.Text = "";
                }
            }
            else
            {
                RigName.Text = "";
                RigName.IsEnabled = false;
                RigIP.Text = "";
                RigIP.IsEnabled = false;
                RigPort.Text = "";
                RigPort.IsEnabled = false;
                RigMiner.SelectedIndex = -1;
                RigMiner.IsEnabled = false;
                RigGPUs.SelectedIndex = -1;
                RigGPUs.IsEnabled = false;
                RigHashrate.Text = "";
                RigHashrate.IsEnabled = false;
                RigSave.IsEnabled = false;
            }
            RigMiner_SelectionChanged(null, null);
            WachingSwitch();
        }
        private void PlusRig_Click(object sender, RoutedEventArgs e)
        {
            Worker Wr = new Worker();
            Wr.Name = "Новый воркер";
            PM.Profile.WorkersList.Add(Wr);
            RigsList.ItemsSource = PM.Profile.WorkersList.Select(W => W.Name);
            RigsList.SelectedIndex = PM.Profile.WorkersList.Count - 1;
        }
        private void MinusRig_Click(object sender, RoutedEventArgs e)
        {
            PM.Profile.WorkersList.Remove(PM.Profile.WorkersList[RigsList.SelectedIndex]);
            RigsList.ItemsSource = PM.Profile.WorkersList.Select(W => W.Name);
        }
        private void RigSave_Click(object sender, RoutedEventArgs e)
        {
            int ind = RigsList.SelectedIndex;
            int err = 0;
            double? hashrate = null;
            if (RigHashrate.Text != "")
            {
                try
                { hashrate = Convert.ToDouble(RigHashrate.Text); }
                catch (FormatException)
                { err = 5; }
                catch (OverflowException)
                { err = 6; }
            }
            Miners? min = null;
            if (RigMiner.SelectedIndex != -1)
            {
                min = (Miners)RigMiner.SelectedItem;
            }
            else { err = 7; }


            int? port = null;
            try
            { port = Convert.ToInt32(RigPort.Text); }
            catch (FormatException)
            { err = 3; }
            catch (OverflowException)
            { err = 4; }
            byte? GPUs = null;
            if ((string)(RigGPUs.SelectedItem) != "")
            {
                GPUs = Convert.ToByte((string)(RigGPUs.SelectedItem));
            }
            IPAddress ip = null;
            try
            { ip = IPAddress.Parse(RigIP.Text); }
            catch (ArgumentNullException)
            { err = 1; }
            catch (FormatException)
            { err = 2; }
            
            if (RigName.Text == "")
            {
                RigSendLog("Нужно ввести имя", Brushes.Red);
            }
            else if (err == 1)
            {
                RigSendLog("Нужно ввести IP", Brushes.Red);
            }
            else if (err == 2)
            {
                RigSendLog("Неправильный формат IP", Brushes.Red);
            }
            else if (err == 3)
            {
                RigSendLog("Неправильный формат порта", Brushes.Red);
            }
            else if (err == 4)
            {
                RigSendLog("Слишком большое значение порта", Brushes.Red);
            }
            else if (err == 5)
            {
                RigSendLog("Неправильный формат хешрейта", Brushes.Red);
            }
            else if (err == 6)
            {
                RigSendLog("Слишком большое значение хешрейта", Brushes.Red);
            }
            else if (err == 7)
            {
                RigSendLog("Выберите майнер", Brushes.Red);
            }
            else
            {
                Worker Wr = PM.Profile.WorkersList[ind];

                Wr.Name = RigName.Text;
                Wr.IP = ip.ToString();
                Wr.Port = port;
                Wr.GPUs = GPUs;
                Wr.Miner = min;
                Wr.Hashrate = hashrate;

                PM.Profile.WorkersList[ind] = Wr;

                RigsList.ItemsSource = PM.Profile.WorkersList.Select(W => W.Name);
                RigsList.SelectedIndex = ind;

                PM.SaveProfile();
                RigSendLog("Успешно Сохранено", Brushes.Lime);
                WachingSwitch();
            }
        }
        #region SendLog
        private void RigSendLog(string message, Brush brush)
        {
            RigLog.Text = message;
            RigLog.Foreground = brush;

            if (ClearRigLogTHR.IsAlive)
            {
                ClearRigLogTHR.Abort();
            }
            ClearRigLogTHR = new Thread(() =>
            {
                Thread.Sleep(2000);
                MainContext.Send(ClearRigLog, RigLog);
            });
            ClearRigLogTHR.Start();
        }
        Thread ClearRigLogTHR = new Thread(() => { });
        public static void ClearRigLog(object o)
        {
            ((TextBlock)o).Text = "";
        }
        #endregion
        private void RigMiner_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RigMiner.SelectedIndex != -1)
            {
                RigHashType.Text = Worker.HashrateTypes[(Miners)RigMiner.SelectedItem];
            }
            else
            {
                RigHashrate.IsEnabled = false;
                RigHashType.Text = "";
            }
        }
        private void WachingSwitch()
        {
            if(RigsList.SelectedIndex != -1)
            {
                bool b1 = PM.Profile.WorkersList[RigsList.SelectedIndex].IP != null;
                bool b2 = PM.Profile.WorkersList[RigsList.SelectedIndex].Port != null;
                bool b3 = PM.Profile.WorkersList[RigsList.SelectedIndex].Miner != null;
                if (b1 & b2 & b3)
                {
                    RigWatching.IsEnabled = true;
                    if (PM.Profile.WorkersList[RigsList.SelectedIndex].Waching == null ||
                        PM.Profile.WorkersList[RigsList.SelectedIndex].Waching == false)
                    {
                        RigWatching.Background = Brushes.Crimson;
                    }
                    else
                    {
                        RigWatching.Background = Brushes.Lime;
                    }

                }
                else
                {
                    RigWatching.IsEnabled = false;
                }
            }
            else
            {
                RigWatching.IsEnabled = false;
            }
        }
        private void RigWatching_Click(object sender, RoutedEventArgs e)
        {
            if (PM.Profile.WorkersList[RigsList.SelectedIndex].Waching == true)
            {
                RigWatching.Background = Brushes.Crimson;

                Worker Wr = PM.Profile.WorkersList[RigsList.SelectedIndex];
                Wr.Waching = false;
                PM.Profile.WorkersList[RigsList.SelectedIndex] = Wr;
            }
            else
            {
                RigWatching.Background = Brushes.Lime;
                Worker Wr = PM.Profile.WorkersList[RigsList.SelectedIndex];
                Wr.Waching = true;
                PM.Profile.WorkersList[RigsList.SelectedIndex] = Wr;
            }
            PM.SaveProfile();
        }

        #endregion
        #region POOLSSET
        static Grid POOLSSET;

        #endregion
        #region BASESET
        static Grid BASESET;






        #endregion

        
    }
}
