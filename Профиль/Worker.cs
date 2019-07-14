using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace OMineGuard
{
    public struct Worker
    {
        public static Dictionary<Miners, string> HashrateTypes = new Dictionary<Miners, string>
        {
            {Miners.Bminer, "h/s" },
            {Miners.Claymore, "Mh/s" },
            {Miners.GMiner, "Sol/s" }
        };

        public String Name;
        public string IP;
        public int? Port;
        public byte? GPUs;
        public Miners? Miner;
        public double? Hashrate;
        public bool? Waching;
    }

    public enum Miners
    {
        Claymore,
        GMiner,
        Bminer
    }
}
