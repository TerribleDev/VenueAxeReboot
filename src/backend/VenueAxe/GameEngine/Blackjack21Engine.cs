using System;
using System.Collections.Generic;
using System.Linq;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

/// <summary>
/// Blackjack 21: Target exactly 21 points!
/// Going over 21 is a BUST that penalizes the thrower back to 11.
/// First player to reach exactly 21 wins instantly.
/// </summary>
public class Blackjack21Engine : IGameEngine
{
    public string GameTypeId => "blackjack_21";
    public string DisplayName => "Blackjack 21";
    public string Description => "Target exactly 21! Standard rings add 1-6 points (Clutch adds 7). Over 21 busts back to 11. First to 21 wins!";
    public int DefaultRounds => 10;

    public GameStateSnapshot Initialize(Guid matchId, List<GamePlayer> players, GameConfig? config = null)
    {
        var activePlayers = players.Count > 0
            ? players.Select(p => new GamePlayer
            {
                Id = p.Id,
                Name = p.Name,
                AvatarColor = p.AvatarColor,
                Score = 0,
                ThrowsTaken = 0,
                BullseyesHit = 0,
                ClutchesHit = 0,
                Streak = 0,
                ThrowHistory = new()
            }).ToList()
            : new List<GamePlayer> { new() { Id = Guid.NewGuid().ToString(), Name = "Player 1", Score = 0 } };

        return new GameStateSnapshot
        {
            MatchId = matchId,
            GameTypeId = GameTypeId,
            GameName = DisplayName,
            Status = MatchStatus.InProgress,
            CurrentRound = 1,
            TotalRounds = config?.TotalRounds ?? DefaultRounds,
            CurrentPlayerIndex = 0,
            Players = activePlayers,
            AllThrows = new()
        };
    }

    public GameStateSnapshot RecordThrow(
        GameStateSnapshot state,
        double? x,
        double? y,
        TargetZone? manualZone,
        bool isClutchCalled)
    {
        if (state.Status != MatchStatus.InProgress || state.Players.Count == 0)
            return state;

        var player = state.Players[state.CurrentPlayerIndex];
        TargetZone zone;
        int hitPoints;

        if (x.HasValue && y.HasValue)
        {
            var hit = WatlTargetMath.Evaluate(x.Value, y.Value, isClutchCalled);
            zone = hit.Zone;
            hitPoints = hit.Points;
        }
        else
        {
            zone = manualZone ?? TargetZone.Miss;
            hitPoints = zone switch
            {
                TargetZone.Bullseye => 6,
                TargetZone.Ring5 => 5,
                TargetZone.Ring4 => 4,
                TargetZone.Ring3 => 3,
                TargetZone.Ring2 => 2,
                TargetZone.Ring1 => 1,
                TargetZone.ClutchLeft or TargetZone.ClutchRight => isClutchCalled ? 7 : 0,
                _ => 0
            };
        }

        int prevScore = player.Score;
        int nextScore = prevScore + hitPoints;

        if (nextScore == 21)
        {
            // Exact 21!
            player.Score = 21;
            player.Streak++;
        }
        else if (nextScore > 21)
        {
            // BUST! Penalized to 11
            player.Score = 11;
            player.Streak = 0;
        }
        else
        {
            player.Score = nextScore;
            if (hitPoints > 0) player.Streak++;
            else player.Streak = 0;
        }

        player.ThrowsTaken++;
        player.ThrowHistory.Add(hitPoints);
        if (zone == TargetZone.Bullseye) player.BullseyesHit++;
        if (zone == TargetZone.ClutchLeft || zone == TargetZone.ClutchRight) player.ClutchesHit++;

        var throwRecord = new ThrowRecord
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Zone = zone,
            PointsAwarded = hitPoints,
            X = x,
            Y = y,
            IsClutchCalled = isClutchCalled,
            IsBullseye = (zone == TargetZone.Bullseye),
            ThrownAt = DateTimeOffset.UtcNow
        };

        state.LastThrow = throwRecord;
        state.AllThrows.Add(throwRecord);

        // Win condition: exact 21
        if (player.Score == 21)
        {
            state.Status = MatchStatus.Finished;
            state.WinnerPlayerId = player.Id;
            state.WinnerName = player.Name;
            return state;
        }

        // Advance turns
        int nextPlayerIdx = (state.CurrentPlayerIndex + 1) % state.Players.Count;
        if (nextPlayerIdx == 0)
        {
            state.CurrentRound++;
            if (state.CurrentRound > state.TotalRounds)
            {
                state.Status = MatchStatus.Finished;
                // Winner is player closest to 21 without busting (highest score <= 21)
                var winner = state.Players.OrderByDescending(p => p.Score).FirstOrDefault();
                state.WinnerPlayerId = winner?.Id;
                state.WinnerName = winner?.Name;
                return state;
            }
        }

        state.CurrentPlayerIndex = nextPlayerIdx;
        return state;
    }

    public GameStateSnapshot UndoLastThrow(GameStateSnapshot state)
    {
        if (state.AllThrows.Count == 0) return state;

        state.AllThrows.RemoveAt(state.AllThrows.Count - 1);

        // Cleanly recompute all player scores from scratch from remaining AllThrows to handle any busts
        foreach (var p in state.Players)
        {
            p.Score = 0;
            p.ThrowsTaken = 0;
            p.BullseyesHit = 0;
            p.ClutchesHit = 0;
            p.Streak = 0;
            p.ThrowHistory.Clear();
        }

        foreach (var t in state.AllThrows)
        {
            var p = state.Players.FirstOrDefault(pl => pl.Id == t.PlayerId);
            if (p != null)
            {
                int nextScore = p.Score + t.PointsAwarded;
                p.Score = nextScore > 21 ? 11 : nextScore;
                p.ThrowsTaken++;
                p.ThrowHistory.Add(t.PointsAwarded);
                if (t.IsBullseye) p.BullseyesHit++;
                if (t.IsClutchCalled) p.ClutchesHit++;
            }
        }

        int totalThrows = state.AllThrows.Count;
        state.CurrentPlayerIndex = totalThrows % Math.Max(1, state.Players.Count);
        state.CurrentRound = Math.Max(1, (totalThrows / Math.Max(1, state.Players.Count)) + 1);

        state.Status = MatchStatus.InProgress;
        state.WinnerPlayerId = null;
        state.WinnerName = null;
        state.LastThrow = state.AllThrows.Count > 0 ? state.AllThrows[^1] : null;

        return state;
    }
}
