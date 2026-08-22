using System;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

public record ThrowEvaluation(
    TargetZone Zone,
    int Points,
    bool IsClutchOrKillshotHit,
    string Description
)
{
    public bool IsClutchHit => IsClutchOrKillshotHit;
    public bool IsKillshotHit => IsClutchOrKillshotHit;
}

/// <summary>
/// Precision World Axe Throwing League (WATL) regulation target calculations.
/// Coordinates are normalized relative to target center (0,0), where board radius is 1.0.
/// Official proportions:
/// - Bullseye (6 pts): radius 0.097
/// - 5 Ring (5 pts): radius 0.180
/// - 4 Ring (4 pts): radius 0.264
/// - 3 Ring (3 pts): radius 0.347
/// - 2 Ring (2 pts): radius 0.430
/// - 1 Ring (1 pt):  radius 0.514
/// - Left Killshot (8 pts):  center (-0.380, 0.460), radius 0.073 (Active only on call)
/// - Right Killshot (8 pts): center (0.380, 0.460), radius 0.073 (Active only on call)
/// </summary>
public static class WatlTargetMath
{
    public const double BullseyeRadius = 0.097;
    public const double Ring5Radius = 0.180;
    public const double Ring4Radius = 0.264;
    public const double Ring3Radius = 0.347;
    public const double Ring2Radius = 0.430;
    public const double Ring1Radius = 0.514;

    public const double KillshotX = 0.380;
    public const double KillshotY = 0.460;
    public const double KillshotRadius = 0.073;

    public static ThrowEvaluation Evaluate(double x, double y, bool isClutchCalled = false)
    {
        // Check Left Killshot
        double distKillshotLeft = Math.Sqrt(Math.Pow(x - (-KillshotX), 2) + Math.Pow(y - KillshotY, 2));
        if (distKillshotLeft <= KillshotRadius)
        {
            if (isClutchCalled)
            {
                return new ThrowEvaluation(TargetZone.ClutchLeft, 8, true, "Killshot Hit! (8 Points)");
            }
            // In WATL, hitting an uncalled killshot is 0 points
            return new ThrowEvaluation(TargetZone.ClutchLeft, 0, false, "Uncalled Killshot (0 Points)");
        }

        // Check Right Killshot
        double distKillshotRight = Math.Sqrt(Math.Pow(x - KillshotX, 2) + Math.Pow(y - KillshotY, 2));
        if (distKillshotRight <= KillshotRadius)
        {
            if (isClutchCalled)
            {
                return new ThrowEvaluation(TargetZone.ClutchRight, 8, true, "Killshot Hit! (8 Points)");
            }
            return new ThrowEvaluation(TargetZone.ClutchRight, 0, false, "Uncalled Killshot (0 Points)");
        }

        // Distance from target center (0, 0)
        double r = Math.Sqrt(x * x + y * y);

        if (r <= BullseyeRadius)
        {
            return new ThrowEvaluation(TargetZone.Bullseye, 6, false, "Bullseye! (6 Points)");
        }
        if (r <= Ring5Radius)
        {
            return new ThrowEvaluation(TargetZone.Ring5, 5, false, "5-Ring (5 Points)");
        }
        if (r <= Ring4Radius)
        {
            return new ThrowEvaluation(TargetZone.Ring4, 4, false, "4-Ring (4 Points)");
        }
        if (r <= Ring3Radius)
        {
            return new ThrowEvaluation(TargetZone.Ring3, 3, false, "3-Ring (3 Points)");
        }
        if (r <= Ring2Radius)
        {
            return new ThrowEvaluation(TargetZone.Ring2, 2, false, "2-Ring (2 Points)");
        }
        if (r <= Ring1Radius)
        {
            return new ThrowEvaluation(TargetZone.Ring1, 1, false, "1-Ring (1 Point)");
        }

        return new ThrowEvaluation(TargetZone.Miss, 0, false, "Miss / Drop (0 Points)");
    }
}

/// <summary>
/// International Axe Throwing Federation (IATF) regulation target calculations.
/// - Bullseye (5 pts): radius 0.14
/// - Middle Ring (3 pts): radius 0.32
/// - Outer Ring (1 pt): radius 0.50
/// - Clutch Left/Right (7 pts): center (+/- 0.38, 0.46), radius 0.073 (Active only on 5th throw call)
/// </summary>
public static class IatfTargetMath
{
    public const double BullseyeRadius = 0.140;
    public const double RingMiddleRadius = 0.320;
    public const double RingOuterRadius = 0.500;

    public const double ClutchX = 0.380;
    public const double ClutchY = 0.460;
    public const double ClutchRadius = 0.073;

    public static ThrowEvaluation Evaluate(double x, double y, bool isClutchCalled)
    {
        double distLeft = Math.Sqrt(Math.Pow(x - (-ClutchX), 2) + Math.Pow(y - ClutchY, 2));
        if (distLeft <= ClutchRadius)
        {
            return isClutchCalled
                ? new ThrowEvaluation(TargetZone.ClutchLeft, 7, true, "IATF Clutch Hit! (7 Points)")
                : new ThrowEvaluation(TargetZone.ClutchLeft, 0, false, "Uncalled Clutch (0 Points)");
        }

        double distRight = Math.Sqrt(Math.Pow(x - ClutchX, 2) + Math.Pow(y - ClutchY, 2));
        if (distRight <= ClutchRadius)
        {
            return isClutchCalled
                ? new ThrowEvaluation(TargetZone.ClutchRight, 7, true, "IATF Clutch Hit! (7 Points)")
                : new ThrowEvaluation(TargetZone.ClutchRight, 0, false, "Uncalled Clutch (0 Points)");
        }

        double r = Math.Sqrt(x * x + y * y);
        if (r <= BullseyeRadius) return new ThrowEvaluation(TargetZone.Bullseye, 5, false, "IATF Bullseye (5 Points)");
        if (r <= RingMiddleRadius) return new ThrowEvaluation(TargetZone.Ring3, 3, false, "IATF Middle Ring (3 Points)");
        if (r <= RingOuterRadius) return new ThrowEvaluation(TargetZone.Ring1, 1, false, "IATF Outer Ring (1 Point)");

        return new ThrowEvaluation(TargetZone.Miss, 0, false, "Miss (0 Points)");
    }
}

