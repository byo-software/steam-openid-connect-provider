using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SteamOpenIdConnectProvider.Profile.Models;

namespace SteamOpenIdConnectProvider.Profile
{
    public class SteamProfileService : IProfileService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IUserClaimsPrincipalFactory<IdentityUser> _claimsFactory;
        private readonly UserManager<IdentityUser> _userManager;

        private async Task<GetPlayerSummariesResponse> GetPlayerSummariesAsync(IEnumerable<string> steamIds)
        {
            const string baseurl = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002";

            var applicationKey = _configuration["Authentication:Steam:ApplicationKey"];
            var url = $"{baseurl}/?key={applicationKey}&steamids={string.Join(',', steamIds)}";

            var res = await _httpClient.GetStringAsync(url);
            var response = JsonConvert.DeserializeObject<SteamResponse<GetPlayerSummariesResponse>>(res);
            return response.Response;
        }

        public SteamProfileService(
            UserManager<IdentityUser> userManager,
            IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
            IConfiguration configuration, 
            HttpClient httpClient)
        {
            _userManager = userManager;
            _claimsFactory = claimsFactory;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            var principal = await _claimsFactory.CreateAsync(user);

            var claims = principal.Claims.ToList();
            claims = claims.Where(claim => context.RequestedClaimTypes.Contains(claim.Type)).ToList();

            const string steamUrl = "https://steamcommunity.com/openid/id/";
            var steamId = sub.Substring(steamUrl.Length);

            var userSummary = await GetPlayerSummariesAsync(new[] { steamId });
            var player = userSummary.Players.FirstOrDefault();

            if (player != null)
            {
                claims.Add(new Claim("picture", player.AvatarFull));
                claims.Add(new Claim("nickname", player.PersonaName));
                claims.Add(new Claim("given_name", player.RealName));
                claims.Add(new Claim("website", player.ProfileUrl));
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