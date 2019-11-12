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
    public partial class RigView : UserControl
    {
        public RigView()
        {
            InitializeComponent();
            _context = SynchronizationContext.Current;

            ((RigViewModel)DataContext).PropertyChanged += RigView_PropertyChanged;
        }
        private SynchronizationContext _context;
        private void RigView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _context.Send((object o) => 
            {
                switch (e.PropertyName)
                {
                    case "GPUs":
                        {
                        }
                        break;
                }
            },
            null);
        }
    }
}
