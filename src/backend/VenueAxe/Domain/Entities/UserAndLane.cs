using System;
using System.Collections.Generic;
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
    public UserRole Role { get; set; } = UserRole.Owner;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    public Tenant? Tenant { get; set; }
    public Venue? Venue { get; set; }
}

public class Lane : VenueScopedEntity
{
    public int LaneNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxThrowers { get; set; } = 6;
    public bool IsActive { get; set; } = true;
    public LaneStatus CurrentStatus { get; set; } = LaneStatus.Available;

    public string? TabletPairingCode { get; set; }
    public string? ScreenPairingCode { get; set; }
    public string? TabletDeviceToken { get; set; }
    public string? ScreenDeviceToken { get; set; }
    public DateTimeOffset? LastHeartbeatAt { get; set; }

    public Venue? Venue { get; set; }
    public ICollection<BookingLane> BookingLanes { get; set; } = new List<BookingLane>();
    public ICollection<LaneSession> Sessions { get; set; } = new List<LaneSession>();
}
