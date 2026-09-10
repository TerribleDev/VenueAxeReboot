using System;

namespace VenueAxe.Domain.Common;

/// <summary>
/// Ambient context representing the currently authenticated user and tenant scope.
/// Enforces multi-tenancy across all repository operations.
/// </summary>
public interface IUserContext
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    Guid? VenueId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }

    void SetManualContext(Guid tenantId, Guid? userId = null, Guid? venueId = null);
}
