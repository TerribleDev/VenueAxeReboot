namespace VenueAxe.DTOs;

public record UpdateVenueRequest(
    string Name,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string? Phone,
    string? Email,
    string BusinessHoursJson,
    string BrandingConfigJson,
    string? IconUrl = null
);