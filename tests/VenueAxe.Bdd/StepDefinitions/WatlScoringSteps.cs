using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class WatlScoringSteps
{
    private readonly WatlStandardMatchEngine _engine = new();
    private GameStateSnapshot _state = null!;
    private readonly List<GameStateSnapshot> _history = new();
    private bool _clutchCalled = false;
    private string _currentThrowerName = string.Empty;

    [Given(@"a new WATL standard match is initialized")]
    public void GivenANewWatlStandardMatchIsInitialized()
    {
        // Ready for players
    }

    [Given(@"players ""(.*)"" and ""(.*)"" are enrolled")]
    public void GivenPlayersAreEnrolled(string player1, string player2)
    {
        var players = new List<GamePlayer>
        {
            new() { Id = "p1", Name = player1 },
            new() { Id = "p2", Name = player2 }
        };

        _state = _engine.Initialize(Guid.NewGuid(), players);
        _history.Add(_state);
        _currentThrowerName = player1;
    }

    [Given(@"""(.*)"" calls a Killshot on the ""(.*)"" side")]
    public void GivenPlayerCallsKillshot(string playerName, string side)
    {
        _clutchCalled = true;
    }

    [Given(@"""(.*)"" does not call a Killshot")]
    public void GivenPlayerDoesNotCallKillshot(string playerName)
    {
        _clutchCalled = false;
    }

    [When(@"""(.*)"" throws at coordinates (.*) and (.*)")]
    [Given(@"""(.*)"" throws at coordinates (.*) and (.*)")]
    public void WhenPlayerThrowsAtCoordinates(string playerName, double x, double y)
    {
        _currentThrowerName = playerName;
        _history.Add(_state);
        _state = _engine.RecordThrow(_state, x, y, null, _clutchCalled);
        _clutchCalled = false; // Reset after throw
    }

    [Then(@"the throw should be scored as a ""(.*)""")]
    [Then(@"the throw should be scored as an ""(.*)""")]
    [Then(@"the throw should be scored as ""(.*)""")]
    public void ThenThrowShouldBeScoredAs(string expectedZone)
    {
        var lastThrow = _state.LastThrow;
        lastThrow.Should().NotBeNull();
        if (expectedZone == "Bullseye")
            lastThrow!.Zone.Should().Be(TargetZone.Bullseye);
        else if (expectedZone == "Ring 5")
            lastThrow!.Zone.Should().Be(TargetZone.Ring5);
        else if (expectedZone == "Ring 1")
            lastThrow!.Zone.Should().Be(TargetZone.Ring1);
        else if (expectedZone == "Miss")
            lastThrow!.Zone.Should().Be(TargetZone.Miss);
    }

    [Then(@"""(.*)"" should receive (.*) points")]
    public void ThenPlayerShouldReceivePoints(string playerName, int expectedPoints)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.Score.Should().Be(expectedPoints);
    }

    [Then(@"the turn should advance to ""(.*)""")]
    public void ThenTheTurnShouldAdvanceTo(string expectedNextPlayer)
    {
        var currentPlayer = _state.Players[_state.CurrentPlayerIndex];
        currentPlayer.Name.Should().Be(expectedNextPlayer);
    }

    [Then(@"""(.*)"" should have (.*) Killshot attempt used")]
    [Then(@"""(.*)"" should have (.*) Killshot attempts used")]
    public void ThenPlayerShouldHaveKillshotAttemptsUsed(string playerName, int expectedAttempts)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.KillsCalled.Should().Be(expectedAttempts);
    }

    [Then(@"""(.*)"" cannot call any further Killshots in this match")]
    public void ThenPlayerCannotCallFurtherKillshots(string playerName)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.KillsRemaining.Should().Be(0);
    }

    [When(@"the lane master undoes the last throw")]
    public void WhenTheLaneMasterUndoesTheLastThrow()
    {
        _state = _engine.UndoLastThrow(_state);
    }

    [Then(@"the turn should be restored to ""(.*)""")]
    public void ThenTheTurnShouldBeRestoredTo(string expectedPlayer)
    {
        var currentPlayer = _state.Players[_state.CurrentPlayerIndex];
        currentPlayer.Name.Should().Be(expectedPlayer);
    }
}
