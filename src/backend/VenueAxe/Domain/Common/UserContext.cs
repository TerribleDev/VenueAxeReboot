using System;
using System.Security.Claims;

namespace VenueAxe.Domain.Common;

public class UserContext : IUserContext
{
    public Guid? UserId { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? VenueId { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public void SetManualContext(Guid tenantId, Guid? userId = null, Guid? venueId = null)
    {
        TenantId = tenantId;
        UserId = userId;
        VenueId = venueId;
    }

    public void PopulateFromClaims(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true) return;

        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out var userId)) UserId = userId;

        var tenantClaim = principal.FindFirst("TenantId")?.Value;
        if (Guid.TryParse(tenantClaim, out var tenantId)) TenantId = tenantId;

        var venueClaim = principal.FindFirst("VenueId")?.Value;
        if (Guid.TryParse(venueClaim, out var venueId)) VenueId = venueId;

        Email = principal.FindFirst(ClaimTypes.Email)?.Value;
        Role = principal.FindFirst(ClaimTypes.Role)?.Value;
    }
}
