using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class KillHunterSteps
{
    private readonly KillHunterEngine _engine = new();
    private GameStateSnapshot _state = null!;
    private bool _killCalled = false;

    [Given(@"a new Kill Hunter match is initialized")]
    public void GivenANewKillHunterMatchIsInitialized()
    {
        // Ready
    }

    [Given(@"kill hunter players ""(.*)"" and ""(.*)"" are enrolled")]
    public void GivenKillHunterPlayersAreEnrolled(string p1, string p2)
    {
        var players = new List<GamePlayer>
        {
            new() { Id = "kh-1", Name = p1 },
            new() { Id = "kh-2", Name = p2 }
        };
        _state = _engine.Initialize(Guid.NewGuid(), players);
    }

    [Given(@"""(.*)"" calls a Killshot in kill hunter")]
    public void GivenPlayerCallsKillshotInKillHunter(string playerName)
    {
        _killCalled = true;
    }

    [When(@"""(.*)"" throws at coordinates (.*) and (.*) in kill hunter")]
    public void WhenPlayerThrowsAtCoordinatesInKillHunter(string playerName, double x, double y)
    {
        _state = _engine.RecordThrow(_state, x, y, null, _killCalled);
        _killCalled = false;
    }

    [Then(@"""(.*)"" should receive (.*) points in kill hunter")]
    public void ThenPlayerShouldReceivePointsInKillHunter(string playerName, int expectedPoints)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.Score.Should().Be(expectedPoints);
    }

    [Then(@"""(.*)"" streak should be (.*)")]
    public void ThenPlayerStreakShouldBe(string playerName, int expectedStreak)
    {
        var player = _state.Players.First(p => p.Name == playerName);
        player.Streak.Should().Be(expectedStreak);
    }

    [When(@"the kill hunter throw is undone")]
    public void WhenTheKillHunterThrowIsUndone()
    {
        _state = _engine.UndoLastThrow(_state);
    }
}
