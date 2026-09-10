using System.Collections.Generic;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Services;

public record LaneAllocationResult(
    bool IsSuccess,
    IReadOnlyList<Lane> AllocatedLanes,
    int TotalAvailableLanesCount,
    string? FailureReason = null
);