using System;
using System.Collections.Generic;

namespace VenueAxe.DTOs;

public record LaneScheduleMatrixDto(
    DateOnly Date,
    List<LaneDto> Lanes,
    List<ScheduleBookingBlockDto> Bookings,
    string BusinessHoursJson
);