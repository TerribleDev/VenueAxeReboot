using System;
using System.Collections.Generic;
using System.Linq;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

public class CountdownGameEngine : IGameEngine
{
    public string GameTypeId => "countdown_603";
    public string DisplayName => "Countdown 603";
    public string Description => "Start at 603. Subtract points with each throw to reach exactly zero!";
    public int DefaultRounds => 15;
    public string Objective => "Start with 603 points and subtract your throw score on every throw. First player to reach exactly zero points wins the match!";
    public string ScoringRules => "Throws reduce your remaining score: Bullseye = -6, Rings 1-5 = -1 to -5, Killshots = -8. Miss / Drop = 0 deduction.";
    public string SpecialRules => "Bust Rule: If a throw exceeds your remaining balance (would take score below zero) or leaves you with 1 point, you BUST! Your score reverts to what it was at the beginning of the turn.";

    public GameStateSnapshot Initialize(Guid matchId, List<GamePlayer> players, GameConfig? config = null)
    {
        int startScore = config?.StartingScore ?? 603;
        var initialPlayers = players.Select(p => new GamePlayer
        {
            Id = p.Id,
            Name = p.Name,
            AvatarColor = p.AvatarColor,
            Score = startScore,
            ThrowsTaken = 0,
            BullseyesHit = 0,
            KillsHit = 0,
            KillsRemaining = 2,
            KillsCalled = 0,
            Streak = 0,
            ThrowHistory = new List<int>()
        }).ToList();

        return new GameStateSnapshot
        {
            MatchId = matchId,
            GameTypeId = GameTypeId,
            GameName = DisplayName,
            Status = MatchStatus.InProgress,
            CurrentRound = 1,
            TotalRounds = config?.TotalRounds ?? DefaultRounds,
            CurrentPlayerIndex = 0,
            Players = initialPlayers
        };
    }

    public GameStateSnapshot RecordThrow(
        GameStateSnapshot state,
        double? x,
        double? y,
        TargetZone? manualZone = null,
        bool isClutchCalled = false)
    {
        if (state.Status != MatchStatus.InProgress || state.Players.Count == 0)
        {
            return state;
        }

        var player = state.Players[state.CurrentPlayerIndex];
        int points = 0;
        TargetZone zone = TargetZone.Miss;
        bool isBullseye = false;

        if (x.HasValue && y.HasValue)
        {
            var eval = WatlTargetMath.Evaluate(x.Value, y.Value, isClutchCalled);
            points = eval.Points;
            zone = eval.Zone;
            isBullseye = eval.Zone == TargetZone.Bullseye;
        }
        else if (manualZone.HasValue)
        {
            zone = manualZone.Value;
            points = zone switch
            {
                TargetZone.Bullseye => 6,
                TargetZone.Ring5 => 5,
                TargetZone.Ring4 => 4,
                TargetZone.Ring3 => 3,
                TargetZone.Ring2 => 2,
                TargetZone.Ring1 => 1,
                TargetZone.KillLeft or TargetZone.KillRight or TargetZone.ClutchLeft or TargetZone.ClutchRight => isClutchCalled ? 8 : 0,
                TargetZone.Drop => 0,
                TargetZone.Miss => 0,
                TargetZone.Fault => 0,
                _ => 0
            };
            isBullseye = zone == TargetZone.Bullseye;
        }

        // Check for Bust
        if (player.Score - points < 0)
        {
            // Bust! Points don't count
            points = 0;
        }
        else
        {
            player.Score -= points;
        }

        player.ThrowsTaken++;
        player.ThrowHistory.Add(points);
        if (isBullseye) player.BullseyesHit++;

        state.LastThrow = new ThrowRecord
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Zone = zone,
            PointsAwarded = points,
            X = x,
            Y = y,
            IsKillCalled = isClutchCalled,
            IsBullseye = isBullseye,
            ThrownAt = DateTimeOffset.UtcNow
        };
        state.AllThrows.Add(state.LastThrow);

        if (player.Score == 0)
        {
            state.Status = MatchStatus.Finished;
            state.WinnerPlayerId = player.Id;
            state.WinnerName = player.Name;
            return state;
        }

        int nextIdx = (state.CurrentPlayerIndex + 1) % state.Players.Count;
        if (nextIdx == 0) state.CurrentRound++;
        state.CurrentPlayerIndex = nextIdx;

        return state;
    }

    public GameStateSnapshot UndoLastThrow(GameStateSnapshot state)
    {
        if (state.Players.Count == 0 || state.AllThrows.Count == 0) return state;

        int prevPlayerIdx = state.CurrentPlayerIndex - 1;
        if (prevPlayerIdx < 0)
        {
            if (state.CurrentRound > 1)
            {
                state.CurrentRound--;
                prevPlayerIdx = state.Players.Count - 1;
            }
            else
            {
                return state;
            }
        }

        var prevPlayer = state.Players[prevPlayerIdx];
        if (prevPlayer.ThrowHistory.Count > 0)
        {
            int lastAwarded = prevPlayer.ThrowHistory[^1];
            prevPlayer.ThrowHistory.RemoveAt(prevPlayer.ThrowHistory.Count - 1);
            prevPlayer.Score += lastAwarded; // add back points subtracted
            prevPlayer.ThrowsTaken = Math.Max(0, prevPlayer.ThrowsTaken - 1);
            if (lastAwarded == 6)
            {
                prevPlayer.BullseyesHit = Math.Max(0, prevPlayer.BullseyesHit - 1);
            }
        }

        state.CurrentPlayerIndex = prevPlayerIdx;
        state.Status = MatchStatus.InProgress;
        state.WinnerPlayerId = null;
        state.WinnerName = null;
        state.AllThrows.RemoveAt(state.AllThrows.Count - 1);
        state.LastThrow = state.AllThrows.LastOrDefault();

        return state;
    }
}
