using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HiveOS.API;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            HiveClient.HiveAuthentication("", "");
            List<Worker> Workers = HiveClient.GetWorkers();
            MinerInfo? mi = HiveClient.GetWorkerInfo(Workers[0].farm_id, Workers[0].id);

            Console.ReadLine();
        }
    }
}