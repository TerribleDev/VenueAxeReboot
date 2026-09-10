namespace VenueAxe.DTOs;

public record CreateLaneRequest(
    int LaneNumber,
    string Name,
    int MaxThrowers = 6
);