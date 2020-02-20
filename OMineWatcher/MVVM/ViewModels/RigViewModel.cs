using OMineWatcher.Managers;
using OMineWatcher.Rigs;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;

namespace OMineWatcher.MVVM.ViewModels
{
    public class RigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private MainViewModel _model;
        public RigViewModel(MainViewModel MVM, int index)
        {
            Index = index;
            _model = MVM;
            SetEvents();
            _model.PropertyChanged += ModelChanged;
        }
        public void SetEvents()
        {
            var rig = _model.Rigs.Where(r => r.Index == Index).First();

            rig.InformReceived += inf => Task.Run(() =>
            {
                if (inf != null)
                {
                    if (inf.InfHashrates != null)
                    {
                        Hashrates = inf.InfHashrates;
                        Totalhashrate = inf.InfHashrates.Sum();
                    }
                    if (inf.InfTemperatures != null)
                    {
                        Temperatures = inf.InfTemperatures;
                        TotalTemperature = inf.InfTemperatures.Max();
                    }

                    if (inf.RigInactive != null)
                    {
                        if (Hashrates != null)
                            Hashrates = new double?[Hashrates.Length];
                        Totalhashrate = null;
                        if (Temperatures != null)
                            Temperatures = new int?[Temperatures.Length];
                        TotalTemperature = null;
                    }
                }
            });
            SetIndicator(rig.CurrentStatus);
            rig.Status2Changed += s => Task.Run(() => { SetIndicator(s); });
        }
        public void InitializeRigViewModel()
        {
            SetRig(_model.Rigs[Index]);
            _model.Rigs.CollectionChanged += (s, e) =>
            {
                try
                {
                    SetRig(_model.Rigs.Where(r => r.Index == Index).First());
                }
                catch { }
            };
        }

        private void SetRig(Settings.Rig R)
        {
            RigName = R.Name; IP = R.IP;

            RMaxTemp = R.MaxTemp != null ? R.MaxTemp.Value : -1;
            RMinTemp = R.MinTemp != null ? R.MinTemp.Value : -1;

            MaxTempCurr = R.MaxTemp != null ? R.MaxTemp.Value : Settings.GenSets.TotalMaxTemp;
            MinTempCurr = R.MinTemp != null ? R.MinTemp.Value : Settings.GenSets.TotalMinTemp;
        }

        public int Index;
        public string IP { get; private set; }
        private void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Rigs":
                    {
                        SetRig(_model.Rigs[Index]);
                    }
                    break;
            }
        }

        private int RMaxTemp;
        public int MaxTempCurr { get; set; }
        private int RMinTemp;
        public int MinTempCurr { get; set; }
        public void SetBaseMaxTemp(int max)
        {
            if(RMaxTemp < 0)
            {
                MaxTempCurr = max;
            }
        }
        public void SetBaseMinTemp(int min)
        {
            if (RMinTemp < 0)
            {
                MinTempCurr = min;
            }
        }

        public string RigName { get; set; }
        public double?[] Hashrates { get; set; }
        public double? Totalhashrate { get; set; }
        public string HashrateType { get; set; }
        public int?[] Temperatures { get; set; }
        public int? TotalTemperature { get; set; }

        public Brush Indicator { get; set; } = Brushes.Red;
        private RigStatus? LastStatus;
        public void SetIndicator(RigStatus s)
        {
            if (LastStatus.HasValue)
            {
                if (LastStatus.Value != s) goto SetInd;
                else return;
            }
            else goto SetInd;
        SetInd:
            var x = LastStatus;
            LastStatus = s;
            switch (s)
            {
                case RigStatus.offline:
                    {
                        Indicator = Brushes.Red;
                        if (x == RigStatus.works) UserInformer.AlarmStart();
                    }
                    break;
                case RigStatus.online:
                    {
                        Indicator = Brushes.Red;
                        if (x == RigStatus.works) UserInformer.AlarmStart();
                    }
                    break;
                case RigStatus.works:
                    {
                        Indicator = Brushes.Lime;
                        UserInformer.AlarmStop();
                    }
                    break;
            }
        }
    }
}