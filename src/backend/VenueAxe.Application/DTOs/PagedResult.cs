using System;
using System.Collections.Generic;

namespace VenueAxe.DTOs;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);
