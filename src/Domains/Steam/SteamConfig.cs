using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamOpenIdConnectProvider.Domains.Steam
{
    public class SteamConfig
    {
        public static readonly string Key = "Steam";

        public string ApplicationKey { get; internal set; }
    }
}
