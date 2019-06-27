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

namespace OMineGuard
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
            RigName.Text = Profile.WorkersList[RigsList.SelectedIndex].Name;
            if (Profile.WorkersList[RigsList.SelectedIndex].IP != null)
            {
                RigIP.Text = Profile.WorkersList[RigsList.SelectedIndex].IP.ToString();
            }
            else
            {
                RigIP.Text = "";
            }
            if (Profile.WorkersList[RigsList.SelectedIndex].Port != null)
            {
                RigPort.Text = Profile.WorkersList[RigsList.SelectedIndex].Port.ToString();
            }
            else
            {
                RigPort.Text = "";
            }
            if (Profile.WorkersList[RigsList.SelectedIndex].GPUs == null)
            {
                RigGPUs.SelectedIndex = 0;
            }
            else
            {
                RigGPUs.SelectedItem = Profile.WorkersList[RigsList.SelectedIndex].GPUs.ToString();
            }
            if (Profile.WorkersList[RigsList.SelectedIndex].Miner != null)
            {
                RigMiner.SelectedItem = Profile.WorkersList[RigsList.SelectedIndex].Miner;
            }
            else
            {
                RigMiner.SelectedIndex = -1;
            }

            if (Profile.WorkersList[RigsList.SelectedIndex].Hashrate != null)
            {
                RigHashrate.Text = Profile.WorkersList[RigsList.SelectedIndex].Hashrate.ToString();
            }
            else
            {
                RigHashrate.Text = "";
            }
            WachingSwitch();
        }
        private void PlusRig_Click(object sender, RoutedEventArgs e)
        {
            Profile.WorkersList.Add(new Worker());
            RigsList.ItemsSource = Profile.WorkersList.Select(W => W.Name);
            RigsList.SelectedIndex = Profile.WorkersList.Count - 1;
        }
        private void MinusRig_Click(object sender, RoutedEventArgs e)
        {
            Profile.WorkersList.Remove(Profile.WorkersList[RigsList.SelectedIndex]);
            RigsList.ItemsSource = Profile.WorkersList.Select(W => W.Name);
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
                RigLog.Text = "Нужно ввести имя";
                RigLog.Foreground = Brushes.Red;
            }
            else if (err == 1)
            {
                RigLog.Text = "Нужно ввести IP";
                RigLog.Foreground = Brushes.Red;
            }
            else if (err == 2)
            {
                RigLog.Text = "Неправильный формат IP";
                RigLog.Foreground = Brushes.Red;
            }
            else if (err == 3)
            {
                RigLog.Text = "Неправильный формат порта";
                RigLog.Foreground = Brushes.Red;
            }
            else if (err == 4)
            {
                RigLog.Text = "Слишком большое значение порта";
                RigLog.Foreground = Brushes.Red;
            }
            else if (err == 5)
            {
                RigLog.Text = "Неправильный формат хешрейта";
                RigLog.Foreground = Brushes.Red;
            }
            else if (err == 6)
            {
                RigLog.Text = "Слишком большое значение хешрейта";
                RigLog.Foreground = Brushes.Red;
            }
            else if (err == 7)
            {
                RigLog.Text = "Выберите майнер";
                RigLog.Foreground = Brushes.Red;
            }
            else
            {
                Profile.WorkersList[ind].Name = RigName.Text;
                Profile.WorkersList[ind].IP = ip;
                Profile.WorkersList[ind].Port = port;
                Profile.WorkersList[ind].GPUs = GPUs;
                Profile.WorkersList[ind].Miner = min;
                Profile.WorkersList[ind].Hashrate = hashrate;
                RigsList.ItemsSource = Profile.WorkersList.Select(W => W.Name);
                RigsList.SelectedIndex = ind;

                RigLog.Text = "Успешно Сохранено";
                RigLog.Foreground = Brushes.Lime;
                WachingSwitch();
            }

            if (ClearRigLogTHR != null)
            {
                if (ClearRigLogTHR.IsAlive)
                {
                    ClearRigLogTHR.Abort();
                }
            }

            ClearRigLogTHR = new Thread(() =>
            {
                Thread.Sleep(3000);
                MainContext.Send(ClearRigLog, RigLog);
            });
        }
        Thread ClearRigLogTHR;
        public static void ClearRigLog(object o)
        {
            ((TextBlock)o).Text = "";
        }
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
            bool b1 = Profile.WorkersList[RigsList.SelectedIndex].IP != null;
            bool b2 = Profile.WorkersList[RigsList.SelectedIndex].Port != null;
            bool b3 = Profile.WorkersList[RigsList.SelectedIndex].Miner != null;
            if (b1 & b2 & b3)
            {
                RigWatching.IsEnabled = true;
                if (Profile.WorkersList[RigsList.SelectedIndex].Waching == null ||
                    Profile.WorkersList[RigsList.SelectedIndex].Waching == false)
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
        private void RigWatching_Click(object sender, RoutedEventArgs e)
        {
            if (Profile.WorkersList[RigsList.SelectedIndex].Waching == true)
            {
                RigWatching.Background = Brushes.Crimson;
                Profile.WorkersList[RigsList.SelectedIndex].Waching = false;
            }
            else
            {
                RigWatching.Background = Brushes.Lime;
                Profile.WorkersList[RigsList.SelectedIndex].Waching = true;
            }
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
