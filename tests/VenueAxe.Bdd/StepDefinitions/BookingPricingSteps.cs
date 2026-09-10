using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Services;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class BookingPricingSteps
{
    private BookingConfig _config = new();
    private int _partySize;
    private int _durationMinutes;
    private PricingBreakdownDto _pricingResult = null!;
    private List<Lane> _arenaLanes = new();
    private LaneAllocationResult _allocationResult = null!;

    [Given(@"a venue booking configuration with base rate \$(.*) per person per hour")]
    public void GivenAVenueBookingConfigurationWithBaseRate(int baseDollarRate)
    {
        _config = new BookingConfig
        {
            PricingModel = PricingModel.PerPerson,
            BasePriceCents = baseDollarRate * 100,
            PeakPriceCents = baseDollarRate * 100,
            MinPartySize = 1,
            MaxPartySize = 30
        };
    }

    [Given(@"a volume discount rule of (.*) percent off for parties of (.*) or more")]
    public void GivenAVolumeDiscountRule(int percent, int threshold)
    {
        _config.DiscountRulesJson = $@"[{{""id"":""disc-vol"",""name"":""Volume Group Discount"",""type"":""group_size"",""minPartySize"":{threshold},""discountPercent"":{percent},""autoApply"":true}}]";
    }

    [When(@"a guest requests a booking for (.*) throwers for (.*) minutes")]
    public void WhenAGuestRequestsABooking(int partySize, int durationMinutes)
    {
        _partySize = partySize;
        _durationMinutes = durationMinutes;

        var request = new CalculatePriceRequest(
            PartySize: partySize,
            DurationMinutes: durationMinutes,
            StartTime: new DateTimeOffset(2026, 10, 15, 14, 0, 0, TimeSpan.Zero) // 2 PM off-peak
        );

        _pricingResult = BookingService.CalculatePricingInternal(_config, request);
    }

    [Then(@"the total amount should be (.*) cents")]
    public void ThenTotalAmountShouldBe(int expectedTotalCents)
    {
        _pricingResult.NetTotalCents.Should().Be(expectedTotalCents);
    }

    [Then(@"the gross total should be (.*) cents")]
    public void ThenGrossTotalShouldBe(int expectedGrossCents)
    {
        _pricingResult.GrossTotalCents.Should().Be(expectedGrossCents);
    }

    [Then(@"the discount amount should be (.*) cents")]
    public void ThenDiscountAmountShouldBe(int expectedDiscountCents)
    {
        _pricingResult.DiscountAmountCents.Should().Be(expectedDiscountCents);
    }

    [Then(@"the final net total should be (.*) cents")]
    public void ThenFinalNetTotalShouldBe(int expectedNetCents)
    {
        _pricingResult.NetTotalCents.Should().Be(expectedNetCents);
    }

    [Given(@"an arena with (.*) available lanes numbered 1 through (.*)")]
    public void GivenAnArenaWithLanes(int laneCount, int maxLaneNum)
    {
        _arenaLanes.Clear();
        for (int i = 1; i <= laneCount; i++)
        {
            _arenaLanes.Add(new Lane
            {
                Id = Guid.NewGuid(),
                LaneNumber = i,
                Name = $"Lane {i}",
                MaxThrowers = 6,
                IsActive = true
            });
        }
    }

    [Given(@"each lane has a maximum capacity of (.*) throwers")]
    public void GivenEachLaneHasAMaximumCapacity(int capacity)
    {
        foreach (var l in _arenaLanes)
        {
            l.MaxThrowers = capacity;
        }
    }

    [When(@"a party of (.*) throwers is allocated lanes")]
    public void WhenAPartyIsAllocatedLanes(int partySize)
    {
        _allocationResult = LaneAllocationEngine.AllocateContiguousLanes(_arenaLanes, new List<Booking>(), partySize);
    }

    [Then(@"exactly (.*) lanes should be assigned")]
    public void ThenExactlyLanesShouldBeAssigned(int expectedLaneCount)
    {
        _allocationResult.IsSuccess.Should().BeTrue();
        _allocationResult.AllocatedLanes.Count.Should().Be(expectedLaneCount);
    }

    [Then(@"the assigned lane numbers must be contiguous")]
    public void ThenTheAssignedLaneNumbersMustBeContiguous()
    {
        var laneNumbers = _allocationResult.AllocatedLanes.Select(l => l.LaneNumber).OrderBy(n => n).ToList();
        for (int i = 1; i < laneNumbers.Count; i++)
        {
            (laneNumbers[i] - laneNumbers[i - 1]).Should().Be(1);
        }
    }
}
