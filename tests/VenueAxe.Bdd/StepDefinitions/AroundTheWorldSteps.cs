using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class AroundTheWorldSteps
{
    private readonly AroundTheWorldEngine _engine = new();
    private GameStateSnapshot _state = null!;

    [Given(@"an Around The World match is initialized")]
    public void GivenAnAroundTheWorldMatchIsInitialized()
    {
        // Ready
    }

    [Given(@"around the world players ""(.*)"" and ""(.*)"" are enrolled")]
    public void GivenAroundTheWorldPlayersAreEnrolled(string p1, string p2)
    {
        var players = new List<GamePlayer>
        {
            new() { Id = "atw-1", Name = p1 },
            new() { Id = "atw-2", Name = p2 }
        };
        _state = _engine.Initialize(Guid.NewGuid(), players);
    }

    [Given(@"""(.*)"" is targeting step (.*) ""(.*)""")]
    public void GivenPlayerIsTargetingStep(string playerName, int step, string targetName)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.Score = step;
    }

    [Given(@"""(.*)"" has completed (.*) target rings")]
    public void GivenPlayerHasCompletedTargetRings(string playerName, int ringsCompleted)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.Score = ringsCompleted;
    }

    [When(@"""(.*)"" hits ""(.*)""")]
    public void WhenPlayerHits(string playerName, string zoneName)
    {
        var zone = Enum.Parse<TargetZone>(zoneName, ignoreCase: true);
        bool isClutch = (zone == TargetZone.ClutchLeft || zone == TargetZone.ClutchRight);
        _state = _engine.RecordThrow(_state, null, null, manualZone: zone, isClutchCalled: isClutch);
    }

    [Then(@"""(.*)"" around the world score should be (.*)")]
    public void ThenPlayerAroundTheWorldScoreShouldBe(string playerName, int expectedScore)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.Score.Should().Be(expectedScore);
    }

    [Then(@"""(.*)"" should now be targeting ""(.*)""")]
    [Then(@"""(.*)"" should still be targeting ""(.*)""")]
    public void ThenPlayerShouldBeTargeting(string playerName, string expectedTarget)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        int step = player.Score;
        string currentTarget = step switch
        {
            0 => "Ring 1",
            1 => "Ring 2",
            2 => "Ring 3",
            3 => "Ring 4",
            4 => "Ring 5",
            5 => "Bullseye",
            6 => "Clutch",
            _ => "Complete"
        };
        currentTarget.Should().Be(expectedTarget);
    }

    [Then(@"the around the world match should be finished")]
    public void ThenTheAroundTheWorldMatchShouldBeFinished()
    {
        _state.Status.Should().Be(MatchStatus.Finished);
    }

    [Then(@"the around the world winner should be ""(.*)""")]
    public void ThenTheAroundTheWorldWinnerShouldBe(string expectedWinner)
    {
        _state.WinnerName.Should().Be(expectedWinner);
    }

    [When(@"the around the world throw is undone")]
    public void WhenTheAroundTheWorldThrowIsUndone()
    {
        _state = _engine.UndoLastThrow(_state);
    }

    [Then(@"the around the world turn should be restored to ""(.*)""")]
    public void ThenTheAroundTheWorldTurnShouldBeRestoredTo(string expectedPlayer)
    {
        var currentPlayer = _state.Players[_state.CurrentPlayerIndex];
        currentPlayer.Name.Should().Be(expectedPlayer);
    }
}
