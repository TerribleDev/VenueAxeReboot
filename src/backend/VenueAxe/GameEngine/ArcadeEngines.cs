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

/// <summary>
/// Axe Tic-Tac-Toe: 3x3 territory battle
/// Cells (0..8) mapped to 3x3 target grid:
///   0 | 1 | 2
///   3 | 4 | 5
///   6 | 7 | 8
/// Claiming 3-in-a-row (horizontal, vertical, diagonal) wins instantly!
/// </summary>
public class AxeTicTacToeEngine : IGameEngine
{
    public string GameTypeId => "axe_tictactoe";
    public string DisplayName => "Axe Tic-Tac-Toe";
    public string Description => "Interactive 3x3 territory grid! Hit cells to claim territory and connect 3 in a row to win.";
    public int DefaultRounds => 9;

    private static readonly int[][] WinningLines =
    {
        new[] { 0, 1, 2 },
        new[] { 3, 4, 5 },
        new[] { 6, 7, 8 },
        new[] { 0, 3, 6 },
        new[] { 1, 4, 7 },
        new[] { 2, 5, 8 },
        new[] { 0, 4, 8 },
        new[] { 2, 4, 6 }
    };

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

    public static int MapCoordinatesToCell(double x, double y)
    {
        int col = x < -0.16 ? 0 : (x <= 0.16 ? 1 : 2);
        int row = y > 0.16 ? 0 : (y >= -0.16 ? 1 : 2);
        return (row * 3) + col;
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

        // Determine cell index 0 to 8
        int cellIndex = 4; // Default to center
        TargetZone zone = TargetZone.Bullseye;

        if (x.HasValue && y.HasValue)
        {
            cellIndex = MapCoordinatesToCell(x.Value, y.Value);
            var hit = WatlTargetMath.Evaluate(x.Value, y.Value, isClutchCalled);
            zone = hit.Zone;
        }
        else if (manualZone.HasValue)
        {
            zone = manualZone.Value;
            cellIndex = zone switch
            {
                TargetZone.ClutchLeft => 0,
                TargetZone.Ring5 => 1,
                TargetZone.ClutchRight => 2,
                TargetZone.Ring4 => 3,
                TargetZone.Bullseye => 4,
                TargetZone.Ring3 => 5,
                TargetZone.Ring2 => 6,
                TargetZone.Ring1 => 7,
                _ => 8
            };
        }

        // Build current board ownership from AllThrows: ThrowRecord.PointsAwarded stores (CellIndex + 1)
        var board = new Dictionary<int, string>();
        foreach (var t in state.AllThrows)
        {
            if (t.PointsAwarded > 0 && t.PointsAwarded <= 9)
            {
                int c = t.PointsAwarded - 1;
                board[c] = t.PlayerId;
            }
        }

        bool claimed = false;
        if (!board.ContainsKey(cellIndex))
        {
            claimed = true;
            board[cellIndex] = player.Id;
            player.Score++;
        }

        int pointsAwarded = claimed ? (cellIndex + 1) : 0;
        player.ThrowsTaken++;
        player.ThrowHistory.Add(claimed ? 1 : 0);
        if (zone == TargetZone.Bullseye) player.BullseyesHit++;

        var throwRecord = new ThrowRecord
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Zone = zone,
            PointsAwarded = pointsAwarded,
            X = x,
            Y = y,
            IsClutchCalled = isClutchCalled,
            IsBullseye = (zone == TargetZone.Bullseye),
            ThrownAt = DateTimeOffset.UtcNow
        };

        state.LastThrow = throwRecord;
        state.AllThrows.Add(throwRecord);

        // Check if current player completed 3 in a row
        foreach (var line in WinningLines)
        {
            if (board.TryGetValue(line[0], out var p0) && p0 == player.Id &&
                board.TryGetValue(line[1], out var p1) && p1 == player.Id &&
                board.TryGetValue(line[2], out var p2) && p2 == player.Id)
            {
                state.Status = MatchStatus.Finished;
                state.WinnerPlayerId = player.Id;
                state.WinnerName = player.Name;
                return state;
            }
        }

        // Check if all 9 cells are claimed
        if (board.Count >= 9)
        {
            state.Status = MatchStatus.Finished;
            var winner = state.Players.OrderByDescending(p => p.Score).FirstOrDefault();
            state.WinnerPlayerId = winner?.Id;
            state.WinnerName = winner?.Name;
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
