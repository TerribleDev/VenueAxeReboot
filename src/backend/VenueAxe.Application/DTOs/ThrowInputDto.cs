using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record ThrowInputDto(
    double? X,
    double? Y,
    TargetZone? ManualZone,
    bool IsClutchCalled
);