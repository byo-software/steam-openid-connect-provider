using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamOpenIdConnectProvider.Domains.IdentityServer;

namespace SteamOpenIdConnectProvider.Controllers;

[AllowAnonymous]
public sealed class ExternalLoginController(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IIdentityServerInteractionService interaction,
    IEventService events,
    IOptions<OpenIdConfig> config,
    ILogger<ExternalLoginController> logger)
    : Controller
{
    private readonly OpenIdConfig _config = config.Value;


    [HttpGet("external-login")]
    public Task<IActionResult> ExternalLogin(string? returnUrl = null)
    {
        const string Provider = "Steam";

        var redirectUrl = Url.Action("ExternalLoginCallback", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(Provider, redirectUrl);
        return Task.FromResult<IActionResult>(new ChallengeResult(Provider, properties));
    }

    [HttpGet("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallback(
        string? returnUrl = null, 
        string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            throw new Exception($"Error from external provider: {remoteError}");
        }

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            throw new Exception("Error loading external login information.");
        }

        var externalLoginResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, 
            info.ProviderKey, 
            isPersistent: false, 
            bypassTwoFactor: true);
            
        if (externalLoginResult.Succeeded)
        {
            logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name ?? "<null>", info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        var userName = info.Principal.FindFirstValue(ClaimTypes.Name);
        var userId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentNullException(nameof(userId), $"No claim found for {ClaimTypes.NameIdentifier}");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = userId.Split('/').Last();
        }

        var user = new IdentityUser { UserName = userName, Id = userId };

        userManager.UserValidators.Clear();

        var result = await userManager.CreateAsync(user);
        if (result.Succeeded)
        {
            result = await userManager.AddLoginAsync(user, info);
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return BadRequest();
    }

    [HttpGet("external-logout")]
    public async Task<ActionResult> ExternalLogout(string logoutId)
    {
        var logout = await interaction.GetLogoutContextAsync(logoutId);

        if (User.Identity?.IsAuthenticated == true)
        {
            await signInManager.SignOutAsync();
            await events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
        }

        return Redirect(logout?.PostLogoutRedirectUri ??
                        _config.PostLogoutRedirectUris.FirstOrDefault() ??
                        Url.Content("~/"));
    }
}
