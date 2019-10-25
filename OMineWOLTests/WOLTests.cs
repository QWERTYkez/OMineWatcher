using Microsoft.VisualStudio.TestTools.UnitTesting;
using OMineWOL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMineWOL.Tests
{
    [TestClass()]
    public class WOLTests
    {
        [TestMethod()]
        public void GetMACfromIPTest()
        {
            string ip = "ipAddres";
            Debug.WriteLine($"{ip} - {WOL.GetMACfromIP(ip)}");
        }

        [TestMethod()]
        public void WakeFunctionTest()
        {
            byte[] mac = "MacAddres".Split('-').Select(x => Convert.ToByte(x, 16)).ToArray();
            WOL.WakeFunction(mac);
        }
    }
}