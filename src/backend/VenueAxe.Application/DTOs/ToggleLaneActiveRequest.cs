namespace VenueAxe.DTOs;

public record ToggleLaneActiveRequest(
    bool? IsActive = null
);