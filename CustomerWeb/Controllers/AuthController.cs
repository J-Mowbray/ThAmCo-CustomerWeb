using System.Security.Claims;
using CustomerWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerWeb.Controllers;

public class AuthController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Login(string returnUrl = "/")
    {
        await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties() { RedirectUri = returnUrl });
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Get Auth0 configuration
        var auth0Domain = _configuration["Auth0:Domain"]?.TrimEnd('/');
        var auth0ClientId = _configuration["Auth0:ClientId"];

        // Determine the return URL based on the environment
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
        var returnUrl = _configuration[$"Auth0:LogoutReturnUrls:{environment}"];

        // Fallback to request-based URL if configuration is not set
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = $"{Request.Scheme}://{Request.Host}/";
        }

        // Construct logout URL
        var logoutUrl = $"https://{auth0Domain}/v2/logout?client_id={auth0ClientId}&returnTo={Uri.EscapeDataString(returnUrl)}";

        // Sign out of both local and OpenID Connect authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

        // Log the logout URL for debugging
        _logger.LogInformation("Logout URL: {LogoutUrl}", logoutUrl);

        // Redirect to Auth0 logout endpoint
        return Redirect(logoutUrl);
    }

    [Authorize]
    public IActionResult Profile()
    {
        return View(new UserProfileViewModel()
        {
            Name = User.Identity?.Name,
            EmailAddress = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
            ProfileImage = User.Claims.FirstOrDefault(c => c.Type == "picture")?.Value
        });
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}