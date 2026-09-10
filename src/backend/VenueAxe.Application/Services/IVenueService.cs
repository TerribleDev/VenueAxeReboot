using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Repositories;

namespace VenueAxe.Services;
public interface IVenueService
{
    Task<IReadOnlyList<VenueDto>> GetAllVenuesAsync();
    Task<VenueDto?> GetVenueByIdAsync(Guid venueId);
    Task<VenueDto> CreateVenueAsync(CreateVenueRequest request);
    Task<VenueDto?> UpdateVenueAsync(Guid venueId, UpdateVenueRequest request);
    Task<BookingConfigDto?> GetBookingConfigAsync(Guid venueId);
    Task<BookingConfigDto?> UpdateBookingConfigAsync(Guid venueId, UpdateBookingConfigRequest request);
}
