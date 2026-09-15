using System;

namespace VenueAxe.DTOs;

public record TerminalAuthResult(
    Guid LaneId,
    int LaneNumber,
    string LaneName,
    string DeviceToken,
    string TerminalType,
    string? VenueName = null,
    string? VenueIconUrl = null
);