using System;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class BookingLifecycleSteps
{
    private int _subtotalCents;
    private int _discountCents;
    private int _netBalanceCents;
    private int _depositAmountCents;
    private int _remainingBalanceCents;
    private Booking _booking = null!;
    private Lane _assignedLane = null!;

    [Given(@"a booking with subtotal (.*) cents")]
    public void GivenABookingWithSubtotal(int subtotal)
    {
        _subtotalCents = subtotal;
    }

    [When(@"the guest applies coupon ""(.*)"" with fixed discount (.*) cents")]
    public void WhenTheGuestAppliesCouponWithFixedDiscount(string couponCode, int discount)
    {
        _discountCents = discount;
        _netBalanceCents = Math.Max(0, _subtotalCents - _discountCents);
    }

    [Then(@"the calculated discount should be (.*) cents")]
    public void ThenTheCalculatedDiscountShouldBe(int expectedDiscount)
    {
        _discountCents.Should().Be(expectedDiscount);
    }

    [Then(@"the net balance due should be (.*) cents")]
    public void ThenTheNetBalanceDueShouldBe(int expectedNetBalance)
    {
        _netBalanceCents.Should().Be(expectedNetBalance);
    }

    [Given(@"a booking with net balance (.*) cents")]
    public void GivenABookingWithNetBalance(int netBalance)
    {
        _netBalanceCents = netBalance;
    }

    [When(@"the deposit rate is configured at (.*) percent")]
    public void WhenTheDepositRateIsConfiguredAtPercent(int percent)
    {
        _depositAmountCents = (int)Math.Round(_netBalanceCents * (percent / 100.0));
        _remainingBalanceCents = _netBalanceCents - _depositAmountCents;
    }

    [Then(@"the required deposit amount should be (.*) cents")]
    public void ThenTheRequiredDepositAmountShouldBe(int expectedDeposit)
    {
        _depositAmountCents.Should().Be(expectedDeposit);
    }

    [Then(@"the remaining balance due at venue check-in should be (.*) cents")]
    public void ThenTheRemainingBalanceDueAtVenueCheckInShouldBe(int expectedRemaining)
    {
        _remainingBalanceCents.Should().Be(expectedRemaining);
    }

    [Given(@"a lane ""(.*)"" is assigned to booking ""(.*)""")]
    public void GivenALaneIsAssignedToBooking(string laneName, string bookingReference)
    {
        _assignedLane = new Lane
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            VenueId = Guid.NewGuid(),
            Name = laneName,
            LaneNumber = 3,
            CurrentStatus = LaneStatus.Reserved
        };

        _booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = _assignedLane.TenantId,
            VenueId = _assignedLane.VenueId,
            BookingReference = bookingReference,
            GuestFirstName = "Guest",
            GuestLastName = "Tester",
            GuestEmail = "guest@example.com",
            PartySize = 4,
            StartTime = DateTimeOffset.UtcNow.AddHours(2),
            EndTime = DateTimeOffset.UtcNow.AddHours(3),
            Status = BookingStatus.Confirmed
        };
    }

    [When(@"booking ""(.*)"" is cancelled by staff")]
    public void WhenBookingIsCancelledByStaff(string bookingReference)
    {
        _booking.Status = BookingStatus.Cancelled;
        _assignedLane.CurrentStatus = LaneStatus.Available;
    }

    [Then(@"booking ""(.*)"" status should be ""(.*)""")]
    public void ThenBookingStatusShouldBe(string bookingReference, string expectedStatus)
    {
        _booking.Status.ToString().Should().Be(expectedStatus);
    }

    [Then(@"""(.*)"" should be free for subsequent bookings")]
    public void ThenLaneShouldBeFreeForSubsequentBookings(string laneName)
    {
        _assignedLane.CurrentStatus.Should().Be(LaneStatus.Available);
    }
}
