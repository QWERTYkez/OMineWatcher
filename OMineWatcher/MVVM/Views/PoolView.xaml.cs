using OMineWatcher.MVVM.ViewModels;
using System.Windows.Controls;

namespace OMineWatcher.MVVM.Views
{
    public partial class PoolView : UserControl
    {
        public int Index { get; set; }
        public PoolView(PoolViewModel PVM, int index)
        {
            InitializeComponent();

            this.SizeChanged += PVM.SizeChanged;
            Shield.MouseDown += PVM.Click;

            PVM.Dispatcher = this.Dispatcher;

            Index = index;
            DataContext = PVM;
            PVM.PropertyChanged += PVM_PropertyChanged;

            PVM.StartWach();
        }

        private void PVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var VM = sender as PoolViewModel;
            switch (e.PropertyName)
            {
                case "Index": Index = VM.Index; break;
            }
        }
    }
}