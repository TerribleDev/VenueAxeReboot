using System;
using VenueAxe.Domain.Common;

namespace VenueAxe.Domain.Entities;

public class Waiver : VenueScopedEntity
{
    public Guid TemplateId { get; set; }
    public Guid? BookingId { get; set; }

    public string SignerFirstName { get; set; } = string.Empty;
    public string SignerLastName { get; set; } = string.Empty;
    public string SignerEmail { get; set; } = string.Empty;
    public string SignerPhone { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public bool IsGuardianSigning { get; set; } = false;

    /// <summary>
    /// Stored as JSONB in PostgreSQL (List of minors: { firstName, lastName, dateOfBirth })
    /// </summary>
    public string? MinorsCoveredJson { get; set; }

    public string SignatureImagePngBase64 { get; set; } = string.Empty;
    public string? SignatureVectorSvg { get; set; }
    public DateTimeOffset SignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddYears(1);
    public string IpAddress { get; set; } = "127.0.0.1";
    public string UserAgent { get; set; } = string.Empty;

    public Venue? Venue { get; set; }
    public WaiverTemplate? Template { get; set; }
    public Booking? Booking { get; set; }
}
