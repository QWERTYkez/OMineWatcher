using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OMineWatcher.Managers;

namespace OMineWatcher.Models
{
    public class MainModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public MainModel()
        {
            OMG_TCP.OMGcontrolLost += OMGcontrolLost;
            OMG_TCP.OMGcontrolReceived += OMGcontrolReceived;
        }

        public bool OmgConnection { get; set; } = false;
        public void cmd_SwitchConnect(string IP)
        {
            if (OmgConnection)
            {
                OMG_TCP.OMGcontrolDisconnect();
            }
            else
            {
                OMG_TCP.ConnectToOMG(IP);
            }
        }
        private void OMGcontrolReceived()
        {
            OmgConnection = true;
        }
        private void OMGcontrolLost()
        {
            OmgConnection = false;
        }

        
    }
}
