using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Services;

namespace VenueAxe.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("api/admin/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileDto>> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.AuthenticateAsync(request.Email, request.Password);
        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for user {UserEmail}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        await SignInUserAsync(user);
        _logger.LogInformation("User {UserId} ({UserEmail}) authenticated successfully with role {Role} for tenant {TenantId}", user.Id, user.Email, user.Role, user.TenantId);

        return Ok(new UserProfileDto(
            user.Id,
            user.TenantId,
            user.VenueId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.Venue?.Name,
            user.Tenant?.Name
        ));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileDto>> Register([FromBody] RegisterTenantRequest request)
    {
        try
        {
            var user = await _authService.RegisterTenantAsync(request);
            await SignInUserAsync(user);
            _logger.LogInformation("New tenant and owner registered: {UserEmail} (Tenant: {TenantName}, Venue: {VenueName})", request.Email, request.OrganizationName, request.VenueName);

            return Ok(new UserProfileDto(
                user.Id,
                user.TenantId,
                user.VenueId,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                request.VenueName,
                request.OrganizationName
            ));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed for {UserEmail}: {Reason}", request.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task SignInUserAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("TenantId", user.TenantId.ToString())
        };

        if (user.VenueId.HasValue)
        {
            claims.Add(new("VenueId", user.VenueId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            }
        );
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User logged out");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("VenueAxe.Auth", new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Path = "/"
        });
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        return Ok(new UserProfileDto(
            user.Id,
            user.TenantId,
            user.VenueId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.Venue?.Name
        ));
    }
}
