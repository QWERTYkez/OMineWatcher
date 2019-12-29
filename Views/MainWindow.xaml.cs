using Microsoft.Xaml.Behaviors;
using OMineGuardControlLibrary;
using OMineWatcher.Managers;
using OMineWatcher.Styles;
using OMineWatcher.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Windows;
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

            OMGcontroller.ControlStart += () => Dispatcher.Invoke(() => OMGcontrolReceived());
            OMGcontroller.ControlEnd += () => Dispatcher.Invoke(() => OMGcontrolLost());

            _ViewModel = (MainViewModel)DataContext;
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
                new View(new Models.OmgModel());
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
            List<Managers.RigStatus?> types = _ViewModel.Indicators;
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
            ObservableCollection<bool> watch = _ViewModel.Watch;
            while (Tumblers.Count != watch.Count)
            {
                if (Tumblers.Count < watch.Count)
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
    }
}