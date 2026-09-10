namespace VenueAxe.DTOs;

public record SubstitutePlayerRequest(
    string PlayerId,
    string NewName,
    string? NewAvatarColor = null
);