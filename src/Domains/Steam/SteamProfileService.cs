using System;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamOpenIdConnectProvider.Domains.IdentityServer;

namespace SteamOpenIdConnectProvider.Domains.Steam;

public sealed class SteamProfileService(
    UserManager<IdentityUser> userManager,
    IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
    IOptions<SteamConfig> config,
    ILogger<SteamProfileService> logger,
    HttpClient httpClient)
    : IProfileService
{
    private readonly SteamConfig _config = config.Value;

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var sub = context.Subject.GetSubjectId();
        if (string.IsNullOrWhiteSpace(sub))
        {
            return;
        }

        var user = await userManager.FindByIdAsync(sub);
        if (user == null)
        {
            return;
        }

        var principal = await claimsFactory.CreateAsync(user);

        var claims = principal.Claims.ToList();
        claims = claims
            .Where(claim => 
                context.RequestedClaimTypes.Contains(claim.Type))
            .ToList();

        var steamId = sub[SteamConstants.OpenIdUrl.Length..];
        AddClaim(claims, SteamClaims.SteamId, steamId);

        GetPlayerSummariesResponse? userSummary = null;
        try
        {
            userSummary = await GetPlayerSummariesAsync([steamId]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve player summary for SteamID: {SteamID}. Some claims will be missing.", steamId);
        }

        var player = userSummary?.Players.FirstOrDefault();
        if (player != null)
        {
            AddClaim(claims, OpenIdStandardClaims.Picture, player.AvatarFull ?? string.Empty);
            AddClaim(claims, OpenIdStandardClaims.Nickname, player.PersonaName ?? string.Empty);
            AddClaim(claims, OpenIdStandardClaims.PreferredUsername, player.PersonaName ?? string.Empty);
            AddClaim(claims, OpenIdStandardClaims.GivenName, player.RealName ?? string.Empty);
            AddClaim(claims, OpenIdStandardClaims.Website, player.ProfileUrl ?? string.Empty);
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var claim in claims)
            {
                logger.LogDebug("Issued claim {claim}:{value} for {principle}",
                    claim.Type,
                    claim.Value,
                    principal.Identity!.Name);
            }
        }

        context.IssuedClaims = claims;
    }

    private async Task<GetPlayerSummariesResponse> GetPlayerSummariesAsync(IEnumerable<string> steamIds)
    {
        const string EndPoint = $"{SteamConstants.ApiBaseUrl}ISteamUser/GetPlayerSummaries/v0002";

        var appKey = _config.ApplicationKey;
        var steamIdList = string.Join(',', steamIds);

        var url = $"{EndPoint}/?key={appKey}&steamids={steamIdList}";
        var res = await httpClient.GetStringAsync(url);
        var response = JsonSerializer.Deserialize<SteamResponse<GetPlayerSummariesResponse>>(res)!;

        return response.Response;
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var sub = context.Subject.GetSubjectId();
        var user = await userManager.FindByIdAsync(sub);
        context.IsActive = user != null;
    }
    
    private static void AddClaim(List<Claim> claims, string type, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(type, value));
        }
    }
}
