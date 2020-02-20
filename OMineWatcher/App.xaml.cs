using EwelinkNet;
using System.Windows;

namespace OMineWatcher
{
    public partial class App : Application
    {
        public static Ewelink Ewelink;
        public static bool Live = true;
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Live = false;
        }
    }
}
