using System;
using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record CreateUserRequest(
    Guid VenueId,
    string Email,
    string FullName,
    UserRole Role,
    string Password
);