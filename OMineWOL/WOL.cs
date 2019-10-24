using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OMineWOL
{
    public static class WOL
    {
        public static string GetMACfromIP(string IP)
        {
            byte[] ab = new byte[6]; int len = ab.Length;
            if (SendARP((int)IPAddress.Parse(IP).Address, 0, ab, ref len) == 0)
            {
                return BitConverter.ToString(ab);
            }
            return null;
        }








        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);
    }
}
