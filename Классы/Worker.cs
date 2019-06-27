using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace OMineGuard
{
    public class Worker
    {
        public Worker() { }
        public Worker(string name)
        {
            Name = name;
        }

        public String Name { get; set; } = "Новый воркер";
        public IPAddress IP { get; set; }
        public int? Port { get; set; }
        public byte? GPUs { get; set; }
        public Miners? Miner { get; set; }
        public double? Hashrate { get; set; }
        public bool? Waching { get; set; }
    }

    public enum Miners
    {
        Claymore,
        GMiner,
        Bminer
    }
}
