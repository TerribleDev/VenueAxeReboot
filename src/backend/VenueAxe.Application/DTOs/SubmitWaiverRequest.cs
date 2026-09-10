using System;

namespace VenueAxe.DTOs;

public record SubmitWaiverRequest(
    Guid TemplateId,
    Guid? BookingId,
    string SignerFirstName,
    string SignerLastName,
    string SignerEmail,
    string SignerPhone,
    DateOnly DateOfBirth,
    bool IsGuardianSigning,
    string? MinorsCoveredJson,
    string SignatureImagePngBase64,
    string? SignatureVectorSvg,
    string UserAgent,
    string? BookingReference = null
);