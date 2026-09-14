using System;
using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record LaneDto(
    Guid Id,
    Guid VenueId,
    int LaneNumber,
    string Name,
    int MaxThrowers,
    LaneStatus CurrentStatus,
    string? TabletPairingCode,
    string? ScreenPairingCode,
    DateTimeOffset? LastHeartbeatAt,
    ActiveSessionSummaryDto? ActiveSession,
    bool IsActive = true,
    NextBookingSummaryDto? NextBookingToday = null,
    NextBookingSummaryDto? CurrentBooking = null
);