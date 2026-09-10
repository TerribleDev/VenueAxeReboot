using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record UpdateUserRoleRequest(
    UserRole Role
);