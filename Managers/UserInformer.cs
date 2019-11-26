using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMineWatcher.Managers
{
    public static class UserInformer
    {
        public static void SendMSG(string sender, string msg)
        {
            Debug.WriteLine($"{sender} >> {msg}");
            // vk informer
        }
    }
}