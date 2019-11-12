using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using OMineWatcher.Models;
using PropertyChanged;

namespace OMineWatcher.ViewModels
{
    public class RigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        readonly RigModel _model = new RigModel();
        public RigViewModel()
        {
            _model.PropertyChanged += ModelChanged;
        }

        private void ModelChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Profile":
                    {
                        
                        
                    }
                    break;
            }
        }
    }
}