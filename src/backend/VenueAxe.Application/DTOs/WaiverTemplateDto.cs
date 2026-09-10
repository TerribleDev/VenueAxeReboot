using System;

namespace VenueAxe.DTOs;

public record WaiverTemplateDto(
    Guid Id,
    Guid VenueId,
    int VersionNumber,
    string Title,
    string BodyTextMarkdown,
    string Sha256Hash
);