using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamOpenIdConnectProvider.Domains.Steam
{
    public static class Constants
    {
        public static readonly string OpenIdUrl = "https://steamcommunity.com/openid/id/";
        public static readonly string ApiUrl = "https://api.steampowered.com/";
        public static readonly string GetPlayerSummariesUrl = ApiUrl + "ISteamUser/GetPlayerSummaries/v0002";
    }
}
