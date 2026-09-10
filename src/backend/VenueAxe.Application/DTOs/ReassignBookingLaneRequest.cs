using System;

namespace VenueAxe.DTOs;

public record ReassignBookingLaneRequest(
    Guid TargetLaneId
);