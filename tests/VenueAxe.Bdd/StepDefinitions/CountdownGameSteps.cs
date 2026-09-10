using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class CountdownGameSteps
{
    private readonly CountdownGameEngine _engine = new();
    private GameStateSnapshot _state = null!;

    [Given(@"a new Countdown 301 match is initialized")]
    public void GivenANewCountdown301MatchIsInitialized()
    {
    }

    [Given(@"player ""(.*)"" is enrolled with starting score (.*)")]
    public void GivenPlayerIsEnrolledWithStartingScore(string playerName, int startingScore)
    {
        var player = new GamePlayer { Id = "p1", Name = playerName };
        _state = _engine.Initialize(Guid.NewGuid(), new List<GamePlayer> { player }, new GameConfig { StartingScore = startingScore });
    }

    [Given(@"""(.*)"" has a remaining score of (.*)")]
    public void GivenPlayerHasRemainingScoreOf(string playerName, int score)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.Score = score;
    }

    [When(@"""(.*)"" throws and scores (.*) points")]
    public void WhenPlayerThrowsAndScoresPoints(string playerName, int points)
    {
        TargetZone zone = points switch
        {
            6 => TargetZone.Bullseye,
            5 => TargetZone.Ring5,
            4 => TargetZone.Ring4,
            3 => TargetZone.Ring3,
            2 => TargetZone.Ring2,
            1 => TargetZone.Ring1,
            8 => TargetZone.ClutchLeft,
            _ => TargetZone.Miss
        };

        _state = _engine.RecordThrow(_state, null, null, manualZone: zone, isClutchCalled: points == 8);
    }

    [Then(@"""(.*)"" remaining score should be (.*)")]
    public void ThenPlayerRemainingScoreShouldBe(string playerName, int expectedScore)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.Score.Should().Be(expectedScore);
    }

    [Then(@"""(.*)"" should be marked as ""Bust""")]
    public void ThenPlayerShouldBeMarkedAsBust(string playerName)
    {
        var lastThrow = _state.LastThrow;
        lastThrow.Should().NotBeNull();
        lastThrow!.PointsAwarded.Should().Be(0);
    }

    [Then(@"""(.*)"" remaining score should remain (.*)")]
    public void ThenPlayerRemainingScoreShouldRemain(string playerName, int expectedScore)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.Score.Should().Be(expectedScore);
    }

    [Then(@"the throw points should not be deducted")]
    public void ThenThrowPointsShouldNotBeDeducted()
    {
        var lastThrow = _state.LastThrow;
        lastThrow.Should().NotBeNull();
        lastThrow!.PointsAwarded.Should().Be(0);
    }

    [Then(@"the match should declare ""(.*)"" as the winner")]
    public void ThenMatchShouldDeclareWinner(string playerName)
    {
        _state.Status.Should().Be(MatchStatus.Finished);
        var player = _state.Players.First(p => p.Name == playerName);
        _state.WinnerPlayerId.Should().Be(player.Id);
    }
}
