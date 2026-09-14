using System;

namespace VenueAxe.DTOs;

public record SubmitWaiverRequest(
    Guid TemplateId,
    Guid? BookingId = null,
    string SignerFirstName = "",
    string SignerLastName = "",
    string SignerEmail = "",
    string? SignerPhone = null,
    DateOnly? DateOfBirth = null,
    bool IsGuardianSigning = false,
    string? MinorsCoveredJson = null,
    string SignatureImagePngBase64 = "",
    string? SignatureVectorSvg = null,
    string? UserAgent = null,
    string? BookingReference = null
);