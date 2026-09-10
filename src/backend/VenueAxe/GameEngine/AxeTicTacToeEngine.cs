using System;
using System.Collections.Generic;
using System.Linq;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

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
