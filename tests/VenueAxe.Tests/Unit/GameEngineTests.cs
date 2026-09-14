using System;
using System.Collections.Generic;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class GameEngineTests
{
    [Fact]
    public void WatlMatchEngine_Initialize_SetsCorrectInitialState()
    {
        var engine = new WatlStandardMatchEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Sarah" },
            new() { Id = "p2", Name = "Marcus" }
        };

        var state = engine.Initialize(matchId, players);

        Assert.Equal(matchId, state.MatchId);
        Assert.Equal("watl_standard", state.GameTypeId);
        Assert.Equal(MatchStatus.InProgress, state.Status);
        Assert.Equal(1, state.CurrentRound);
        Assert.Equal(10, state.TotalRounds);
        Assert.Equal(0, state.CurrentPlayerIndex);
        Assert.Equal(2, state.Players.Count);
        Assert.Equal(0, state.Players[0].Score);
    }

    [Fact]
    public void WatlMatchEngine_RecordThrow_AdvancesTurnsAndCalculatesStreaks()
    {
        var engine = new WatlStandardMatchEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Sarah" },
            new() { Id = "p2", Name = "Marcus" }
        };

        var state = engine.Initialize(matchId, players);

        // Player 1 throws Bullseye (0,0)
        state = engine.RecordThrow(state, 0.0, 0.0, null, isClutchCalled: false);
        Assert.Equal(6, state.Players[0].Score);
        Assert.Equal(1, state.Players[0].BullseyesHit);
        Assert.Equal(1, state.Players[0].Streak);
        Assert.Equal(1, state.CurrentPlayerIndex); // Turn passed to Player 2

        // Player 2 throws 5-ring (0.15, 0.0)
        state = engine.RecordThrow(state, 0.15, 0.0, null, isClutchCalled: false);
        Assert.Equal(5, state.Players[1].Score);
        Assert.Equal(0, state.CurrentPlayerIndex); // Rotated back to Player 1
        Assert.Equal(2, state.CurrentRound);      // Advanced to Round 2
    }

    [Fact]
    public void WatlMatchEngine_CompletesMatchAndDeclaresWinner()
    {
        var engine = new WatlStandardMatchEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Sarah" }
        };

        var state = engine.Initialize(matchId, players, new GameConfig { TotalRounds = 2 });

        // Round 1
        state = engine.RecordThrow(state, 0.0, 0.0, null, false); // 6 pts
        Assert.Equal(MatchStatus.InProgress, state.Status);

        // Round 2 (Final)
        state = engine.RecordThrow(state, 0.0, 0.0, null, false); // 6 pts

        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal("p1", state.WinnerPlayerId);
        Assert.Equal("Sarah", state.WinnerName);
        Assert.Equal(12, state.Players[0].Score);
    }

    [Fact]
    public void CountdownEngine_SubtractsPointsAndHandlesBust()
    {
        var engine = new CountdownGameEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Alex" }
        };

        // Initialize with starting score of 10
        var state = engine.Initialize(matchId, players, new GameConfig { StartingScore = 10 });
        Assert.Equal(10, state.Players[0].Score);

        // Throw 6 pts -> Score becomes 4
        state = engine.RecordThrow(state, 0.0, 0.0, null, false);
        Assert.Equal(4, state.Players[0].Score);

        // Throw 6 pts -> Bust! (4 - 6 < 0). Score remains 4
        state = engine.RecordThrow(state, 0.0, 0.0, null, false);
        Assert.Equal(4, state.Players[0].Score);

        // Throw 4 pts -> Exact 0! Finished & Win
        state = engine.RecordThrow(state, 0.0, 0.22, null, false);
        Assert.Equal(0, state.Players[0].Score);
        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal("p1", state.WinnerPlayerId);
    }

    [Fact]
    public void WatlMatchEngine_UndoLastThrow_RevertsTurnAndScore()
    {
        var engine = new WatlStandardMatchEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Sarah" },
            new() { Id = "p2", Name = "Marcus" }
        };

        var state = engine.Initialize(matchId, players);

        // Player 1 throws Bullseye (6 pts)
        state = engine.RecordThrow(state, 0.0, 0.0, null, false);
        Assert.Equal(6, state.Players[0].Score);
        Assert.Equal(1, state.Players[0].BullseyesHit);
        Assert.Equal(1, state.CurrentPlayerIndex); // Player 2's turn

        // Undo Player 1's throw
        state = engine.UndoLastThrow(state);
        Assert.Equal(0, state.Players[0].Score);
        Assert.Equal(0, state.Players[0].BullseyesHit);
        Assert.Equal(0, state.Players[0].ThrowsTaken);
        Assert.Equal(0, state.CurrentPlayerIndex); // Back to Player 1!
        Assert.Equal(1, state.CurrentRound);
    }

    [Fact]
    public void WatlMatchEngine_TwoKillsAnytime_EnforcesTwoCallsAndScoresEightPoints()
    {
        var engine = new WatlStandardMatchEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Sarah" }
        };

        var state = engine.Initialize(matchId, players);
        Assert.Equal(2, state.Players[0].KillsRemaining);
        Assert.Equal(0, state.Players[0].KillsCalled);

        // Throw 1: Call Kill on Left Killshot (-0.380, 0.460) -> 8 pts, KillsRemaining becomes 1
        state = engine.RecordThrow(state, -0.380, 0.460, null, isClutchCalled: true);
        Assert.Equal(8, state.Players[0].Score);
        Assert.Equal(1, state.Players[0].KillsHit);
        Assert.Equal(1, state.Players[0].KillsCalled);
        Assert.Equal(1, state.Players[0].KillsRemaining);

        // Throw 2: Call Kill on Right Killshot (0.380, 0.460) -> 8 pts, KillsRemaining becomes 0
        state = engine.RecordThrow(state, 0.380, 0.460, null, isClutchCalled: true);
        Assert.Equal(16, state.Players[0].Score);
        Assert.Equal(2, state.Players[0].KillsHit);
        Assert.Equal(2, state.Players[0].KillsCalled);
        Assert.Equal(0, state.Players[0].KillsRemaining);

        // Throw 3: Try to call Kill a 3rd time -> Kills exhausted! Scores 0 as uncalled killshot
        state = engine.RecordThrow(state, -0.380, 0.460, null, isClutchCalled: true);
        Assert.Equal(16, state.Players[0].Score); // Did not award 8 pts because kills were exhausted
        Assert.Equal(2, state.Players[0].KillsHit);
        Assert.Equal(2, state.Players[0].KillsCalled);
        Assert.Equal(0, state.Players[0].KillsRemaining);
    }

    [Fact]
    public void CountdownEngine_UndoLastThrow_RevertsSubtractedPointsAndAllThrows()
    {
        var engine = new CountdownGameEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Alex" }
        };

        var state = engine.Initialize(matchId, players, new GameConfig { StartingScore = 100 });
        Assert.Empty(state.AllThrows);

        // Record throw 1: 6 pts (Score 94)
        state = engine.RecordThrow(state, 0.0, 0.0, null, false);
        Assert.Equal(94, state.Players[0].Score);
        Assert.Single(state.AllThrows);

        // Record throw 2: 5 pts (Score 89)
        state = engine.RecordThrow(state, 0.15, 0.0, null, false);
        Assert.Equal(89, state.Players[0].Score);
        Assert.Equal(2, state.AllThrows.Count);

        // Undo throw 2
        state = engine.UndoLastThrow(state);
        Assert.Equal(94, state.Players[0].Score);
        Assert.Single(state.AllThrows);

        // Undo throw 1
        state = engine.UndoLastThrow(state);
        Assert.Equal(100, state.Players[0].Score);
        Assert.Empty(state.AllThrows);
    }

    [Fact]
    public void KillHunterEngine_BullseyeAndKillshotsScore_OtherRingsZero()
    {
        var engine = new KillHunterEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Hunter" }
        };

        var state = engine.Initialize(matchId, players);

        // Bullseye -> 6 pts
        state = engine.RecordThrow(state, 0.0, 0.0, null, false);
        Assert.Equal(6, state.Players[0].Score);

        // Ring 5 (0.15, 0.0) -> In Kill Hunter, normal rings score 0!
        state = engine.RecordThrow(state, 0.15, 0.0, null, false);
        Assert.Equal(6, state.Players[0].Score);

        // Killshot (-0.380, 0.460) with call -> 8 pts
        state = engine.RecordThrow(state, -0.380, 0.460, null, isClutchCalled: true);
        Assert.Equal(14, state.Players[0].Score);
        Assert.Equal(1, state.Players[0].KillsHit);
    }

    [Fact]
    public void WatlMatchEngine_BullseyeWhenKillshotCalled_ScoresZeroPoints()
    {
        var engine = new WatlStandardMatchEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Sarah" }
        };

        var state = engine.Initialize(matchId, players);

        // Sarah arms/calls killshot in Round 1 (unlimited killshots for testing or via engine)
        state = engine.RecordThrow(state, 0.0, 0.0, TargetZone.Bullseye, isClutchCalled: true);
        Assert.Equal(0, state.Players[0].Score);
        Assert.Equal(0, state.Players[0].BullseyesHit);
        Assert.Equal(TargetZone.Miss, state.LastThrow?.Zone);
    }
}


