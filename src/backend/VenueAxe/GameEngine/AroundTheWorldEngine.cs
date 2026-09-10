using System;
using System.Collections.Generic;
using System.Linq;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

/// <summary>
/// Around The World: Sequential ring progression
/// Sequence: Ring 1 (1) -> Ring 2 (2) -> Ring 3 (3) -> Ring 4 (4) -> Ring 5 (5) -> Bullseye (6) -> Clutch (7)
/// First player to complete all 7 target rings in ascending order wins immediately!
/// </summary>
public class AroundTheWorldEngine : IGameEngine
{
    public string GameTypeId => "around_the_world";
    public string DisplayName => "Around The World";
    public string Description => "Hit rings in ascending order: Ring 1 -> 2 -> 3 -> 4 -> 5 -> Bullseye -> Clutch! First to complete all 7 wins.";
    public int DefaultRounds => 15;

    private static readonly TargetZone[] Sequence =
    {
        TargetZone.Ring1,
        TargetZone.Ring2,
        TargetZone.Ring3,
        TargetZone.Ring4,
        TargetZone.Ring5,
        TargetZone.Bullseye,
        TargetZone.ClutchLeft // Left or Right clutch accepted
    };

    public GameStateSnapshot Initialize(Guid matchId, List<GamePlayer> players, GameConfig? config = null)
    {
        var activePlayers = players.Count > 0
            ? players.Select(p => new GamePlayer
            {
                Id = p.Id,
                Name = p.Name,
                AvatarColor = p.AvatarColor,
                Score = 0, // Score = current completed step (0 to 7)
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
        int currentStep = player.Score; // 0 to 6 is pending target step

        TargetZone hitZone;
        if (x.HasValue && y.HasValue)
        {
            var hit = WatlTargetMath.Evaluate(x.Value, y.Value, isClutchCalled);
            hitZone = hit.Zone;
        }
        else
        {
            hitZone = manualZone ?? TargetZone.Miss;
        }

        bool stepCompleted = false;
        if (currentStep < Sequence.Length)
        {
            var targetZone = Sequence[currentStep];
            if (targetZone == TargetZone.ClutchLeft)
            {
                // Accept either clutch
                stepCompleted = (hitZone == TargetZone.ClutchLeft || hitZone == TargetZone.ClutchRight);
            }
            else
            {
                stepCompleted = (hitZone == targetZone);
            }
        }

        int pointsAwarded = stepCompleted ? 1 : 0;
        if (stepCompleted)
        {
            player.Score++;
            player.Streak++;
            if (hitZone == TargetZone.Bullseye) player.BullseyesHit++;
            if (hitZone == TargetZone.ClutchLeft || hitZone == TargetZone.ClutchRight) player.ClutchesHit++;
        }
        else
        {
            player.Streak = 0;
        }

        player.ThrowsTaken++;
        player.ThrowHistory.Add(pointsAwarded);

        var throwRecord = new ThrowRecord
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Zone = hitZone,
            PointsAwarded = pointsAwarded,
            X = x,
            Y = y,
            IsClutchCalled = isClutchCalled,
            IsBullseye = (hitZone == TargetZone.Bullseye),
            ThrownAt = DateTimeOffset.UtcNow
        };

        state.LastThrow = throwRecord;
        state.AllThrows.Add(throwRecord);

        // Win condition: completed all 7 rings!
        if (player.Score >= Sequence.Length)
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

        var lastThrow = state.AllThrows[^1];
        state.AllThrows.RemoveAt(state.AllThrows.Count - 1);

        var player = state.Players.FirstOrDefault(p => p.Id == lastThrow.PlayerId);
        if (player != null)
        {
            if (player.ThrowHistory.Count > 0)
                player.ThrowHistory.RemoveAt(player.ThrowHistory.Count - 1);

            player.ThrowsTaken = Math.Max(0, player.ThrowsTaken - 1);
            if (lastThrow.PointsAwarded > 0)
            {
                player.Score = Math.Max(0, player.Score - 1);
                if (lastThrow.IsBullseye) player.BullseyesHit = Math.Max(0, player.BullseyesHit - 1);
                if (lastThrow.IsClutchCalled) player.ClutchesHit = Math.Max(0, player.ClutchesHit - 1);
            }

            int pIdx = state.Players.IndexOf(player);
            state.CurrentPlayerIndex = pIdx >= 0 ? pIdx : 0;
        }

        int totalThrows = state.AllThrows.Count;
        state.CurrentRound = Math.Max(1, (totalThrows / Math.Max(1, state.Players.Count)) + 1);

        state.Status = MatchStatus.InProgress;
        state.WinnerPlayerId = null;
        state.WinnerName = null;
        state.LastThrow = state.AllThrows.Count > 0 ? state.AllThrows[^1] : null;

        return state;
    }
}
