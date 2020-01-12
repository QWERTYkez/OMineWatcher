﻿using OMineWatcher.Managers;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace OMineWatcher.ViewModels
{
    public class RigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public MainViewModel _model = new MainViewModel();
        public RigViewModel(MainViewModel MVM, int index)
        {
            RigsWacher.SendInform += inf =>
            {
                if (inf.Index == this.Index)
                {
                    if (inf.RO != null)
                    {
                        if (inf.RO.Indication != null)
                        {
                            SetIndicator(inf.RO.Indication.Value);
                        }
                        if (inf.RO.InfHashrates != null)
                        {
                            Hashrates = inf.RO.InfHashrates;
                            Totalhashrate = inf.RO.InfHashrates.Sum();
                            SetIndicator(true);
                        }
                        if (inf.RO.InfTemperatures != null)
                        {
                            Temperatures = inf.RO.InfTemperatures;
                            TotalTemperature = inf.RO.InfTemperatures.Max();
                        }

                        if (inf.RO.RigInactive != null)
                        {
                            SetIndicator(false);
                            Hashrates = null;
                            Totalhashrate = null;
                            Temperatures = null;
                            TotalTemperature = null;
                        }
                    }
                }
            };

            Index = index;
            _model = MVM;
            _model.PropertyChanged += ModelChanged;
        }
        public void InitializeRigViewModel()
        {
            SetRig(_model.Rigs[Index]);
            _model.Rigs.CollectionChanged += (s, e) =>
            {
                SetRig(_model.Rigs[Index]);
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

        public int Index { get; set; }
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

        public Brush LimeIndicator { get; set; }
        public Brush RedIndicator { get; set; } = Brushes.Red;
        public void SetIndicator(bool b)
        {
            if (b)
            {
                LimeIndicator = Brushes.Lime;
                RedIndicator = null;
            }
            else
            {
                LimeIndicator = null;
                RedIndicator = Brushes.Red;
            }
        }
    }
}