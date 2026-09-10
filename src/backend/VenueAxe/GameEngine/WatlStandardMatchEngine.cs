using System;
using System.Collections.Generic;
using System.Linq;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

public class WatlStandardMatchEngine : IGameEngine
{
    public string GameTypeId => "watl_standard";
    public string DisplayName => "WATL Standard Match";
    public string Description => "Official WATL 10-throw match format. 2 Killshots (8 pts) can be called anytime.";
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

    public GameStateSnapshot RecordThrow(GameStateSnapshot state, double? x, double? y, TargetZone? manualZone, bool isClutchCalled)
    {
        if (state.Status == MatchStatus.Finished || state.Players.Count == 0)
            return state;

        var player = state.Players[state.CurrentPlayerIndex];
        int points = 0;
        TargetZone zone = TargetZone.Miss;
        bool isBullseye = false;
        bool isSpecialHit = false;

        // Player can call Killshot up to 2 times anytime during the match
        bool isKillArmAllowed = isClutchCalled && player.KillsRemaining > 0;
        if (isClutchCalled && isKillArmAllowed)
        {
            player.KillsCalled++;
            player.KillsRemaining = Math.Max(0, player.KillsRemaining - 1);
        }

        if (x.HasValue && y.HasValue)
        {
            var eval = WatlTargetMath.Evaluate(x.Value, y.Value, isKillArmAllowed);
            points = eval.Points;
            zone = eval.Zone;
            isSpecialHit = eval.IsKillshotHit;
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
                TargetZone.KillLeft or TargetZone.KillRight or TargetZone.ClutchLeft or TargetZone.ClutchRight => isKillArmAllowed ? 8 : 0,
                TargetZone.Drop => 0,
                TargetZone.Miss => 0,
                TargetZone.Fault => 0,
                _ => 0
            };
            isBullseye = zone == TargetZone.Bullseye;
            isSpecialHit = (zone == TargetZone.KillLeft || zone == TargetZone.KillRight || zone == TargetZone.ClutchLeft || zone == TargetZone.ClutchRight) && isKillArmAllowed;
        }

        player.Score += points;
        player.ThrowsTaken++;
        player.ThrowHistory.Add(points);

        if (isBullseye)
        {
            player.BullseyesHit++;
            player.Streak++;
        }
        else if (isSpecialHit)
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
            IsKillCalled = isKillArmAllowed,
            IsBullseye = isBullseye,
            IsKillshot = isSpecialHit,
            ThrownAt = DateTimeOffset.UtcNow
        };
        state.AllThrows.Add(state.LastThrow);

        // Advance to next player
        int nextPlayerIdx = state.CurrentPlayerIndex + 1;
        if (nextPlayerIdx >= state.Players.Count)
        {
            // End of round for all players
            state.CurrentPlayerIndex = 0;
            state.CurrentRound++;

            if (state.CurrentRound > state.TotalRounds)
            {
                // Match Over!
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

        var lastThrow = state.AllThrows.LastOrDefault();
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
            }
            else if (lastScore == 8)
            {
                prevPlayer.KillsHit = Math.Max(0, prevPlayer.KillsHit - 1);
            }

            if (lastThrow != null && lastThrow.IsKillCalled)
            {
                prevPlayer.KillsCalled = Math.Max(0, prevPlayer.KillsCalled - 1);
                prevPlayer.KillsRemaining = Math.Min(2, prevPlayer.KillsRemaining + 1);
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
