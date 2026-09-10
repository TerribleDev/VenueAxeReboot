namespace VenueAxe.DTOs;

public record RegisterTenantRequest(
    string OrganizationName,
    string VenueName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? City = null,
    string? Timezone = null
);