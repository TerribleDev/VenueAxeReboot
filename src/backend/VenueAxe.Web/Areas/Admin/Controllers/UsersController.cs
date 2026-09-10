using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Repositories;
using VenueAxe.Services;

namespace VenueAxe.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("api/admin/users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public UsersController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet("venue/{venueId:guid}")]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetUsersForVenue(Guid venueId)
    {
        var users = await _uow.Users.GetAllAsync();
        var venueUsers = users
            .Where(u => u.VenueId == venueId)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.VenueId,
                u.Email,
                u.FullName,
                u.Role,
                u.IsActive,
                u.LastLoginAt,
                u.CreatedAt
            ))
            .ToList();

        return Ok(venueUsers);
    }

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var existing = await _uow.Users.GetByEmailAsync(request.Email.Trim().ToLower());
        if (existing != null)
        {
            return BadRequest(new { message = "A user with this email address already exists." });
        }

        var venue = await _uow.Venues.GetByIdAsync(request.VenueId);
        if (venue == null) return NotFound(new { message = "Venue not found" });

        var passwordHash = PasswordHelper.HashPassword(request.Password);

        var nameParts = (request.FullName ?? string.Empty).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : request.Email;
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        var user = new User
        {
            Id = UuidV7.NewGuid(),
            TenantId = venue.TenantId,
            VenueId = venue.Id,
            Email = request.Email.Trim().ToLower(),
            FirstName = firstName,
            LastName = lastName,
            Role = request.Role,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        var dto = new UserSummaryDto(
            user.Id,
            user.VenueId,
            user.Email,
            user.FullName,
            user.Role,
            user.IsActive,
            user.LastLoginAt,
            user.CreatedAt
        );

        return Ok(dto);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.Role = request.Role;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();

        return Ok(new { id, role = user.Role.ToString() });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();

        return Ok(new { message = "User deactivated" });
    }
}
