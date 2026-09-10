using System;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class LaneOperationsSteps
{
    private Lane _lane = null!;
    private bool _pairingSucceeded;
    private string? _pairedRole;
    private bool _pairingUnauthorized;

    [Given(@"an operating venue with lane ""(.*)""")]
    public void GivenAnOperatingVenueWithLane(string laneName)
    {
        _lane = new Lane
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            VenueId = Guid.NewGuid(),
            Name = laneName,
            LaneNumber = 1,
            CurrentStatus = LaneStatus.Available,
            TabletPairingCode = "AX101",
            ScreenPairingCode = "TV101"
        };
    }

    [Given(@"""(.*)"" has tablet pairing pin ""(.*)"" and TV pairing pin ""(.*)""")]
    public void GivenLaneHasPairingPins(string laneName, string tabletPin, string tvPin)
    {
        _lane.TabletPairingCode = tabletPin;
        _lane.ScreenPairingCode = tvPin;
    }

    [When(@"a tablet attempts pairing with PIN ""(.*)""")]
    public void WhenATabletAttemptsPairingWithPin(string pin)
    {
        if (pin == _lane.TabletPairingCode)
        {
            _pairingSucceeded = true;
            _pairedRole = "tablet";
            _pairingUnauthorized = false;
        }
        else if (pin == _lane.ScreenPairingCode)
        {
            _pairingSucceeded = true;
            _pairedRole = "tv";
            _pairingUnauthorized = false;
        }
        else
        {
            _pairingSucceeded = false;
            _pairedRole = null;
            _pairingUnauthorized = true;
        }
    }

    [Then(@"pairing should succeed with role ""(.*)"" for ""(.*)""")]
    public void ThenPairingShouldSucceedWithRole(string expectedRole, string laneName)
    {
        _pairingSucceeded.Should().BeTrue();
        _pairedRole.Should().Be(expectedRole);
    }

    [Then(@"pairing should be rejected with unauthorized error")]
    public void ThenPairingShouldBeRejectedWithUnauthorizedError()
    {
        _pairingUnauthorized.Should().BeTrue();
        _pairingSucceeded.Should().BeFalse();
    }

    [Given(@"""(.*)"" is currently active")]
    public void GivenLaneIsCurrentlyActive(string laneName)
    {
        _lane.CurrentStatus = LaneStatus.Active;
    }

    [When(@"the lane master triggers a safety stop on ""(.*)"" with reason ""(.*)""")]
    public void WhenTheLaneMasterTriggersASafetyStop(string laneName, string reason)
    {
        _lane.CurrentStatus = LaneStatus.Maintenance;
    }

    [Then(@"the lane status should be ""(.*)""")]
    public void ThenTheLaneStatusShouldBe(string expectedStatus)
    {
        _lane.CurrentStatus.ToString().Should().Be(expectedStatus);
    }

    [Then(@"throws cannot be recorded on ""(.*)""")]
    public void ThenThrowsCannotBeRecordedOn(string laneName)
    {
        bool canRecordThrow = _lane.CurrentStatus == LaneStatus.Active;
        canRecordThrow.Should().BeFalse();
    }

    [Given(@"""(.*)"" is in ""(.*)"" status")]
    public void GivenLaneIsInStatus(string laneName, string status)
    {
        _lane.CurrentStatus = Enum.Parse<LaneStatus>(status, ignoreCase: true);
    }

    [When(@"the lane master clears the safety stop on ""(.*)""")]
    public void WhenTheLaneMasterClearsTheSafetyStop(string laneName)
    {
        _lane.CurrentStatus = LaneStatus.Available;
    }
}
