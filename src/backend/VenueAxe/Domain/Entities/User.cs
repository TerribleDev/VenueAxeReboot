using System;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Domain.Entities;

public class User : TenantEntity
{
    public Guid? VenueId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => string.IsNullOrWhiteSpace($"{FirstName} {LastName}") ? Email : $"{FirstName} {LastName}".Trim();
    public UserRole Role { get; set; } = UserRole.Owner;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    public Tenant? Tenant { get; set; }
    public Venue? Venue { get; set; }
}
