using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace OMineWOL
{
    public static class WOL
    {
        public static byte[] GetMACfromIP(string IP)
        {
            byte[] ab = new byte[6]; int len = ab.Length;
            if (SendARP((int)IPAddress.Parse(IP).Address, 0, ab, ref len) == 0) return ab;
            return null;
        }

        public static void WakeFunction(byte[] MAC_ADDRESS)
        {
            UdpClient UDP = new UdpClient();
            UDP.Connect(new IPAddress(0xffffffff), 0x2fff);
            UDP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0);
            int counter = 0;
            byte[] bytes = new byte[1024];
            for (int y = 0; y < 6; y++) bytes[counter++] = 0xFF;
            for (int y = 0; y < 16; y++) foreach (byte b in MAC_ADDRESS) bytes[counter++] = b;

            UDP.Send(bytes, 1024);
        }

        #region DllImport

        #region GetMAC
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);
        #endregion

        #endregion
    }
}
