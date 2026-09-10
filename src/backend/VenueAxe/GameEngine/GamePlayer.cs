using System;
using System.Collections.Generic;

namespace VenueAxe.GameEngine;

public class GamePlayer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Thrower";
    public string AvatarColor { get; set; } = "#f59e0b";
    public int Score { get; set; } = 0;
    public int ThrowsTaken { get; set; } = 0;
    public int BullseyesHit { get; set; } = 0;
    public int KillsHit { get; set; } = 0;
    public int ClutchesHit
    {
        get => KillsHit;
        set => KillsHit = value;
    }
    public int KillsRemaining { get; set; } = 2;
    public int KillsCalled { get; set; } = 0;
    public int Streak { get; set; } = 0;
    public List<int> ThrowHistory { get; set; } = new();
}
