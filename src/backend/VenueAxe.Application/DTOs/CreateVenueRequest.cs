namespace VenueAxe.DTOs;

public record CreateVenueRequest(
    string Name,
    string? Slug,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string? Phone,
    string? Email,
    string Timezone = "America/New_York",
    string Currency = "USD",
    string? IconUrl = null
);