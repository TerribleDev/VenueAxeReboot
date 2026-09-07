using System;
using System.Collections.Generic;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class ArcadeGameEngineTests
{
    [Fact]
    public void AroundTheWorld_ProgressionAndVictory_WorksCorrectly()
    {
        var engine = new AroundTheWorldEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Alice" },
            new() { Id = "p2", Name = "Bob" }
        };

        var state = engine.Initialize(matchId, players);

        Assert.Equal(0, state.Players[0].Score);
        Assert.Equal(MatchStatus.InProgress, state.Status);

        // Alice needs Ring 1. She throws Bullseye -> miss progression
        state = engine.RecordThrow(state, null, null, TargetZone.Bullseye, false);
        Assert.Equal(0, state.Players[0].Score); // Still 0
        Assert.Equal(1, state.CurrentPlayerIndex); // Bob's turn

        // Bob throws Miss
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false);
        Assert.Equal(0, state.Players[1].Score);

        // Alice hits Ring 1 -> progress to step 1
        state = engine.RecordThrow(state, null, null, TargetZone.Ring1, false);
        Assert.Equal(1, state.Players[0].Score);

        // Bob hits Miss
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false);

        // Alice hits Ring 2, 3, 4, 5, Bullseye, ClutchLeft
        state = engine.RecordThrow(state, null, null, TargetZone.Ring2, false);
        Assert.Equal(2, state.Players[0].Score);
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false); // Bob

        state = engine.RecordThrow(state, null, null, TargetZone.Ring3, false);
        Assert.Equal(3, state.Players[0].Score);
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false); // Bob

        state = engine.RecordThrow(state, null, null, TargetZone.Ring4, false);
        Assert.Equal(4, state.Players[0].Score);
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false); // Bob

        state = engine.RecordThrow(state, null, null, TargetZone.Ring5, false);
        Assert.Equal(5, state.Players[0].Score);
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false); // Bob

        state = engine.RecordThrow(state, null, null, TargetZone.Bullseye, false);
        Assert.Equal(6, state.Players[0].Score);
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false); // Bob

        // Alice hits Clutch -> 7th step completed -> Victory!
        state = engine.RecordThrow(state, null, null, TargetZone.ClutchLeft, true);
        Assert.Equal(7, state.Players[0].Score);
        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal("p1", state.WinnerPlayerId);
        Assert.Equal("Alice", state.WinnerName);

        // Undo final winning throw
        state = engine.UndoLastThrow(state);
        Assert.Equal(6, state.Players[0].Score);
        Assert.Equal(MatchStatus.InProgress, state.Status);
        Assert.Null(state.WinnerPlayerId);
    }

    [Fact]
    public void AxeTicTacToe_ClaimingAndThreeInARowWin_Works()
    {
        var engine = new AxeTicTacToeEngine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Xavier" },
            new() { Id = "p2", Name = "Olivia" }
        };

        var state = engine.Initialize(matchId, players);

        // Xavier claims Top-Left cell 0
        state = engine.RecordThrow(state, null, null, TargetZone.ClutchLeft, false);
        Assert.Equal(1, state.Players[0].Score);

        // Olivia claims Middle-Left cell 3
        state = engine.RecordThrow(state, null, null, TargetZone.Ring4, false);
        Assert.Equal(1, state.Players[1].Score);

        // Xavier claims Top-Center cell 1
        state = engine.RecordThrow(state, null, null, TargetZone.Ring5, false);
        Assert.Equal(2, state.Players[0].Score);

        // Olivia claims Center cell 4
        state = engine.RecordThrow(state, null, null, TargetZone.Bullseye, false);
        Assert.Equal(2, state.Players[1].Score);

        // Xavier claims Top-Right cell 2 -> Completes line [0, 1, 2] -> Wins!
        state = engine.RecordThrow(state, null, null, TargetZone.ClutchRight, false);
        Assert.Equal(3, state.Players[0].Score);
        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal("p1", state.WinnerPlayerId);

        // Undo winning throw
        state = engine.UndoLastThrow(state);
        Assert.Equal(2, state.Players[0].Score);
        Assert.Equal(MatchStatus.InProgress, state.Status);
        Assert.Null(state.WinnerPlayerId);
    }

    [Fact]
    public void Blackjack21_Exact21AndBustPenalty_Works()
    {
        var engine = new Blackjack21Engine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = "Charlie" },
            new() { Id = "p2", Name = "Dana" }
        };

        var state = engine.Initialize(matchId, players);

        // Charlie throws Bullseye (6)
        state = engine.RecordThrow(state, null, null, TargetZone.Bullseye, false);
        Assert.Equal(6, state.Players[0].Score);

        // Dana throws Miss (0)
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false);
        Assert.Equal(0, state.Players[1].Score);

        // Charlie throws Bullseye (6) -> 12
        state = engine.RecordThrow(state, null, null, TargetZone.Bullseye, false);
        Assert.Equal(12, state.Players[0].Score);

        // Dana throws Miss (0)
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false);

        // Charlie throws Ring5 (5) -> 17
        state = engine.RecordThrow(state, null, null, TargetZone.Ring5, false);
        Assert.Equal(17, state.Players[0].Score);

        // Dana throws Miss (0)
        state = engine.RecordThrow(state, null, null, TargetZone.Miss, false);

        // Charlie throws Bullseye (6) -> 17 + 6 = 23 > 21 -> BUST! Penalized to 11
        state = engine.RecordThrow(state, null, null, TargetZone.Bullseye, false);
        Assert.Equal(11, state.Players[0].Score);

        // Undo the bust throw -> Charlie should be restored back to 17!
        // It is now Charlie's turn again to retake his throw.
        state = engine.UndoLastThrow(state);
        Assert.Equal(17, state.Players[0].Score);
        Assert.Equal(MatchStatus.InProgress, state.Status);
        Assert.Equal(0, state.CurrentPlayerIndex); // Charlie's turn again

        // Charlie hits Ring4 (4) -> 17 + 4 = 21 -> Exact 21 Victory!
        state = engine.RecordThrow(state, null, null, TargetZone.Ring4, false);
        Assert.Equal(21, state.Players[0].Score);
        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal("p1", state.WinnerPlayerId);
    }
}
