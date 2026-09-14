using System;

namespace VenueAxe.DTOs;

public record VenueSquareConfigDto(
    string? ApplicationId = null,
    string? LocationId = null,
    string? Environment = null,
    bool HasAccessToken = false,
    string? MaskedAccessToken = null,
    string? WebhookSignatureKey = null
);

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
    string BrandingConfigJson,
    VenueSquareConfigDto? SquareConfig = null
);