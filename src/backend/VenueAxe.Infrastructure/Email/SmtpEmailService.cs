using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Services;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpOptions>? options,
        ILogger<SmtpEmailService> logger)
    {
        _options = options?.Value ?? new SmtpOptions();
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? venueName = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Email delivery aborted: recipient address is null or empty");
            return false;
        }

        // Format subject with [VenueName] prefix if not already present
        string finalSubject = subject;
        if (!string.IsNullOrWhiteSpace(venueName) && !subject.StartsWith($"[{venueName}]", StringComparison.OrdinalIgnoreCase))
        {
            finalSubject = EmailTemplateBuilder.FormatSubject(venueName, subject);
        }
        else if (!finalSubject.StartsWith("[") && string.IsNullOrWhiteSpace(venueName))
        {
            finalSubject = EmailTemplateBuilder.FormatSubject("VenueAxe", subject);
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail.Trim()));
            message.Subject = finalSubject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = plainTextBody ?? ""
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Port 465 is implicit SSL (SSL on Connect); other ports like 587 use StartTls
            var secureOption = _options.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : (_options.EnableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None);

            _logger.LogInformation("Connecting to SMTP server {Host}:{Port} with SSL option {Security}", _options.Host, _options.Port, secureOption);
            await client.ConnectAsync(_options.Host, _options.Port, secureOption, ct);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Transactional email successfully sent to {Recipient} with subject {Subject}", toEmail, finalSubject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver transactional email to {Recipient} with subject {Subject}", toEmail, finalSubject);
            return false;
        }
    }

    public async Task<bool> SendBookingConfirmationAsync(
        Venue venue,
        Booking booking,
        IReadOnlyList<int> allocatedLanes,
        string? baseUrl = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(booking.GuestEmail))
        {
            _logger.LogInformation("Booking {BookingReference} has no guest email; skipping email confirmation", booking.BookingReference);
            return false;
        }

        var subject = EmailTemplateBuilder.FormatSubject(venue.Name, $"Reservation Confirmed - #{booking.BookingReference}");
        var html = EmailTemplateBuilder.BuildBookingConfirmationHtml(venue, booking, allocatedLanes, baseUrl);

        return await SendEmailAsync(
            booking.GuestEmail,
            subject,
            html,
            venueName: venue.Name,
            ct: ct
        );
    }

    public async Task<bool> SendWaiverConfirmationAsync(
        Venue venue,
        Waiver waiver,
        string? baseUrl = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(waiver.SignerEmail))
        {
            _logger.LogInformation("Waiver {WaiverId} has no signer email; skipping email confirmation", waiver.Id);
            return false;
        }

        var subject = EmailTemplateBuilder.FormatSubject(venue.Name, $"Safety Waiver Verified - {waiver.SignerFirstName} {waiver.SignerLastName}");
        var html = EmailTemplateBuilder.BuildWaiverConfirmationHtml(venue, waiver, baseUrl);

        return await SendEmailAsync(
            waiver.SignerEmail,
            subject,
            html,
            venueName: venue.Name,
            ct: ct
        );
    }

    public async Task<bool> SendBookingCancellationAsync(
        Venue venue,
        Booking booking,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(booking.GuestEmail)) return false;

        var subject = EmailTemplateBuilder.FormatSubject(venue.Name, $"Reservation Cancelled - #{booking.BookingReference}");
        var html = EmailTemplateBuilder.BuildBookingCancellationHtml(venue, booking);

        return await SendEmailAsync(
            booking.GuestEmail,
            subject,
            html,
            venueName: venue.Name,
            ct: ct
        );
    }

    public async Task<bool> SendAdminReservationNotificationAsync(
        Venue venue,
        Booking booking,
        IReadOnlyList<int> allocatedLanes,
        string? adminEmail = null,
        CancellationToken ct = default)
    {
        var targetEmail = !string.IsNullOrWhiteSpace(adminEmail)
            ? adminEmail
            : (!string.IsNullOrWhiteSpace(venue.Email) ? venue.Email : "owner@venueaxe.com");

        var guestName = $"{booking.GuestFirstName} {booking.GuestLastName}".Trim();
        var subject = EmailTemplateBuilder.FormatSubject(venue.Name, $"🚨 New Reservation: #{booking.BookingReference} - {guestName} ({booking.PartySize} Throwers)");
        var html = EmailTemplateBuilder.BuildAdminReservationHtml(venue, booking, allocatedLanes);

        return await SendEmailAsync(
            targetEmail,
            subject,
            html,
            venueName: venue.Name,
            ct: ct
        );
    }

    public async Task<bool> SendAdminCancellationNotificationAsync(
        Venue venue,
        Booking booking,
        string? adminEmail = null,
        CancellationToken ct = default)
    {
        var targetEmail = !string.IsNullOrWhiteSpace(adminEmail)
            ? adminEmail
            : (!string.IsNullOrWhiteSpace(venue.Email) ? venue.Email : "owner@venueaxe.com");

        var guestName = $"{booking.GuestFirstName} {booking.GuestLastName}".Trim();
        var subject = EmailTemplateBuilder.FormatSubject(venue.Name, $"⚠️ Cancellation Notice: #{booking.BookingReference} - {guestName}");
        var html = EmailTemplateBuilder.BuildAdminCancellationHtml(venue, booking);

        return await SendEmailAsync(
            targetEmail,
            subject,
            html,
            venueName: venue.Name,
            ct: ct
        );
    }

    public async Task<bool> SendTestEmailAsync(
        string toEmail,
        string? venueName = null,
        CancellationToken ct = default)
    {
        var subject = EmailTemplateBuilder.FormatSubject(venueName, "Test Transactional Email");
        var html = EmailTemplateBuilder.BuildTestEmailHtml(venueName, _options.Host, _options.Port, _options.FromEmail);

        return await SendEmailAsync(
            toEmail,
            subject,
            html,
            venueName: venueName,
            ct: ct
        );
    }
}
