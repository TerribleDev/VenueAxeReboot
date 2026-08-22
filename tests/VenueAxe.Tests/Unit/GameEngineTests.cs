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
}
