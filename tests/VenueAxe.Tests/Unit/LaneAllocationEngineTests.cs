using System;
using System.Collections.Generic;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class LaneAllocationEngineTests
{
    private static Lane CreateLane(int laneNumber, int capacity = 10)
    {
        return new Lane
        {
            Id = Guid.NewGuid(),
            LaneNumber = laneNumber,
            Name = $"Lane {laneNumber:D2}",
            MaxThrowers = capacity,
            IsActive = true,
            CurrentStatus = LaneStatus.Available
        };
    }

    [Fact]
    public void AllocateContiguousLanes_SingleLaneCapacitySufficient_AllocatesSingleLane()
    {
        var lanes = new List<Lane>
        {
            CreateLane(1, 10),
            CreateLane(2, 10),
            CreateLane(3, 10)
        };

        var result = LaneAllocationEngine.AllocateContiguousLanes(lanes, new List<Booking>(), 6);

        Assert.True(result.IsSuccess);
        Assert.Single(result.AllocatedLanes);
        Assert.Equal(1, result.AllocatedLanes[0].LaneNumber);
    }

    [Fact]
    public void AllocateContiguousLanes_12PersonGroup_TwoAdjacent10PersonLanes_Succeeds()
    {
        // Example from spec:
        // Group of 12. Lanes 1 & 2 free, each supports 10.
        // Booking should work because 1 & 2 are contiguous and total capacity 20 >= 12.
        var lanes = new List<Lane>
        {
            CreateLane(1, 10),
            CreateLane(2, 10),
            CreateLane(3, 10)
        };

        var result = LaneAllocationEngine.AllocateContiguousLanes(lanes, new List<Booking>(), 12);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.AllocatedLanes.Count);
        Assert.Equal(1, result.AllocatedLanes[0].LaneNumber);
        Assert.Equal(2, result.AllocatedLanes[1].LaneNumber);
    }

    [Fact]
    public void AllocateContiguousLanes_12PersonGroup_Lane2Booked_Lanes1And3Free_FailsDueToNonContiguous()
    {
        // Constraint from user prompt:
        // "However, if lane 2 is taken but lane 1 & 3 work, you still shouldn't be able to book as the spots need to exist next to each other."
        var lane1 = CreateLane(1, 10);
        var lane2 = CreateLane(2, 10);
        var lane3 = CreateLane(3, 10);

        var overlappingBooking = new Booking
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane>
            {
                new() { LaneId = lane2.Id }
            }
        };

        var lanes = new List<Lane> { lane1, lane2, lane3 };
        var overlapping = new List<Booking> { overlappingBooking };

        var result = LaneAllocationEngine.AllocateContiguousLanes(lanes, overlapping, 12);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.AllocatedLanes);
        Assert.Contains("non-adjacent", result.FailureReason?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public void AllocateContiguousLanes_ContiguousBlockAvailableLaterInSequence_Succeeds()
    {
        // Lanes 1 & 2 booked, Lanes 3 & 4 free (each capacity 8), party size 14
        var lane1 = CreateLane(1, 8);
        var lane2 = CreateLane(2, 8);
        var lane3 = CreateLane(3, 8);
        var lane4 = CreateLane(4, 8);

        var booking1 = new Booking
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane>
            {
                new() { LaneId = lane1.Id },
                new() { LaneId = lane2.Id }
            }
        };

        var lanes = new List<Lane> { lane1, lane2, lane3, lane4 };
        var overlapping = new List<Booking> { booking1 };

        var result = LaneAllocationEngine.AllocateContiguousLanes(lanes, overlapping, 14);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.AllocatedLanes.Count);
        Assert.Equal(3, result.AllocatedLanes[0].LaneNumber);
        Assert.Equal(4, result.AllocatedLanes[1].LaneNumber);
    }

    [Fact]
    public void AllocateContiguousLanes_TotalVenueCapacityExceeded_FailsGracefully()
    {
        var lanes = new List<Lane>
        {
            CreateLane(1, 6),
            CreateLane(2, 6)
        };

        var result = LaneAllocationEngine.AllocateContiguousLanes(lanes, new List<Booking>(), 15);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.AllocatedLanes);
    }
}
