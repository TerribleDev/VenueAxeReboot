using System;

namespace VenueAxe.Domain.Entities;

public class BookingLane
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid LaneId { get; set; }
    public Lane? Lane { get; set; }
}
