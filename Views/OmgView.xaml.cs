using OMineWatcher.ViewModels;
using System;
using System.Collections.Generic;
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
        }

        private void GPUsCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int k = ((OmgViewModels)DataContext).GPUsCountSelected;
            ComboBox ComB = (ComboBox)sender;
            if (((OmgViewModels)DataContext).GPUsCanSelect)
            {
                GPUswitchSP.Children.Clear();

                if (k < 1)
                {
                    ((OmgViewModels)DataContext).GPUsSwitch = null;
                }
                else
                {
                    ((OmgViewModels)DataContext).GPUsSwitch = new List<bool>();
                    for (int i = 0; i < k; i++)
                    {
                        ((OmgViewModels)DataContext).GPUsSwitch.Add(true);
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
                    ((OmgViewModels)DataContext).SetGPUsSwitch.Execute(null);
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
                        IsChecked = ((OmgViewModels)DataContext).GPUsSwitch[n],
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    CB.Checked += SwitchGPU;
                    CB.Unchecked += SwitchGPU;
                    GR.Children.Add(CB);
                    GPUswitchSP.Children.Add(GR);
                }
                ((OmgViewModels)DataContext).GPUsCanSelect = true;
            }
        }
        private void SwitchGPU(object sender, RoutedEventArgs e)
        {
            int i = Convert.ToInt32(((CheckBox)sender).Name.Replace("g", ""));
            if (((CheckBox)sender).IsChecked == true)
            {
                ((OmgViewModels)DataContext).GPUsSwitch[i] = true;
            }
            else
            {
                ((OmgViewModels)DataContext).GPUsSwitch[i] = false;
            }
            ((OmgViewModels)DataContext).SetGPUsSwitch.Execute(null);
        }
    }
}
