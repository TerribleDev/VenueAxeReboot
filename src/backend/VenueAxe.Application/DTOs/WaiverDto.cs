using System;

namespace VenueAxe.DTOs;

public record WaiverDto(
    Guid Id,
    Guid VenueId,
    Guid? BookingId,
    string SignerFirstName,
    string SignerLastName,
    string SignerEmail,
    string SignerPhone,
    DateOnly DateOfBirth,
    bool IsGuardianSigning,
    string? MinorsCoveredJson,
    string SignatureImagePngBase64,
    DateTimeOffset SignedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool IsExpired
);