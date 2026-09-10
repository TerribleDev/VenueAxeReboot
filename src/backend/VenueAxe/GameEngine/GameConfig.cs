namespace VenueAxe.GameEngine;

public class GameConfig
{
    public int TotalRounds { get; set; } = 10;
    public int ThrowsPerRound { get; set; } = 1;
    public int StartingScore { get; set; } = 301;
    public bool AllowClutchAnytime { get; set; } = true;
    public bool AllowKillAnytime { get; set; } = true;
}
