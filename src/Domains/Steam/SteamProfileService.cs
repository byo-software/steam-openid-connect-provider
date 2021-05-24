using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SteamOpenIdConnectProvider.Domains.IdentityServer;
using SteamOpenIdConnectProvider.Domains.Steam;
using SteamOpenIdConnectProvider.Models.Steam;

namespace SteamOpenIdConnectProvider.Services
{
    public class SteamProfileService : IProfileService
    {
        private readonly HttpClient _httpClient;
        private readonly SteamConfig _config;
        private readonly IUserClaimsPrincipalFactory<IdentityUser> _claimsFactory;
        private readonly UserManager<IdentityUser> _userManager;

        public SteamProfileService(
            UserManager<IdentityUser> userManager,
            IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
            IOptions<SteamConfig> config,
            HttpClient httpClient)
        {
            _userManager = userManager;
            _claimsFactory = claimsFactory;
            _config = config.Value;
            _httpClient = httpClient;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            var principal = await _claimsFactory.CreateAsync(user);

            var claims = principal.Claims.ToList();
            claims = claims.Where(claim => context.RequestedClaimTypes.Contains(claim.Type)).ToList();

            var steamId = sub.Substring(Constants.OpenIdUrl.Length);

            var userSummary = await GetPlayerSummariesAsync(new[] { steamId });
            var player = userSummary.Players.FirstOrDefault();

            if (player != null)
            {
                AddClaim(claims, OpenIdStandardClaims.Picture, player.AvatarFull);
                AddClaim(claims, OpenIdStandardClaims.Nickname, player.PersonaName);
                AddClaim(claims, OpenIdStandardClaims.PreferredUsername, player.PersonaName);
                AddClaim(claims, OpenIdStandardClaims.GivenName, player.RealName);
                AddClaim(claims, OpenIdStandardClaims.Website, player.ProfileUrl);
            }

            context.IssuedClaims = claims;
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            context.IsActive = user != null;
        }

        private void AddClaim(List<Claim> claims, string type, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                claims.Add(new Claim(type, value));
            }
        }

        private async Task<GetPlayerSummariesResponse> GetPlayerSummariesAsync(IEnumerable<string> steamIds)
        {
            var url = $"{Constants.GetPlayerSummariesUrl}/?key={_config.ApplicationKey}&steamids={string.Join(',', steamIds)}";
            var res = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<SteamResponse<GetPlayerSummariesResponse>>(res);
            return response.Response;
        }
    }
}
