using System.ComponentModel;
using OMineWatcher.Models;
using System.Windows.Controls;
using PropertyChanged;

namespace OMineWatcher.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainViewModel
    {
        readonly MainModel _model = new MainModel();
        public MainViewModel()
        {
            _model.PropertyChanged += ModelChanged;

            OMGSwitchConnect = new RelayCommand(obj => _model.cmd_SwitchConnect(MainWindow.This.SettingsRigIP.Text));
        }
        private void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "OmgConnection":
                    if (_model.OmgConnection)
                    {
                        MainWindow.MainContext.Send((object o) => OmgControlView = new Views.OmgView(), null);
                        OmgConnectButtonName = "Отключиться";
                    }
                    else
                    {
                        MainWindow.MainContext.Send((object o) => OmgControlView = null, null);
                        OmgConnectButtonName = "Подключиться";
                    }
                    break;
            }
        }

        public UserControl OmgControlView { get; set; }




        public string OmgConnectButtonName { get; set; } = "Подключиться";
        public RelayCommand OMGSwitchConnect { get; set; }

    }
}
