using OMineWatcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OMineWatcher.Views
{
    public partial class OmgView : UserControl
    {
        public OmgView()
        {
            InitializeComponent();
            _context = SynchronizationContext.Current;
            OMGindication();

            ((OmgViewModel)DataContext).PropertyChanged += OmgView_PropertyChanged;
        }
        private SynchronizationContext _context;

        private void GPUsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int k = ((OmgViewModel)DataContext).GPUsCountSelected;
            ((OmgViewModel)DataContext).ResetGPUs();
            ComboBox ComB = (ComboBox)sender;
            if (((OmgViewModel)DataContext).GPUsCanSelect)
            {
                GPUswitchSP.Children.Clear();

                if (k < 1)
                {
                    ((OmgViewModel)DataContext).GPUsSwitch = null;
                }
                else
                {
                    ((OmgViewModel)DataContext).GPUsSwitch = new List<bool>();
                    for (int i = 0; i < k; i++)
                    {
                        ((OmgViewModel)DataContext).GPUsSwitch.Add(true);
                    }
                    for (byte n = 0; n < k; n++)
                    {
                        Grid GR = new Grid { Width = 60 };
                        GR.Children.Add(new TextBlock { Text = "GPU" + n, Effect = null, Foreground = Brushes.White });
                        CheckBox CB = new CheckBox
                        {
                            Name = "g" + n.ToString(),
                            Margin = new Thickness(0, 0, 7, 0),
                            IsChecked = true,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        CB.Checked += SwitchGPU;
                        CB.Unchecked += SwitchGPU;
                        GR.Children.Add(CB);
                        GPUswitchSP.Children.Add(GR);
                    }
                    ((OmgViewModel)DataContext).SetGPUsSwitch.Execute(null);
                }
            }
            else
            {
                for (byte n = 0; n < k; n++)
                {
                    Grid GR = new Grid { Width = 60 };
                    GR.Children.Add(new TextBlock { Text = "GPU" + n, Effect = null, Foreground = Brushes.White });
                    CheckBox CB = new CheckBox
                    {
                        Name = "g" + n.ToString(),
                        Margin = new Thickness(0, 0, 7, 0),
                        IsChecked = ((OmgViewModel)DataContext).GPUsSwitch[n],
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    CB.Checked += SwitchGPU;
                    CB.Unchecked += SwitchGPU;
                    GR.Children.Add(CB);
                    GPUswitchSP.Children.Add(GR);
                }
                ((OmgViewModel)DataContext).GPUsCanSelect = true;
            }
        }
        private void SwitchGPU(object sender, RoutedEventArgs e)
        {
            int i = Convert.ToInt32(((CheckBox)sender).Name.Replace("g", ""));
            if (((CheckBox)sender).IsChecked == true)
            {
                ((OmgViewModel)DataContext).GPUsSwitch[i] = true;
            }
            else
            {
                ((OmgViewModel)DataContext).GPUsSwitch[i] = false;
            }
            ((OmgViewModel)DataContext).SetGPUsSwitch.Execute(null);
        }

        private void OmgView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _context.Send((object o) => 
            {
                switch (e.PropertyName)
                {
                    case "GPUs":
                        {
                            int n = ((OmgViewModel)DataContext).GPUs;
                            /*SetGPUs*/ {
                                GPUs.Children.Clear();
                                for (int i = 0; i < n; i++)
                                {
                                    Label Lb = new Label
                                    {
                                        Height = 26,
                                        HorizontalContentAlignment = HorizontalAlignment.Center,
                                        VerticalContentAlignment = VerticalAlignment.Center,
                                        FontFamily = new FontFamily("Consolas"),
                                        FontSize = 14,
                                        Content = $"GPU{i}"
                                    };
                                    GPUs.Children.Add(Lb);
                                }
                            }
                            SetLabels(InfPowerLimits, "InfPowerLimits", n);
                            SetTextBoxes(PowerLimits, "PowerLimits", n);
                            SetLabels(InfCoreClocks, "InfCoreClocks", n);
                            SetTextBoxes(CoreClocks, "CoreClocks", n);
                            SetLabels(InfMemoryClocks, "InfMemoryClocks", n);
                            SetTextBoxes(MemoryClocks, "MemoryClocks", n);
                            SetLabels(InfFanSpeeds, "InfFanSpeeds", n);
                            SetTextBoxes(FanSpeeds, "FanSpeeds", n);
                            SetLabels(InfTemps, "InfTemperatures", n);
                            SetLabels(LogTemperatures, "InfTemperatures", n, "0°C");
                            SetLabels(InfHashrates, "InfHashrates", n, "0.00");
                            SetLabels(LogHashrates, "InfHashrates", n, "0.00");
                        }
                        break;
                    case "Indication":
                        OMGworking = ((OmgViewModel)DataContext).Indication;
                        break;
                    case "Log":
                        if (((OmgViewModel)DataContext).LogAutoscroll) LogScroller.ScrollToEnd();
                        break;
                }
            },
            null);
        }
        private void SetLabels(StackPanel SP, string prop, int length)
        {
            SP.Children.Clear();
            for (int i = 0; i < length; i++)
            {
                Label Lb = new Label
                {
                    Height = 26,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14
                };
                Lb.SetBinding(Label.ContentProperty, $"{prop}[{i}]");
                SP.Children.Add(Lb);
            }
        }
        private void SetLabels(WrapPanel WP, string prop, int length)
        {
            WP.Children.Clear();
            for (int i = 0; i < length; i++)
            {
                Label Lb = new Label
                {
                    Width = 65,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14
                };
                Lb.SetBinding(Label.ContentProperty, $"{prop}[{i}]");
                WP.Children.Add(Lb);
            }
        }
        private void SetLabels(StackPanel SP, string prop, int length, string format)
        {
            SP.Children.Clear();
            for (int i = 0; i < length; i++)
            {
                Label Lb = new Label
                {
                    Height = 26,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    ContentStringFormat = format
                };
                Lb.SetBinding(Label.ContentProperty, $"{prop}[{i}]");
                SP.Children.Add(Lb);
            }
        }
        private void SetLabels(WrapPanel WP, string prop, int length, string format)
        {
            WP.Children.Clear();
            for (int i = 0; i < length; i++)
            {
                Label Lb = new Label
                {
                    Width = 65,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    ContentStringFormat = format
                };
                Lb.SetBinding(Label.ContentProperty, $"{prop}[{i}]");
                WP.Children.Add(Lb);
            }
        }
        private void SetTextBoxes(StackPanel SP, string prop, int length)
        {
            SP.Children.Clear();
            for (int i = 0; i < length; i++)
            {
                TextBox Tb = new TextBox
                {
                    Height = 26,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14
                };
                Tb.SetBinding(TextBox.TextProperty, $"{prop}[{i}]");
                Binding b = new Binding($"{prop}");
                b.Converter = new Styles.ListIntExistToBool();
                Tb.SetBinding(TextBox.IsEnabledProperty, b);
                SP.Children.Add(Tb);
            }
        }

        private static bool? OMGworking;
        private void OMGindication()
        {
            Task.Run(() =>
            {
                OMGworking = false;
                while (OMGworking != null)
                {
                    while (OMGworking == true)
                    {
                        _context.Send((object o) =>
                        {
                            IndicatorEl.Fill = Brushes.Lime;
                            IndicatorEl2.Fill = Brushes.Lime;
                        },
                        null);
                        Thread.Sleep(700);
                        _context.Send((object o) =>
                        {
                            IndicatorEl.Fill = null;
                            IndicatorEl2.Fill = null;
                        },
                        null);
                        Thread.Sleep(300);
                    }
                    while (OMGworking == false)
                    {
                        _context.Send((object o) =>
                        {
                            IndicatorEl.Fill = Brushes.Red;
                            IndicatorEl2.Fill = Brushes.Red;
                        },
                        null);
                        Thread.Sleep(200);
                    }
                }
            });
        }
    }
}
