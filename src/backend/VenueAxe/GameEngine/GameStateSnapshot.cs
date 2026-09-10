using System;
using System.Collections.Generic;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

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
    public List<ThrowRecord> AllThrows { get; set; } = new();
}
