using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record UpdateLaneRequest(
    int LaneNumber,
    string Name,
    int MaxThrowers,
    LaneStatus Status
);