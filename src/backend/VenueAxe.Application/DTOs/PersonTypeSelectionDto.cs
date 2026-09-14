namespace VenueAxe.DTOs;

/// <summary>
/// Represents the selected count for a given person type (e.g. adult, minor, first_responder).
/// </summary>
public record PersonTypeSelectionDto(
    string PersonTypeId,
    int Count
);
