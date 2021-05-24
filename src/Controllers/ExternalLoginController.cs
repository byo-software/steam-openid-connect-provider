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

namespace SteamOpenIdConnectProvider.Controllers
{
    [AllowAnonymous]
    public class ExternalLoginController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;

        private readonly OpenIdConfig _config;
        private readonly ILogger<ExternalLoginController> _logger;

        public ExternalLoginController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IIdentityServerInteractionService interaction,
            IEventService events,
            IOptions<OpenIdConfig> config,
            ILogger<ExternalLoginController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _config = config.Value;
            _logger = logger;
            _interaction = interaction;
            _events = events;
        }


        [HttpGet("external-login")]
        public Task<IActionResult> ExternalLogin(string returnUrl = null)
        {
            const string provider = "Steam";

            var redirectUrl = Url.Action("ExternalLoginCallback", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Task.FromResult<IActionResult>(new ChallengeResult(provider, properties));
        }

        [HttpGet("external-login-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                throw new Exception($"Error from external provider: {remoteError}");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                throw new Exception($"Error loading external login information.");
            }

            var externalLoginResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (externalLoginResult.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }

            var userName = info.Principal.FindFirstValue(ClaimTypes.Name);
            var userId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId), $"No claim found for {ClaimTypes.NameIdentifier}");
            }

            if (string.IsNullOrEmpty(userName))
            {
                userName = userId.Split('/').Last();
            }

            var user = new IdentityUser { UserName = userName, Id = userId };

            _userManager.UserValidators.Clear();

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
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
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            return Redirect(logout?.PostLogoutRedirectUri ??
                _config.PostLogoutRedirectUris.FirstOrDefault() ??
                Url.Content("~/"));
        }
    }
}
