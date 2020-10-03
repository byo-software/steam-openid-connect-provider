using IdentityServer4.Models;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace IdentityServer.Services
{
    public class ProfileService : IProfileService
    {
        private IConfiguration _configuration;
        private readonly IUserClaimsPrincipalFactory<IdentityUser> _claimsFactory;
        private readonly UserManager<IdentityUser> _userManager;

        private async Task<IPlayerSummary> FetchUserSummary(string steamid64) 
        {
            using (var httpClient = new HttpClient()) 
            {

                var baseurl = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002";
                var applicationKey = _configuration["Authentication:Steam:ApplicationKey"];
                var url = $"{baseurl}/?key={applicationKey}&steamids={steamid64}";

                var res = await httpClient.GetStringAsync(url);
                var json = JsonConvert.DeserializeObject<IPlayerSummary>(res);
                return json;
            }
        }

        public class Player    {
            public string steamid { get; set; } 
            public int communityvisibilitystate { get; set; } 
            public int profilestate { get; set; } 
            public string personaname { get; set; } 
            public int commentpermission { get; set; } 
            public string profileurl { get; set; } 
            public string avatar { get; set; } 
            public string avatarmedium { get; set; } 
            public string avatarfull { get; set; } 
            public string avatarhash { get; set; } 
            public int lastlogoff { get; set; } 
            public int personastate { get; set; } 
            public string realname { get; set; } 
            public string primaryclanid { get; set; } 
            public int timecreated { get; set; } 
            public int personastateflags { get; set; } 
            public string loccountrycode { get; set; } 
        }

        public class Response    {
            public List<Player> players { get; set; } 
        }

        public class IPlayerSummary    {
            public Response response { get; set; } 
        }
 
        public ProfileService(
            UserManager<IdentityUser> userManager, 
            IUserClaimsPrincipalFactory<IdentityUser> claimsFactory, 
            IConfiguration configuration
        )
        {
            _userManager = userManager;
            _claimsFactory = claimsFactory;
            _configuration = configuration;
        }
 
        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            var principal = await _claimsFactory.CreateAsync(user);
 
            var claims = principal.Claims.ToList();
            claims = claims.Where(claim => context.RequestedClaimTypes.Contains(claim.Type)).ToList();
 
            var steamurl = "https://steamcommunity.com/openid/id/";
            var steamid64 = sub.Substring(steamurl.Length);

            var userSummary = await FetchUserSummary(steamid64);
            var player = userSummary.response.players[0];

            if (player != null) 
            {
                claims.Add(new Claim("picture", player.avatarfull));
                claims.Add(new Claim("nickname", player.personaname));
                claims.Add(new Claim("given_name", player.realname));
                claims.Add(new Claim("website", player.profileurl));
            }
            
            context.IssuedClaims = claims;
        }
 
        public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            context.IsActive = user != null;
        }
    }
}