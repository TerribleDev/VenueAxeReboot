using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

public record ThrowEvaluation(
    TargetZone Zone,
    int Points,
    bool IsKillshotHit,
    string Description
)
{
    // Backwards compatibility property
    public bool IsClutchOrKillshotHit => IsKillshotHit;
    public bool IsClutchHit => IsKillshotHit;
}
