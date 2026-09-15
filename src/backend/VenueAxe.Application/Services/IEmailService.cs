using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? venueName = null,
        CancellationToken ct = default);

    Task<bool> SendBookingConfirmationAsync(
        Venue venue,
        Booking booking,
        IReadOnlyList<int> allocatedLanes,
        string? baseUrl = null,
        CancellationToken ct = default);

    Task<bool> SendWaiverConfirmationAsync(
        Venue venue,
        Waiver waiver,
        string? baseUrl = null,
        CancellationToken ct = default);

    Task<bool> SendBookingCancellationAsync(
        Venue venue,
        Booking booking,
        CancellationToken ct = default);

    Task<bool> SendAdminReservationNotificationAsync(
        Venue venue,
        Booking booking,
        IReadOnlyList<int> allocatedLanes,
        string? adminEmail = null,
        CancellationToken ct = default);

    Task<bool> SendAdminCancellationNotificationAsync(
        Venue venue,
        Booking booking,
        string? adminEmail = null,
        CancellationToken ct = default);

    Task<bool> SendTestEmailAsync(
        string toEmail,
        string? venueName = null,
        CancellationToken ct = default);
}