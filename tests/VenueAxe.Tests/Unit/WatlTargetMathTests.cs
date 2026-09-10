using System;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class WatlTargetMathTests
{
    [Fact]
    public void Evaluate_CenterPoint_ReturnsBullseyeSixPoints()
    {
        var result = WatlTargetMath.Evaluate(0.0, 0.0, isClutchCalled: false);

        Assert.Equal(TargetZone.Bullseye, result.Zone);
        Assert.Equal(6, result.Points);
        Assert.False(result.IsClutchHit);
    }

    [Fact]
    public void Evaluate_RingFiveThreshold_ReturnsFivePoints()
    {
        // 0.15 is between 0.097 (Bullseye) and 0.180 (Ring 5)
        var result = WatlTargetMath.Evaluate(0.15, 0.0, isClutchCalled: false);

        Assert.Equal(TargetZone.Ring5, result.Zone);
        Assert.Equal(5, result.Points);
    }

    [Fact]
    public void Evaluate_RingFourThreshold_ReturnsFourPoints()
    {
        // 0.22 is between 0.180 and 0.264
        var result = WatlTargetMath.Evaluate(0.0, 0.22, isClutchCalled: false);

        Assert.Equal(TargetZone.Ring4, result.Zone);
        Assert.Equal(4, result.Points);
    }

    [Fact]
    public void Evaluate_RingThreeThreshold_ReturnsThreePoints()
    {
        // 0.30 is between 0.264 and 0.347
        var result = WatlTargetMath.Evaluate(0.30, 0.0, isClutchCalled: false);

        Assert.Equal(TargetZone.Ring3, result.Zone);
        Assert.Equal(3, result.Points);
    }

    [Fact]
    public void Evaluate_RingTwoThreshold_ReturnsTwoPoints()
    {
        // 0.39 is between 0.347 and 0.430
        var result = WatlTargetMath.Evaluate(0.0, -0.39, isClutchCalled: false);

        Assert.Equal(TargetZone.Ring2, result.Zone);
        Assert.Equal(2, result.Points);
    }

    [Fact]
    public void Evaluate_RingOneThreshold_ReturnsOnePoint()
    {
        // 0.48 is between 0.430 and 0.514
        var result = WatlTargetMath.Evaluate(-0.48, 0.0, isClutchCalled: false);

        Assert.Equal(TargetZone.Ring1, result.Zone);
        Assert.Equal(1, result.Points);
    }

    [Fact]
    public void Evaluate_OutsideAllRings_ReturnsMissZeroPoints()
    {
        // 0.60 is outside 0.514
        var result = WatlTargetMath.Evaluate(0.60, 0.60, isClutchCalled: false);

        Assert.Equal(TargetZone.Miss, result.Zone);
        Assert.Equal(0, result.Points);
    }

    [Fact]
    public void Evaluate_LeftKillshotCalled_ReturnsEightPoints()
    {
        // Left killshot is centered at (-0.380, 0.460) with radius 0.073
        var result = WatlTargetMath.Evaluate(-0.380, 0.460, isClutchCalled: true);

        Assert.Equal(TargetZone.ClutchLeft, result.Zone);
        Assert.Equal(8, result.Points);
        Assert.True(result.IsClutchOrKillshotHit);
    }

    [Fact]
    public void Evaluate_LeftKillshotNotCalled_ReturnsZeroPointsPerWatlRules()
    {
        // In WATL, hitting an uncalled killshot yields 0 points
        var result = WatlTargetMath.Evaluate(-0.380, 0.460, isClutchCalled: false);

        Assert.Equal(TargetZone.ClutchLeft, result.Zone);
        Assert.Equal(0, result.Points);
        Assert.False(result.IsClutchOrKillshotHit);
    }

    [Fact]
    public void Evaluate_RightKillshotCalled_ReturnsEightPoints()
    {
        var result = WatlTargetMath.Evaluate(0.380, 0.460, isClutchCalled: true);

        Assert.Equal(TargetZone.ClutchRight, result.Zone);
        Assert.Equal(8, result.Points);
        Assert.True(result.IsClutchOrKillshotHit);
    }

    [Fact]
    public void Evaluate_DropOrMiss_ReturnsZeroPoints()
    {
        var result = WatlTargetMath.Evaluate(0.70, 0.70, isClutchCalled: false);

        Assert.Equal(TargetZone.Miss, result.Zone);
        Assert.Equal(0, result.Points);
        Assert.False(result.IsKillshotHit);
    }
}
