using Microsoft.Xaml.Behaviors;
using OMineGuardControlLibrary;
using OMineWatcher.Managers;
using OMineWatcher.Pools;
using OMineWatcher.Styles;
using OMineWatcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace OMineWatcher.Views
{
    public partial class MainWindow : Window
    {
        public static SynchronizationContext STAContext = SynchronizationContext.Current;
        private static MainViewModel _ViewModel = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();

            _ViewModel._model.controller.ControlStart += () => Dispatcher.Invoke(() => OMGcontrolReceived());
            _ViewModel._model.controller.ControlEnd += () => Dispatcher.Invoke(() => OMGcontrolLost());

            _ViewModel = (MainViewModel)DataContext;
            _ViewModel.PoolsSets.CollectionChanged += PoolsSets_CollectionChanged;
            _ViewModel.PropertyChanged += MainViewModel_PropertyChanged;
            _ViewModel.Watch.CollectionChanged += SetTumblers;
            _ViewModel.InitializeMainViewModel();
        }

        private void OMGcontrolLost()
        {
            OmgControlPresenter.Content = null;
            OmgControlPresenter.Visibility = Visibility.Collapsed;
            OmgDisconnectButton.Visibility = Visibility.Collapsed;
            BaseTabControl.Visibility = Visibility.Visible;
        }
        private void OMGcontrolReceived()
        {
            OmgControlPresenter.Content = 
                new View(new Models.OmgModel(_ViewModel._model.controller));
            OmgControlPresenter.Visibility = Visibility.Visible;
            OmgDisconnectButton.Visibility = Visibility.Visible;
            BaseTabControl.Visibility = Visibility.Collapsed;
        }

        private void MainViewModel_PropertyChanged(object sender, 
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "eWePasswordSend":
                    Dispatcher.Invoke(() => { eWePasswordBox.Password = _ViewModel.eWePasswordSend; });
                    break;
                case "HivePasswordSend":
                    Dispatcher.Invoke(() => { HivePasswordBox.Password = _ViewModel.HivePasswordSend; });
                    break;
                case "Indicators":
                    STAContext.Send(obj => SetIndicators(), null);
                    break;
            }
        }
        private void eWePasswordChanged(object sender, RoutedEventArgs e)
        { _ViewModel.eWePasswordReceive = eWePasswordBox.Password; }
        private void HivePasswordChanged(object sender, RoutedEventArgs e)
        { _ViewModel.HivePasswordReceive = HivePasswordBox.Password; }

        private static List<Ellipse> Indicators { get; set; } = new List<Ellipse>();
        private void SetIndicators()
        {
            if (Indicators.Count == _ViewModel.Indicators.Count) return;
            List<Rigs.RigStatus?> types = _ViewModel.Indicators;
            while (Indicators.Count != types.Count)
            {
                if (Indicators.Count < types.Count)
                {
                    Ellipse E = new Ellipse
                    {
                        Height = 15,
                        Width = 15,
                        Margin = new Thickness(2, 4, 2, 4),
                        Effect = new BlurEffect { Radius = 5 }
                    };
                    Binding b = new Binding($"Indicators[{Indicators.Count}]");
                    b.Converter = new Classes.RigStatusToBrushConverter();
                    E.SetBinding(Ellipse.FillProperty, b);
                    IndicatorsRigsSP.Children.Add(E);
                    Indicators.Add(E);
                }
                else
                {
                    IndicatorsRigsSP.Children.RemoveAt(IndicatorsRigsSP.Children.Count - 1);
                    Indicators.RemoveAt(Indicators.Count - 1);
                }
            }
        }
        private static List<Tumbler> Tumblers { get; set; } = new List<Tumbler>();
        private void SetTumblers(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Tumblers.Count == _ViewModel.Watch.Count) return;
            if (_ViewModel.FreezeWatch) return;
            while (Tumblers.Count != _ViewModel.Watch.Count)
            {
                if (Tumblers.Count < _ViewModel.Watch.Count)
                {
                    Tumbler T = new Tumbler();
                    SetTriggerToTumbler(T, "Checked", "SetWach", Tumblers.Count);
                    SetTriggerToTumbler(T, "Unchecked", "SetWach", Tumblers.Count);

                    T.SetBinding(Tumbler.IsCheckedProperty, $"Watch[{Tumblers.Count}]");
                    WachingRigsSP.Children.Add(T);
                    Tumblers.Add(T);
                }
                else
                {
                    WachingRigsSP.Children.RemoveAt(WachingRigsSP.Children.Count - 1);
                    Tumblers.RemoveAt(Tumblers.Count - 1);
                }
            }
        }

        private void SetTriggerToTumbler(Tumbler T, string EventName, string Command, object Parametr)
        {
            var invokeCommandAction = new InvokeCommandAction { CommandParameter = Parametr };
            var binding = new Binding { Path = new PropertyPath(Command) };
            BindingOperations.SetBinding(invokeCommandAction, InvokeCommandAction.CommandProperty, binding);

            var eventTrigger = new Microsoft.Xaml.Behaviors.EventTrigger { EventName = EventName };
            eventTrigger.Actions.Add(invokeCommandAction);

            var triggers = Interaction.GetTriggers(T);
            triggers.Add(eventTrigger);
        }

        private void PoolsSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            PoolsSeletorsSP.Children.Clear();
            PoolsNamesSP.Children.Clear();
            PoolsTypesSP.Children.Clear();
            PoolsCoinsSP.Children.Clear();
            PoolsWalletsSP.Children.Clear();
            PoolsWachSP.Children.Clear();

            while (PoolsSeletorsSP.Children.Count < _ViewModel.PoolsSets.Count)
            {
                int i = PoolsSeletorsSP.Children.Count;
                var ps = _ViewModel.PoolsSets[i];

                var sel = new Selector
                {
                    index = i,
                    IsChecked = _ViewModel.Selection[i],
                    Height = 26,
                    VerticalAlignment = VerticalAlignment.Center
                };
                sel.Checked += Sel_Checked;
                sel.Unchecked += Sel_Checked;
                PoolsSeletorsSP.Children.Add(sel);

                var nameTb = new TextBox
                {
                    Name = $"nameTb{i}",
                    Text = ps.Name,
                    Height = 26,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontSize = 18
                };
                nameTb.TextChanged += NameTb_TextChanged;
                PoolsNamesSP.Children.Add(nameTb);

                var pooltypeTb = new ComboBox
                {
                    Name = $"pooltypeTb{i}",
                    Height = 26,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    ItemsSource = PoolsWacher.PoolTypes,
                    SelectedItem = ps.Pool
                };
                pooltypeTb.SelectionChanged += PooltypeTb_SelectionChanged;
                PoolsTypesSP.Children.Add(pooltypeTb);

                CoinType[] Coins = null;
                if (ps.Pool != null)
                {
                    Coins = PoolsWacher.Coins[ps.Pool.Value];
                }
                var cointypeTb = new ComboBox
                {
                    Name = $"cointypeTb{i}",
                    Height = 26,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    ItemsSource = Coins,
                    SelectedItem = ps.Coin
                };
                cointypeTb.SelectionChanged += CointypeTb_SelectionChanged;
                PoolsCoinsSP.Children.Add(cointypeTb);

                var walletTb = new TextBox
                {
                    Name = $"walletTb{i}",
                    Text = ps.Wallet,
                    Height = 26,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontSize = 18
                };
                walletTb.TextChanged += WalletTb_TextChanged;
                PoolsWalletsSP.Children.Add(walletTb);

                var wachTb = new Tumbler
                {
                    index = i,
                    IsChecked = ps.Wach,
                    Height = 26,
                    VerticalAlignment = VerticalAlignment.Center
                };
                wachTb.Checked += WachTb_Checked;
                wachTb.Unchecked += WachTb_Checked;
                PoolsWachSP.Children.Add(wachTb);
            }
        }

        private void Sel_Checked(object sender, RoutedEventArgs e)
        {
            var s = sender as Selector;
            _ViewModel.Selection[s.index] = s.IsChecked.Value;

            _ViewModel.PoolsSetsChanged();
        }
        private void NameTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            int i = Convert.ToInt32(tb.Name.Replace("nameTb", ""));
            _ViewModel.PoolsSets[i].Name = tb.Text;

            _ViewModel.PoolsSetsChanged();
        }
        private void PooltypeTb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            int i = Convert.ToInt32(cb.Name.Replace("pooltypeTb", ""));
            _ViewModel.PoolsSets[i].Pool = cb.SelectedItem as PoolType?;

            var cbc = PoolsCoinsSP.Children[i] as ComboBox;
            cbc.ItemsSource = PoolsWacher.Coins[_ViewModel.PoolsSets[i].Pool.Value];

            _ViewModel.PoolsSetsChanged();
        }
        private void CointypeTb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            int i = Convert.ToInt32(cb.Name.Replace("cointypeTb", ""));
            _ViewModel.PoolsSets[i].Coin = cb.SelectedItem as CoinType?;

            _ViewModel.PoolsSetsChanged();
        }
        private void WalletTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            int i = Convert.ToInt32(tb.Name.Replace("walletTb", ""));
            _ViewModel.PoolsSets[i].Wallet = tb.Text;

            _ViewModel.PoolsSetsChanged();
        }
        private void WachTb_Checked(object sender, RoutedEventArgs e)
        {
            var s = sender as Tumbler;
            _ViewModel.PoolsSets[s.index].Wach = s.IsChecked.Value;
            if (s.IsChecked.Value)
                _ViewModel.WachPoolStart(s.index);
            else _ViewModel.WachPoolStop(s.index);

            _ViewModel.PoolsSetsChanged();
        }

        private void StopAlarm(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            UserInformer.AlarmStop();
        }
    }
}