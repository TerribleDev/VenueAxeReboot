using System;
using System.Collections.Generic;
using System.Linq;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Services;
public static class LaneAllocationEngine
{
    /// <summary>
    /// Evaluates whether a contiguous sequence of available adjacent lanes exists
    /// that can collectively accommodate the requested party size.
    /// Lanes are sequentially numbered (e.g., Lane 1, Lane 2, Lane 3) and physically adjacent.
    /// </summary>
    public static LaneAllocationResult AllocateContiguousLanes(
        IReadOnlyList<Lane> allVenueLanes,
        IReadOnlyList<Booking> overlappingBookings,
        int partySize)
    {
        if (partySize <= 0)
        {
            return new LaneAllocationResult(false, Array.Empty<Lane>(), 0, "Party size must be greater than zero.");
        }

        // 1. Identify all lanes currently occupied during the target window
        var occupiedLaneIds = new HashSet<Guid>();
        foreach (var booking in overlappingBookings)
        {
            if (booking.Status == BookingStatus.Cancelled) continue;
            foreach (var bl in booking.BookingLanes)
            {
                occupiedLaneIds.Add(bl.LaneId);
            }
        }

        // 2. Filter for active, available lanes and sort sequentially by LaneNumber
        var availableLanes = allVenueLanes
            .Where(l => l.IsActive && !occupiedLaneIds.Contains(l.Id))
            .OrderBy(l => l.LaneNumber)
            .ToList();

        if (availableLanes.Count == 0)
        {
            return new LaneAllocationResult(false, Array.Empty<Lane>(), 0, "No lanes are available for this time slot.");
        }

        // 3. Find all contiguous sequences of available lanes that can satisfy partySize
        // Lanes are physically adjacent iff LaneNumber[k+1] == LaneNumber[k] + 1
        var validCandidates = new List<List<Lane>>();

        for (int i = 0; i < availableLanes.Count; i++)
        {
            var currentChain = new List<Lane> { availableLanes[i] };
            int accumulatedCapacity = availableLanes[i].MaxThrowers;

            if (accumulatedCapacity >= partySize)
            {
                validCandidates.Add(currentChain);
                continue;
            }

            for (int j = i + 1; j < availableLanes.Count; j++)
            {
                // Check strict sequential adjacency with previous lane in chain
                if (availableLanes[j].LaneNumber != availableLanes[j - 1].LaneNumber + 1)
                {
                    // Contiguity broken
                    break;
                }

                currentChain.Add(availableLanes[j]);
                accumulatedCapacity += availableLanes[j].MaxThrowers;

                if (accumulatedCapacity >= partySize)
                {
                    validCandidates.Add(currentChain);
                    break; // minimal contiguous sequence starting at i
                }
            }
        }

        if (validCandidates.Count == 0)
        {
            return new LaneAllocationResult(
                false,
                Array.Empty<Lane>(),
                availableLanes.Count,
                $"Insufficient contiguous adjacent lanes to accommodate party of {partySize}. Available bays are non-adjacent or capacity exceeded."
            );
        }

        // Pick optimal candidate: fewest lanes first, then lowest starting lane number
        var optimal = validCandidates
            .OrderBy(c => c.Count)
            .ThenBy(c => c[0].LaneNumber)
            .First();

        return new LaneAllocationResult(true, optimal, availableLanes.Count);
    }
}
