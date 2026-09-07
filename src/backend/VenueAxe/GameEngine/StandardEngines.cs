using System;
using System.Collections.Generic;
using System.Linq;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

public class WatlStandardMatchEngine : IGameEngine
{
    public string GameTypeId => "watl_standard";
    public string DisplayName => "WATL Standard Match";
    public string Description => "Official WATL 10-throw match format. Killshot targets (8 pts) enabled on throws 5 & 10.";
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
            ClutchesHit = 0,
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

        if (x.HasValue && y.HasValue)
        {
            var eval = WatlTargetMath.Evaluate(x.Value, y.Value, isClutchCalled);
            points = eval.Points;
            zone = eval.Zone;
            isSpecialHit = eval.IsClutchOrKillshotHit;
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
                TargetZone.ClutchLeft or TargetZone.ClutchRight => isClutchCalled ? 8 : 0,
                _ => 0
            };
            isBullseye = zone == TargetZone.Bullseye;
            isSpecialHit = (zone == TargetZone.ClutchLeft || zone == TargetZone.ClutchRight) && isClutchCalled;
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
            player.ClutchesHit++;
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
            IsClutchCalled = isClutchCalled,
            IsBullseye = isBullseye,
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
        if (state.Players.Count == 0) return state;

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
            }
        }

        state.CurrentPlayerIndex = prevPlayerIdx;
        state.Status = MatchStatus.InProgress;
        state.WinnerPlayerId = null;
        state.WinnerName = null;
        if (state.AllThrows.Count > 0)
        {
            state.AllThrows.RemoveAt(state.AllThrows.Count - 1);
        }
        state.LastThrow = state.AllThrows.LastOrDefault();

        return state;
    }
}

public class IatfStandardMatchEngine : IGameEngine
{
    public string GameTypeId => "iatf_standard";
    public string DisplayName => "IATF Standard Match";
    public string Description => "Official IATF match format. 3-ring target (1, 3, 5 pts) with Clutch (7 pts) on throw 5.";
    public int DefaultRounds => 5;

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
            ClutchesHit = 0,
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
            TotalRounds = DefaultRounds,
            CurrentPlayerIndex = 0,
            Players = initialPlayers
        };
    }

    public ThrowEvaluation EvaluateThrow(double? x, double? y, TargetZone? manualZone = null, bool isClutchCalled = false)
    {
        if (x.HasValue && y.HasValue)
        {
            return IatfTargetMath.Evaluate(x.Value, y.Value, isClutchCalled);
        }

        if (manualZone.HasValue)
        {
            var zone = manualZone.Value;
            var points = zone switch
            {
                TargetZone.Bullseye => 5,
                TargetZone.Ring3 => 3,
                TargetZone.Ring1 => 1,
                TargetZone.ClutchLeft or TargetZone.ClutchRight => isClutchCalled ? 7 : 0,
                _ => 0
            };
            return new ThrowEvaluation(
                zone,
                points,
                (zone == TargetZone.ClutchLeft || zone == TargetZone.ClutchRight) && isClutchCalled,
                zone.ToString()
            );
        }

        return new ThrowEvaluation(TargetZone.Miss, 0, false, "Miss");
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
        bool isClutchHit = false;

        if (x.HasValue && y.HasValue)
        {
            var eval = IatfTargetMath.Evaluate(x.Value, y.Value, isClutchCalled);
            zone = eval.Zone;
            points = eval.Points;
            isBullseye = eval.Zone == TargetZone.Bullseye;
            isClutchHit = eval.IsClutchOrKillshotHit;
        }
        else if (manualZone.HasValue)
        {
            zone = manualZone.Value;
            points = zone switch
            {
                TargetZone.Bullseye => 5,
                TargetZone.Ring3 => 3,
                TargetZone.Ring1 => 1,
                TargetZone.ClutchLeft or TargetZone.ClutchRight => isClutchCalled ? 7 : 0,
                _ => 0
            };
            isBullseye = zone == TargetZone.Bullseye;
            isClutchHit = (zone == TargetZone.ClutchLeft || zone == TargetZone.ClutchRight) && isClutchCalled;
        }

        player.Score += points;
        player.ThrowsTaken++;
        player.ThrowHistory.Add(points);

        if (isBullseye)
        {
            player.BullseyesHit++;
            player.Streak++;
        }
        else
        {
            player.Streak = 0;
        }

        if (isClutchHit)
        {
            player.ClutchesHit++;
        }

        state.LastThrow = new ThrowRecord
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Zone = zone,
            PointsAwarded = points,
            X = x,
            Y = y,
            IsBullseye = isBullseye,
            IsClutchCalled = isClutchCalled,
            ThrownAt = DateTimeOffset.UtcNow
        };
        state.AllThrows.Add(state.LastThrow);

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
        if (state.Players.Count == 0) return state;

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
            if (lastScore == 5)
            {
                prevPlayer.BullseyesHit = Math.Max(0, prevPlayer.BullseyesHit - 1);
            }
            else if (lastScore == 7)
            {
                prevPlayer.ClutchesHit = Math.Max(0, prevPlayer.ClutchesHit - 1);
            }
        }

        state.CurrentPlayerIndex = prevPlayerIdx;
        state.Status = MatchStatus.InProgress;
        state.WinnerPlayerId = null;
        state.WinnerName = null;
        if (state.AllThrows.Count > 0)
        {
            state.AllThrows.RemoveAt(state.AllThrows.Count - 1);
        }
        state.LastThrow = state.AllThrows.LastOrDefault();

        return state;
    }
}

public class CountdownGameEngine : IGameEngine
{
    public string GameTypeId => "countdown_301";
    public string DisplayName => "Countdown 301";
    public string Description => "Start at 301. Subtract points with each throw to reach exactly zero!";
    public int DefaultRounds => 15;

    public GameStateSnapshot Initialize(Guid matchId, List<GamePlayer> players, GameConfig? config = null)
    {
        int startScore = config?.StartingScore ?? 301;
        var initialPlayers = players.Select(p => new GamePlayer
        {
            Id = p.Id,
            Name = p.Name,
            AvatarColor = p.AvatarColor,
            Score = startScore,
            ThrowsTaken = 0,
            BullseyesHit = 0,
            ClutchesHit = 0,
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
        if (state.Status == MatchStatus.Finished || state.Players.Count == 0) return state;

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
                TargetZone.ClutchLeft or TargetZone.ClutchRight => isClutchCalled ? 7 : 0,
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
            IsClutchCalled = isClutchCalled,
            IsBullseye = isBullseye
        };

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

    public GameStateSnapshot UndoLastThrow(GameStateSnapshot state) => state;
}

public static class GameEngineRegistry
{
    private static readonly Dictionary<string, IGameEngine> _engines = new()
    {
        { "watl_standard", new WatlStandardMatchEngine() },
        { "iatf_standard", new IatfStandardMatchEngine() },
        { "countdown_301", new CountdownGameEngine() }
    };

    public static IGameEngine GetEngine(string gameTypeId)
    {
        return _engines.TryGetValue(gameTypeId, out var engine) ? engine : _engines["watl_standard"];
    }

    public static IEnumerable<IGameEngine> GetAllEngines() => _engines.Values;
}
