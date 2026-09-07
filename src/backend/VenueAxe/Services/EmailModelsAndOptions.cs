using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Services;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "mail.tommyparnell.com";
    public int Port { get; set; } = 465;
    public string Username { get; set; } = "bot@tommyparnell.com";
    public string Password { get; set; } = "J5dHmgc14y6Cq7B2puhjHf";
    public string FromEmail { get; set; } = "bot@tommyparnell.com";
    public string FromName { get; set; } = "VenueAxe";
    public bool EnableSsl { get; set; } = true;
}

public interface IEmailService
{
    /// <summary>
    /// Sends an arbitrary email with the specified recipient, subject, and body.
    /// If venueName is provided, the subject will automatically be prefixed with [VenueName].
    /// </summary>
    Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? venueName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a booking confirmation email to the guest with reservation summary, lane numbers,
    /// mandatory footwear warnings, and waiver signing link.
    /// </summary>
    Task<bool> SendBookingConfirmationAsync(
        Venue venue,
        Booking booking,
        IReadOnlyList<int> allocatedLanes,
        string? baseUrl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a waiver confirmation email to the signer with legal audit stamp and minor participants covered.
    /// </summary>
    Task<bool> SendWaiverConfirmationAsync(
        Venue venue,
        Waiver waiver,
        string? baseUrl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a booking cancellation notice to the guest.
    /// </summary>
    Task<bool> SendBookingCancellationAsync(
        Venue venue,
        Booking booking,
        CancellationToken ct = default);

    /// <summary>
    /// Sends an administrative test email to verify live SMTP reachability.
    /// </summary>
    Task<bool> SendTestEmailAsync(
        string toEmail,
        string? venueName = null,
        CancellationToken ct = default);
}

public record SendTestEmailRequest(string ToEmail, string? VenueName);
public record SendTestEmailResponse(bool Success, string Message, string? Server, int Port, string Sender);
