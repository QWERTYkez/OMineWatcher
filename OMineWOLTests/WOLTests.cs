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
            string ip = "192.168.88.11";
            Debug.WriteLine($"{ip} - {WOL.GetMACfromIP(ip)}");
            ip = "192.168.88.12";
            Debug.WriteLine($"{ip} - {WOL.GetMACfromIP(ip)}");
            ip = "192.168.88.113";
            Debug.WriteLine($"{ip} - {WOL.GetMACfromIP(ip)}");
            ip = "192.168.88.14";
            Debug.WriteLine($"{ip} - {WOL.GetMACfromIP(ip)}");
            ip = "192.168.88.15";
            Debug.WriteLine($"{ip} - {WOL.GetMACfromIP(ip)}");
            ip = "192.168.88.21";
            Debug.WriteLine($"{ip} - {WOL.GetMACfromIP(ip)}");
            ip = "192.168.88.31";
            Debug.WriteLine($"{ip} - {WOL.GetMACfromIP(ip)}");
        }
    }
}