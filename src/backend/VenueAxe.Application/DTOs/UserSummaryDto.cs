using System;
using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record UserSummaryDto(
    Guid Id,
    Guid? VenueId,
    string Email,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt
);