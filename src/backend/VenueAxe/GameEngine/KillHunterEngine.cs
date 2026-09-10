using System;
using System.Collections.Generic;
using System.Linq;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

public class KillHunterEngine : IGameEngine
{
    public string GameTypeId => "kill_hunter";
    public string DisplayName => "Kill Hunter";
    public string Description => "High-stakes precision match. Only Bullseyes (6 pts) and Killshots (8 pts) score; all other rings score 0!";
    public int DefaultRounds => 10;

    public GameStateSnapshot Initialize(Guid matchId, List<GamePlayer> players, GameConfig? config = null)
    {
        var initialPlayers = players.Select(p => new GamePlayer
        {
            Id = p.Id,
            Name = p.Name,
            AvatarColor = p.AvatarColor,
            Score = 0,
            ThrowsTaken = 0,
            BullseyesHit = 0,
            KillsHit = 0,
            KillsRemaining = 10, // In Kill Hunter, players can hunt killshots on any round
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

    public GameStateSnapshot RecordThrow(GameStateSnapshot state, double? x, double? y, TargetZone? manualZone, bool isClutchCalled)
    {
        if (state.Status == MatchStatus.Finished || state.Players.Count == 0)
            return state;

        var player = state.Players[state.CurrentPlayerIndex];
        int points = 0;
        TargetZone zone = TargetZone.Miss;
        bool isBullseye = false;
        bool isKillshotHit = false;

        if (x.HasValue && y.HasValue)
        {
            var eval = WatlTargetMath.Evaluate(x.Value, y.Value, isClutchCalled);
            zone = eval.Zone;
            if (eval.Zone == TargetZone.Bullseye)
            {
                points = 6;
                isBullseye = true;
            }
            else if (eval.IsKillshotHit)
            {
                points = 8;
                isKillshotHit = true;
            }
            else
            {
                // In Kill Hunter, rings 1-5 and misses score 0
                points = 0;
            }
        }
        else if (manualZone.HasValue)
        {
            zone = manualZone.Value;
            if (zone == TargetZone.Bullseye)
            {
                points = 6;
                isBullseye = true;
            }
            else if (zone == TargetZone.KillLeft || zone == TargetZone.KillRight ||
                     zone == TargetZone.ClutchLeft || zone == TargetZone.ClutchRight)
            {
                points = isClutchCalled ? 8 : 0;
                isKillshotHit = isClutchCalled;
            }
            else
            {
                points = 0;
            }
        }

        player.Score += points;
        player.ThrowsTaken++;
        player.ThrowHistory.Add(points);

        if (isBullseye)
        {
            player.BullseyesHit++;
            player.Streak++;
        }
        else if (isKillshotHit)
        {
            player.KillsHit++;
            player.Streak++;
        }
        else
        {
            player.Streak = 0;
        }

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
            IsKillshot = isKillshotHit,
            ThrownAt = DateTimeOffset.UtcNow
        };
        state.AllThrows.Add(state.LastThrow);

        // Advance to next player
        int nextPlayerIdx = state.CurrentPlayerIndex + 1;
        if (nextPlayerIdx >= state.Players.Count)
        {
            state.CurrentPlayerIndex = 0;
            state.CurrentRound++;

            if (state.CurrentRound > state.TotalRounds)
            {
                state.Status = MatchStatus.Finished;
                var winner = state.Players.OrderByDescending(p => p.Score).FirstOrDefault();
                if (winner != null)
                {
                    state.WinnerPlayerId = winner.Id;
                    state.WinnerName = winner.Name;
                }
            }
        }
        else
        {
            state.CurrentPlayerIndex = nextPlayerIdx;
        }

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
            int lastScore = prevPlayer.ThrowHistory[^1];
            prevPlayer.ThrowHistory.RemoveAt(prevPlayer.ThrowHistory.Count - 1);
            prevPlayer.Score -= lastScore;
            prevPlayer.ThrowsTaken = Math.Max(0, prevPlayer.ThrowsTaken - 1);
            if (lastScore == 6)
            {
                prevPlayer.BullseyesHit = Math.Max(0, prevPlayer.BullseyesHit - 1);
                prevPlayer.Streak = Math.Max(0, prevPlayer.Streak - 1);
            }
            else if (lastScore == 8)
            {
                prevPlayer.KillsHit = Math.Max(0, prevPlayer.KillsHit - 1);
                prevPlayer.Streak = Math.Max(0, prevPlayer.Streak - 1);
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
