using System;
using System.Collections.Generic;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Domain.Entities;

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
