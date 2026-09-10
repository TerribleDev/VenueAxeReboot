using System;
using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record UserProfileDto(Guid Id, Guid TenantId, Guid? VenueId, string Email, string FirstName, string LastName, UserRole Role, string? VenueName, string? TenantName = null);