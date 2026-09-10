using System;

namespace VenueAxe.DTOs;

public record VenueDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Slug,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? Phone,
    string? Email,
    string Timezone,
    string Currency,
    string BusinessHoursJson,
    string BrandingConfigJson
);