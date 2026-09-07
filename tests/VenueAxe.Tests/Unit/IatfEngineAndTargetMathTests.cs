using System;
using System.Collections.Generic;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class IatfEngineAndTargetMathTests
{
    [Fact]
    public void Evaluate_CenterPoint_ReturnsBullseyeFivePoints()
    {
        var result = IatfTargetMath.Evaluate(0.0, 0.0, isClutchCalled: false);

        Assert.Equal(TargetZone.Bullseye, result.Zone);
        Assert.Equal(5, result.Points);
        Assert.False(result.IsClutchOrKillshotHit);
    }

    [Fact]
    public void Evaluate_WithinBullseyeRadius_ReturnsFivePoints()
    {
        var result = IatfTargetMath.Evaluate(0.10, 0.0, isClutchCalled: false);

        Assert.Equal(TargetZone.Bullseye, result.Zone);
        Assert.Equal(5, result.Points);
    }

    [Fact]
    public void Evaluate_MiddleRingThreshold_ReturnsThreePoints()
    {
        // 0.25 is between 0.14 and 0.32
        var result = IatfTargetMath.Evaluate(0.0, 0.25, isClutchCalled: false);

        Assert.Equal(TargetZone.Ring3, result.Zone);
        Assert.Equal(3, result.Points);
    }

    [Fact]
    public void Evaluate_OuterRingThreshold_ReturnsOnePoint()
    {
        // 0.42 is between 0.32 and 0.50
        var result = IatfTargetMath.Evaluate(0.42, 0.0, isClutchCalled: false);

        Assert.Equal(TargetZone.Ring1, result.Zone);
        Assert.Equal(1, result.Points);
    }

    [Fact]
    public void Evaluate_ClutchCalled_ReturnsSevenPoints()
    {
        // Left Clutch at (-0.38, 0.46)
        var leftResult = IatfTargetMath.Evaluate(-0.38, 0.46, isClutchCalled: true);
        Assert.Equal(TargetZone.ClutchLeft, leftResult.Zone);
        Assert.Equal(7, leftResult.Points);
        Assert.True(leftResult.IsClutchOrKillshotHit);

        // Right Clutch at (0.38, 0.46)
        var rightResult = IatfTargetMath.Evaluate(0.38, 0.46, isClutchCalled: true);
        Assert.Equal(TargetZone.ClutchRight, rightResult.Zone);
        Assert.Equal(7, rightResult.Points);
        Assert.True(rightResult.IsClutchOrKillshotHit);
    }

    [Fact]
    public void Evaluate_ClutchUncalled_ReturnsZeroPoints()
    {
        var result = IatfTargetMath.Evaluate(-0.38, 0.46, isClutchCalled: false);

        Assert.Equal(TargetZone.ClutchLeft, result.Zone);
        Assert.Equal(0, result.Points);
        Assert.False(result.IsClutchOrKillshotHit);
    }

    [Fact]
    public void Evaluate_OffTargetDrop_ReturnsMissZeroPoints()
    {
        var result = IatfTargetMath.Evaluate(0.60, 0.60, isClutchCalled: false);

        Assert.Equal(TargetZone.Miss, result.Zone);
        Assert.Equal(0, result.Points);
    }

    [Fact]
    public void IatfEngine_ProgressesRoundsAndCalculatesWinner()
    {
        var engine = new IatfStandardMatchEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Player A" },
            new() { Id = "p2", Name = "Player B" }
        };

        var state = engine.Initialize(matchId, players);
        Assert.Equal(5, state.TotalRounds);
        Assert.Equal(1, state.CurrentRound);
        Assert.Equal(0, state.CurrentPlayerIndex);

        // Round 1 - Player A throws Bull (5 pts)
        state = engine.RecordThrow(state, 0.0, 0.0, null, false);
        Assert.Equal(5, state.Players[0].Score);
        Assert.Equal(1, state.CurrentPlayerIndex); // Turn moves to Player B

        // Round 1 - Player B throws Middle (3 pts)
        state = engine.RecordThrow(state, 0.0, 0.25, null, false);
        Assert.Equal(3, state.Players[1].Score);
        Assert.Equal(0, state.CurrentPlayerIndex); // Turn loops back to Player A
        Assert.Equal(2, state.CurrentRound);       // Round advances to 2

        // Fast-forward rounds 2, 3, 4
        for (int r = 2; r <= 4; r++)
        {
            state = engine.RecordThrow(state, 0.0, 0.0, null, false);   // Player A: 5 pts
            state = engine.RecordThrow(state, 0.0, 0.25, null, false);  // Player B: 3 pts
        }

        Assert.Equal(5, state.CurrentRound);
        Assert.Equal(MatchStatus.InProgress, state.Status);

        // Round 5 (Clutch round) - Player A calls clutch and hits (7 pts)
        state = engine.RecordThrow(state, 0.38, 0.46, null, true);
        Assert.Equal(5 + 5 + 5 + 5 + 7, state.Players[0].Score); // 27 pts

        // Round 5 - Player B hits outer ring (1 pt)
        state = engine.RecordThrow(state, 0.42, 0.0, null, false);
        Assert.Equal(3 + 3 + 3 + 3 + 1, state.Players[1].Score); // 13 pts

        // Match should now be Finished
        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal("p1", state.WinnerPlayerId);
        Assert.Equal("Player A", state.WinnerName);

        // Test Undo Last Throw on finished match:
        // Undoing Player B's last throw should revert Player B's score from 13 to 12, restore status to InProgress, clear winner
        state = engine.UndoLastThrow(state);
        Assert.Equal(MatchStatus.InProgress, state.Status);
        Assert.Null(state.WinnerPlayerId);
        Assert.Null(state.WinnerName);
        Assert.Equal(12, state.Players[1].Score);
        Assert.Equal(4, state.Players[1].ThrowsTaken);
        Assert.Equal(1, state.CurrentPlayerIndex); // Player B's turn again!
        Assert.Equal(5, state.CurrentRound);

        // Undoing again should undo Player A's clutch throw
        state = engine.UndoLastThrow(state);
        Assert.Equal(20, state.Players[0].Score);
        Assert.Equal(4, state.Players[0].ThrowsTaken);
        Assert.Equal(0, state.Players[0].ClutchesHit);
        Assert.Equal(0, state.CurrentPlayerIndex); // Player A's turn again
        Assert.Equal(5, state.CurrentRound);
    }
}
