using System;
using System.Collections.Generic;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

public class GamePlayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Thrower";
    public string AvatarColor { get; set; } = "#f59e0b";
    public int Score { get; set; } = 0;
    public int ThrowsTaken { get; set; } = 0;
    public int BullseyesHit { get; set; } = 0;
    public int ClutchesHit { get; set; } = 0;
    public int Streak { get; set; } = 0;
    public List<int> ThrowHistory { get; set; } = new();
}

public class GameConfig
{
    public int TotalRounds { get; set; } = 10;
    public int ThrowsPerRound { get; set; } = 1;
    public int StartingScore { get; set; } = 301;
    public bool AllowClutchAnytime { get; set; } = false;
}

public class GameStateSnapshot
{
    public Guid MatchId { get; set; }
    public string GameTypeId { get; set; } = "watl_standard";
    public string GameName { get; set; } = "WATL Standard Match";
    public MatchStatus Status { get; set; } = MatchStatus.InProgress;
    public int CurrentRound { get; set; } = 1;
    public int TotalRounds { get; set; } = 10;
    public int CurrentPlayerIndex { get; set; } = 0;
    public List<GamePlayer> Players { get; set; } = new();
    public string? WinnerPlayerId { get; set; }
    public string? WinnerName { get; set; }
    public ThrowRecord? LastThrow { get; set; }
}

public class ThrowRecord
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public TargetZone Zone { get; set; }
    public int PointsAwarded { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public bool IsClutchCalled { get; set; }
    public bool IsBullseye { get; set; }
    public DateTimeOffset ThrownAt { get; set; } = DateTimeOffset.UtcNow;
}

public interface IGameEngine
{
    string GameTypeId { get; }
    string DisplayName { get; }
    string Description { get; }
    int DefaultRounds { get; }

    GameStateSnapshot Initialize(Guid matchId, List<GamePlayer> players, GameConfig? config = null);
    GameStateSnapshot RecordThrow(GameStateSnapshot state, double? x, double? y, TargetZone? manualZone, bool isClutchCalled);
    GameStateSnapshot UndoLastThrow(GameStateSnapshot state);
}
