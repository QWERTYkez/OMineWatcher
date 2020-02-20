using System.Windows;

namespace OMineWatcher
{
    public partial class App : Application
    {
        public static bool Live = true;
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Live = false;
        }
    }
}
