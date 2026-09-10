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
public interface IBookingService
{
    Task<PublicVenueBookingPageDto?> GetPublicBookingPageAsync(string venueSlug);
    Task<IReadOnlyList<TimeSlotDto>> CheckAvailabilityAsync(string venueSlug, AvailabilityQuery query);
    Task<PricingBreakdownDto?> CalculatePricingAsync(string venueSlug, CalculatePriceRequest request);
    Task<BookingDto?> CreateGuestBookingAsync(string venueSlug, CreateBookingRequest request);
    Task<BookingDto?> CreateAdminBookingAsync(CreateAdminBookingRequest request);
    Task<IReadOnlyList<BookingDto>> GetVenueBookingsAsync(Guid venueId, DateOnly? date);
    Task<LaneScheduleMatrixDto?> GetLaneScheduleMatrixAsync(Guid venueId, DateOnly date);
    Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status);
    Task<BookingDto?> GetBookingByReferenceAsync(string reference);
    Task<bool> ProcessSquareWebhookAsync(string paymentId, string status, string? referenceId, string? orderId, int amountCents);
    Task<BookingDto?> ReassignBookingLaneAsync(Guid bookingId, Guid targetLaneId);
    Task<BookingDto?> CollectPaymentAsync(Guid bookingId, int? amountCents, string? paymentMethod);
}
