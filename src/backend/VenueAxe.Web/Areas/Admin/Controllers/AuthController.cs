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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileDto>> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.AuthenticateAsync(request.Email, request.Password);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        await SignInUserAsync(user);

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
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
