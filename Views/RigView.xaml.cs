using OMineWatcher.Classes;
using OMineWatcher.Managers;
using OMineWatcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace OMineWatcher.Views
{
    public partial class RigView : UserControl
    {
        public RigView(RigViewModel rvm, int index)
        {
            InitializeComponent();
            DataContext = rvm;
            _ViewModel = rvm;
            Index = index;
            _ViewModel.PropertyChanged += RigView_PropertyChanged;

            _ViewModel.InitializeRigViewModel();

            BaseTempVisual = new DrawingVisual { Effect = new BlurEffect { Radius = 5 } };
            BaseTemp.AddVisual(BaseTempVisual);
        }
        private readonly RigViewModel _ViewModel;

        private DrawingVisual BaseTempVisual;
        public int Index { get; set; }
        private int?[] Temperatures = new int?[1];
        private double?[] Hashrates;
        private readonly object TempKey = new object();
        private void RigView_PropertyChanged(object sender, 
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            Task.Run(() => 
            {
                switch (e.PropertyName)
                {
                    case "Index":
                        {
                            //Index = _ViewModel.Index;
                        }
                        break;
                    case "Temperatures":
                        {
                            lock (TempKey)
                            {
                                int?[] Temps = _ViewModel.Temperatures;

                                if (Temps == null)
                                {
                                    SetTemperature(BaseTempVisual, -1);
                                    while (DVs.Count > 0)
                                    {
                                        RemovelastTempLine();
                                    }
                                    return;
                                }

                                if (Temps != null)
                                {
                                    if (Temps.Length > 0)
                                    {
                                        if (Temps.Max() != Temperatures.Max() || Temps.Min() != Temperatures.Min())
                                        {
                                            SetTemperature(BaseTempVisual, Temps);
                                        }
                                    }
                                    else
                                    {
                                        SetTemperature(BaseTempVisual, -1);
                                    }
                                }
                                while (DVs.Count != Temps.Length)
                                {
                                    if (DVs.Count < Temps.Length)
                                        AddTempLine(DVs.Count);
                                    else
                                        RemovelastTempLine();
                                }
                                for (int i = 0; i < DVs.Count; i++)
                                {
                                    SetTemperature(DVs[i], Temps[i]);
                                }
                                Temperatures = Temps;
                            }
                        }
                        break;
                    case "Hashrates":
                        {
                            Hashrates = _ViewModel.Hashrates;
                        }
                        break;
                    case "MaxTempCurr":
                        {
                            if (MaxTemp != _ViewModel.MaxTempCurr)
                            {
                                MaxTemp = _ViewModel.MaxTempCurr;
                                RedrawAllTemperatures();
                            }
                            else MaxTemp = _ViewModel.MaxTempCurr;
                        }
                        break;
                    case "MinTempCurr":
                        {
                            if (MinTemp != _ViewModel.MinTempCurr)
                            {
                                MinTemp = _ViewModel.MinTempCurr;
                                RedrawAllTemperatures();
                            }
                            else MinTemp = _ViewModel.MinTempCurr;
                        }
                        break;
                }
            });
        }
        private void RedrawAllTemperatures()
        {
            Task.Run(() => 
            {
                SetTemperature(BaseTempVisual, Temperatures);
                for (int i = 0; i < DVs.Count; i++)
                {
                    SetTemperature(DVs[i], Temperatures[i]);
                }
            });
        }

        private int MaxTemp = Settings.GenSets.TotalMaxTemp;
        private int MinTemp = Settings.GenSets.TotalMinTemp;
        private void SetTemperature(DrawingVisual DV, int? curr)
        {
            int maxdigits = MaxTemp - MinTemp;
            int currentTemp;
            if (curr != null) currentTemp = curr.Value - MinTemp;
            else { currentTemp = -1; }

            if (currentTemp < 0) return;

            double DigitSpace = 100.0 / maxdigits;
            double DigitHalf = (DigitSpace * 0.6) / 2;
            double ColorStep = 255 / (maxdigits / 2);

            byte blue = 0;
            byte red = 0;
            byte green = 0;

            var Draws = new List<(double PointA, double PointB, byte Alpha, byte Red, byte Green, byte Blue)>();

            for (int i = 0; i < currentTemp && i < maxdigits; i++)
            {
                if (i < maxdigits / 2)
                    blue = Convert.ToByte(255 - ColorStep * i);
                else blue = 0;
                green = Convert.ToByte(255 - Math.Abs(ColorStep * i - 255));
                if (i < maxdigits / 2)
                    red = Convert.ToByte(ColorStep * i);
                else red = 255;

                double center = (DigitSpace / 2) + i * DigitSpace;

                Draws.Add((center - DigitHalf, center + DigitHalf, 255, red, green, blue));
            }

            Dispatcher.InvokeAsync(() =>
            {
                using (DrawingContext dc = DV.RenderOpen())
                {
                    foreach (var (PointA, PointB, Alpha, Red, Green, Blue) in Draws)
                    {
                        Rect R = new Rect(new Point(PointA, 0), new Point(PointB, 20));
                        Brush B = new SolidColorBrush(Color.FromArgb(Alpha, Red, Green, Blue));
                        dc.DrawRectangle(B, null, R);
                    }
                }
            });
        }
        private void SetTemperature(DrawingVisual DV, int?[] Ccurr)
        {
            int minT;
            int curr;
            try
            {
                minT = Ccurr.Min().Value - MinTemp;
                curr = Ccurr.Max().Value;
            }
            catch 
            {
                minT = -1;
                curr = -1;
            }
            
            int maxdigits = MaxTemp - MinTemp;
            int currentTemp = curr - MinTemp;
            if (currentTemp < 0) return;

            double DigitSpace = 100.0 / maxdigits;
            double DigitHalf = (DigitSpace * 0.6) / 2;
            double ColorStep = 255 / (maxdigits / 2);

            byte alpha = 255;
            byte blue = 0;
            byte red = 0;
            byte green = 0;

            var Draws = new List<(double PointA, double PointB, byte Alpha, byte Red, byte Green, byte Blue)>();

            for (int i = 0; i < currentTemp && i < maxdigits; i++)
            {
                if (Ccurr.Length > 1)
                {
                    if (i < minT)
                    {
                        alpha = 50;
                    }
                    else
                    {
                        alpha = 255;
                    }
                }

                if (i < maxdigits / 2)
                    blue = Convert.ToByte(255 - ColorStep * i);
                else blue = 0;
                green = Convert.ToByte(255 - Math.Abs(ColorStep * i - 255));
                if (i < maxdigits / 2)
                    red = Convert.ToByte(ColorStep * i);
                else red = 255;

                double center = (DigitSpace / 2) + i * DigitSpace;

                Draws.Add((center - DigitHalf, center + DigitHalf, alpha, red, green, blue));
            }

            Dispatcher.InvokeAsync(() => 
            {
                using (DrawingContext dc = DV.RenderOpen())
                {
                    foreach (var (PointA, PointB, Alpha, Red, Green, Blue) in Draws)
                    {
                        Rect R = new Rect(new Point(PointA, 0), new Point(PointB, 20));
                        Brush B = new SolidColorBrush(Color.FromArgb(Alpha, Red, Green, Blue));
                        dc.DrawRectangle(B, null, R);
                    }
                }
            });
        }

        private void BaseGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BaseGrid.Background = Brushes.Cyan;
        }
        private void BaseGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BaseGrid.Background = Brushes.DarkSlateGray;
        }
        private bool Detaled = false;
        private void BaseGrid_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bool b1 = Temperatures != null ? (Temperatures.Length > 1 ? true : false) : false;
            bool b2 = Hashrates != null ? (Hashrates.Length > 1 ? true : false) : false;

            if (b1 || b2)
            {
                if (Detaled)
                {
                    Detaled = !Detaled;
                    BaseTempBox.Visibility = Visibility.Visible;
                    DetaledTempBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Detaled = !Detaled;
                    BaseTempBox.Visibility = Visibility.Collapsed;
                    DetaledTempBox.Visibility = Visibility.Visible;
                }
            }
        }

        private readonly List<DrawingVisual> DVs = new List<DrawingVisual>();
        private void AddTempLine(int index)
        {
            Dispatcher.Invoke(() => 
            {
                {
                    Grid grd = new Grid();
                    grd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
                    grd.ColumnDefinitions.Add(new ColumnDefinition());
                    grd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                    {
                        Viewbox vb1 = new Viewbox { Height = 20 };
                        Grid.SetColumn(vb1, 0);
                        {
                            Label l = new Label
                            {
                                ContentStringFormat = "0.00",
                                FontSize = 12,
                                FontWeight = FontWeights.Bold,
                                Foreground = Brushes.Lime
                            };
                            l.SetBinding(Label.ContentProperty, $"Hashrates[{index}]");
                            vb1.Child = l;
                        }

                        Viewbox vb2 = new Viewbox { Height = 16, Stretch = Stretch.Fill };
                        Grid.SetColumn(vb2, 1);
                        {
                            DrawingCanvas DC = new DrawingCanvas
                            {
                                Width = 100,
                                Height = 20
                            };

                            var DV = new DrawingVisual { Effect = new BlurEffect { Radius = 5 } };

                            DC.AddVisual(DV);
                            DVs.Add(DV);
                            vb2.Child = DC;
                        }

                        Viewbox vb3 = new Viewbox { Height = 20 };
                        Grid.SetColumn(vb3, 2);
                        {
                            Label l = new Label
                            {
                                ContentStringFormat = "0℃",
                                FontSize = 12,
                                FontWeight = FontWeights.Bold,
                                Foreground = Brushes.Yellow
                            };
                            l.SetBinding(Label.ContentProperty, $"Temperatures[{index}]");
                            vb3.Child = l;
                        }

                        grd.Children.Add(vb1);
                        grd.Children.Add(vb2);
                        grd.Children.Add(vb3);
                    }
                    DetaledTemperaturesControl.Items.Add(grd);
                }
            });
        }
        private readonly object DetaledTemperaturesKey = new object();
        private void RemovelastTempLine()
        {
            lock (DetaledTemperaturesKey)
            {
                var i = DVs.Count - 1;
                DVs.RemoveAt(i);
                Dispatcher.Invoke(() => DetaledTemperaturesControl.Items.RemoveAt(i));
            }
        }
    }
}
