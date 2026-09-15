using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Services;

public static class EmailTemplateBuilder
{
    public static string FormatSubject(string? venueName, string subject)
    {
        var prefix = string.IsNullOrWhiteSpace(venueName) ? "VenueAxe" : venueName.Trim();
        return $"[{prefix}] {subject.Trim()}";
    }

    public static string BuildBookingConfirmationHtml(
        Venue venue,
        Booking booking,
        IReadOnlyList<int> allocatedLanes,
        string? baseUrl = null)
    {
        var appUrl = (baseUrl ?? "http://localhost:5173").TrimEnd('/');
        var waiverUrl = $"{appUrl}/sign/{venue.Slug}?bookingRef={WebUtility.UrlEncode(booking.BookingReference)}";
        var lanesText = allocatedLanes.Count > 0 ? string.Join(", ", allocatedLanes) : "Assigned at Check-In";
        var dateText = booking.StartTime.ToString("dddd, MMMM d, yyyy");
        var timeText = $"{booking.StartTime:h:mm tt} - {booking.EndTime:h:mm tt}";
        var totalFormatted = $"${booking.TotalAmountCents / 100.0:F2}";
        var paidFormatted = $"${booking.PaidAmountCents / 100.0:F2}";
        var balanceFormatted = $"${Math.Max(0, booking.TotalAmountCents - booking.PaidAmountCents) / 100.0:F2}";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>Reservation Confirmed</title>
  <style>
    body {{ margin:0; padding:0; background-color:#0b0d13; font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#e2e8f0; }}
    table {{ border-collapse:collapse; }}
    .container {{ max-width:600px; margin:0 auto; background-color:#131722; border:1px solid #23293d; border-radius:12px; overflow:hidden; }}
    .header {{ background:linear-gradient(135deg, #1c2237 0%, #0f1320 100%); padding:32px 24px; text-align:center; border-bottom:1px solid #23293d; }}
    .badge {{ display:inline-block; padding:4px 12px; background-color:#e11d48; color:#ffffff; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px; border-radius:999px; margin-bottom:12px; }}
    .title {{ margin:0 0 8px; font-size:24px; font-weight:800; color:#ffffff; }}
    .subtitle {{ margin:0; font-size:14px; color:#94a3b8; }}
    .content {{ padding:24px; }}
    .card {{ background-color:#1a2030; border:1px solid #28324a; border-radius:8px; padding:16px; margin-bottom:20px; }}
    .card-title {{ margin:0 0 12px; font-size:14px; font-weight:700; color:#38bdf8; text-transform:uppercase; letter-spacing:0.5px; }}
    .data-row {{ display:flex; justify-content:space-between; padding:8px 0; border-bottom:1px solid #28324a; font-size:14px; }}
    .data-row:last-child {{ border-bottom:none; }}
    .data-label {{ color:#94a3b8; }}
    .data-val {{ font-weight:600; color:#f8fafc; text-align:right; }}
    .ref-box {{ background:#1e293b; border:2px dashed #f59e0b; border-radius:8px; padding:14px; text-align:center; margin:18px 0; }}
    .ref-label {{ font-size:12px; font-weight:600; text-transform:uppercase; color:#f59e0b; letter-spacing:1px; margin-bottom:4px; }}
    .ref-code {{ font-size:26px; font-weight:900; color:#ffffff; letter-spacing:2px; font-family:monospace; }}
    .alert-box {{ background-color:#451a03; border:1px solid #b45309; border-radius:8px; padding:14px; margin-bottom:20px; color:#fed7aa; font-size:13px; line-height:1.5; }}
    .btn {{ display:block; width:100%; box-sizing:border-box; background-color:#e11d48; color:#ffffff; text-align:center; padding:14px 20px; font-size:15px; font-weight:700; text-decoration:none; border-radius:8px; margin:16px 0 8px; }}
    .waiver-box {{ background-color:#0f2b1d; border:1px solid #059669; border-radius:8px; padding:16px; margin-bottom:20px; text-align:center; }}
    .footer {{ padding:20px 24px; text-align:center; font-size:12px; color:#64748b; border-top:1px solid #23293d; background-color:#0f1320; }}
  </style>
</head>
<body>
  <div style=""padding:24px 12px;"">
    <div class=""container"">
      <div class=""header"">
        {RenderVenueIcon(venue, baseUrl)}
        <span class=""badge"">{WebUtility.HtmlEncode(venue.Name)}</span>
        <h1 class=""title"">Axe Throwing Reservation Confirmed</h1>
        <p class=""subtitle"">We're stoked to host you! Here are your reservation details.</p>
      </div>

      <div class=""content"">
        <div class=""ref-box"">
          <div class=""ref-label"">Booking Reference</div>
          <div class=""ref-code"">{WebUtility.HtmlEncode(booking.BookingReference)}</div>
        </div>

        <div class=""card"">
          <div class=""card-title"">Event Schedule & Location</div>
          <div class=""data-row"">
            <span class=""data-label"">Date</span>
            <span class=""data-val"">{WebUtility.HtmlEncode(dateText)}</span>
          </div>
          <div class=""data-row"">
            <span class=""data-label"">Time</span>
            <span class=""data-val"">{WebUtility.HtmlEncode(timeText)}</span>
          </div>
          <div class=""data-row"">
            <span class=""data-label"">Assigned Lane(s)</span>
            <span class=""data-val"">Lane {WebUtility.HtmlEncode(lanesText)}</span>
          </div>
          <div class=""data-row"">
            <span class=""data-label"">Party Size</span>
            <span class=""data-val"">{booking.PartySize} Throwers</span>
          </div>
          <div class=""data-row"">
            <span class=""data-label"">Location</span>
            <span class=""data-val"">{WebUtility.HtmlEncode(venue.Name)}<br><span style=""font-size:12px; color:#94a3b8;"">{WebUtility.HtmlEncode(venue.AddressLine1)}, {WebUtility.HtmlEncode(venue.City)}, {WebUtility.HtmlEncode(venue.State)}</span></span>
          </div>
        </div>

        <div class=""alert-box"">
          <strong>⚠️ MANDATORY FOOTWEAR REQUIREMENT:</strong><br>
          Closed-toe shoes are strictly required for all throwers in your party. No sandals, flip-flops, crocs, or open-toe shoes are permitted inside throwing lanes.
        </div>

        <div class=""waiver-box"">
          <h3 style=""margin:0 0 6px; color:#34d399; font-size:16px;"">📋 Digital Waiver Required</h3>
          <p style=""margin:0 0 12px; font-size:13px; color:#a7f3d0;"">Every person in your group must sign a digital safety waiver prior to throwing. Sign online now to skip the line!</p>
          <a href=""{waiverUrl}"" class=""btn"" style=""background-color:#10b981;"">Sign Safety Waiver Now</a>
          <p style=""margin:8px 0 0; font-size:11px; color:#6ee7b7; word-break:break-all;"">Or share this link with your party: <br><code>{waiverUrl}</code></p>
        </div>

        <div class=""card"">
          <div class=""card-title"">Payment Receipt</div>
          <div class=""data-row"">
            <span class=""data-label"">Total Reservation</span>
            <span class=""data-val"">{totalFormatted}</span>
          </div>
          <div class=""data-row"">
            <span class=""data-label"">Deposit Paid</span>
            <span class=""data-val"" style=""color:#34d399;"">{paidFormatted}</span>
          </div>
          <div class=""data-row"">
            <span class=""data-label"">Remaining Balance (Due on Arrival)</span>
            <span class=""data-val"">{balanceFormatted}</span>
          </div>
        </div>
      </div>

      <div class=""footer"">
        <p style=""margin:0 0 6px;""><strong>{WebUtility.HtmlEncode(venue.Name)}</strong> &bull; {WebUtility.HtmlEncode(venue.Phone)} &bull; {WebUtility.HtmlEncode(venue.Email)}</p>
        <p style=""margin:0;"">Powered by VenueAxe Commercial Axe Management Platform</p>
      </div>
    </div>
  </div>
</body>
</html>";
    }

    public static string BuildWaiverConfirmationHtml(
        Venue venue,
        Waiver waiver,
        string? baseUrl = null)
    {
        var appUrl = (baseUrl ?? "http://localhost:5173").TrimEnd('/');
        var signedDate = waiver.SignedAtUtc.ToString("dddd, MMMM d, yyyy 'at' h:mm tt 'UTC'");
        var expiresDate = waiver.ExpiresAtUtc.ToString("MMMM d, yyyy");

        string minorsHtml = "";
        if (!string.IsNullOrWhiteSpace(waiver.MinorsCoveredJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(waiver.MinorsCoveredJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var names = new List<string>();
                    foreach (var m in doc.RootElement.EnumerateArray())
                    {
                        if (m.TryGetProperty("name", out var nProp))
                        {
                            names.Add(nProp.GetString() ?? "");
                        }
                    }
                    if (names.Count > 0)
                    {
                        minorsHtml = $@"<div class=""data-row""><span class=""data-label"">Minors Covered</span><span class=""data-val"">{WebUtility.HtmlEncode(string.Join(", ", names))}</span></div>";
                    }
                }
            }
            catch {}
        }

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <title>Safety Waiver Verified</title>
  <style>
    body {{ margin:0; padding:0; background-color:#0b0d13; font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#e2e8f0; }}
    .container {{ max-width:600px; margin:24px auto; background-color:#131722; border:1px solid #23293d; border-radius:12px; overflow:hidden; }}
    .header {{ background:linear-gradient(135deg, #064e3b 0%, #0f1320 100%); padding:28px 24px; text-align:center; border-bottom:1px solid #23293d; }}
    .badge {{ display:inline-block; padding:4px 12px; background-color:#10b981; color:#ffffff; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px; border-radius:999px; margin-bottom:10px; }}
    .title {{ margin:0 0 6px; font-size:22px; font-weight:800; color:#ffffff; }}
    .content {{ padding:24px; }}
    .card {{ background-color:#1a2030; border:1px solid #28324a; border-radius:8px; padding:16px; margin-bottom:20px; }}
    .card-title {{ margin:0 0 12px; font-size:13px; font-weight:700; color:#34d399; text-transform:uppercase; letter-spacing:0.5px; }}
    .data-row {{ display:flex; justify-content:space-between; padding:8px 0; border-bottom:1px solid #28324a; font-size:14px; }}
    .data-row:last-child {{ border-bottom:none; }}
    .data-label {{ color:#94a3b8; }}
    .data-val {{ font-weight:600; color:#f8fafc; text-align:right; }}
    .hash-box {{ background-color:#0b0d13; border:1px solid #1e293b; border-radius:6px; padding:10px; font-family:monospace; font-size:11px; color:#94a3b8; word-break:break-all; text-align:center; }}
    .footer {{ padding:18px 24px; text-align:center; font-size:12px; color:#64748b; border-top:1px solid #23293d; background-color:#0f1320; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      {RenderVenueIcon(venue, baseUrl)}
      <span class=""badge"">{WebUtility.HtmlEncode(venue.Name)}</span>
      <h1 class=""title"">Digital Safety Waiver Verified</h1>
      <p style=""margin:0; font-size:13px; color:#a7f3d0;"">Your legal release has been securely recorded and verified.</p>
    </div>
    <div class=""content"">
      <div class=""card"">
        <div class=""card-title"">Signer Identity</div>
        <div class=""data-row"">
          <span class=""data-label"">Full Legal Name</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(waiver.SignerFirstName)} {WebUtility.HtmlEncode(waiver.SignerLastName)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Email Address</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(waiver.SignerEmail)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Date of Birth</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(waiver.DateOfBirth.ToString("yyyy-MM-dd"))}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Signing Capacity</span>
          <span class=""data-val"">{(waiver.IsGuardianSigning ? "Legal Parent / Guardian" : "Adult Participant")}</span>
        </div>
        {minorsHtml}
        <div class=""data-row"">
          <span class=""data-label"">Signed Timestamp</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(signedDate)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Valid Until</span>
          <span class=""data-val"" style=""color:#34d399;"">{WebUtility.HtmlEncode(expiresDate)}</span>
        </div>
      </div>

      <div style=""margin-bottom:18px;"">
        <div style=""font-size:12px; color:#94a3b8; margin-bottom:6px; font-weight:600;"">Digital Signature SHA-256 Audit Stamp:</div>
        <div class=""hash-box"">{WebUtility.HtmlEncode(waiver.Id.ToString())}</div>
      </div>

      <p style=""font-size:13px; color:#94a3b8; line-height:1.5; margin:0;"">
        You are now cleared to throw axes at <strong>{WebUtility.HtmlEncode(venue.Name)}</strong>! Please remember to wear closed-toe shoes and arrive 10 minutes before your scheduled lane session.
      </p>
    </div>
    <div class=""footer"">
      <p style=""margin:0 0 4px;""><strong>{WebUtility.HtmlEncode(venue.Name)}</strong> &bull; {WebUtility.HtmlEncode(venue.Email)}</p>
      <p style=""margin:0;"">VenueAxe Waiver Vault &bull; Immutable Digital Record</p>
    </div>
  </div>
</body>
</html>";
    }

    public static string BuildBookingCancellationHtml(
        Venue venue,
        Booking booking)
    {
        var dateText = booking.StartTime.ToString("dddd, MMMM d, yyyy");
        var timeText = $"{booking.StartTime:h:mm tt} - {booking.EndTime:h:mm tt}";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <title>Reservation Cancelled</title>
  <style>
    body {{ margin:0; padding:0; background-color:#0b0d13; font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#e2e8f0; }}
    .container {{ max-width:600px; margin:24px auto; background-color:#131722; border:1px solid #23293d; border-radius:12px; overflow:hidden; }}
    .header {{ background:linear-gradient(135deg, #450a0a 0%, #0f1320 100%); padding:28px 24px; text-align:center; border-bottom:1px solid #23293d; }}
    .badge {{ display:inline-block; padding:4px 12px; background-color:#ef4444; color:#ffffff; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px; border-radius:999px; margin-bottom:10px; }}
    .title {{ margin:0 0 6px; font-size:22px; font-weight:800; color:#ffffff; }}
    .content {{ padding:24px; }}
    .card {{ background-color:#1a2030; border:1px solid #28324a; border-radius:8px; padding:16px; margin-bottom:20px; }}
    .data-row {{ display:flex; justify-content:space-between; padding:8px 0; border-bottom:1px solid #28324a; font-size:14px; }}
    .data-row:last-child {{ border-bottom:none; }}
    .data-label {{ color:#94a3b8; }}
    .data-val {{ font-weight:600; color:#f8fafc; text-align:right; }}
    .footer {{ padding:18px 24px; text-align:center; font-size:12px; color:#64748b; border-top:1px solid #23293d; background-color:#0f1320; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      {RenderVenueIcon(venue)}
      <span class=""badge"">{WebUtility.HtmlEncode(venue.Name)}</span>
      <h1 class=""title"">Reservation Cancelled</h1>
      <p style=""margin:0; font-size:13px; color:#fca5a5;"">Your axe throwing reservation has been cancelled.</p>
    </div>
    <div class=""content"">
      <div class=""card"">
        <div class=""data-row"">
          <span class=""data-label"">Booking Reference</span>
          <span class=""data-val"" style=""font-family:monospace;"">{WebUtility.HtmlEncode(booking.BookingReference)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Original Date</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(dateText)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Original Time</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(timeText)}</span>
        </div>
      </div>
      <p style=""font-size:14px; color:#cbd5e1; line-height:1.5; margin:0;"">
        If you have any questions, need a refund review, or wish to reschedule your session, please contact us directly at <strong>{WebUtility.HtmlEncode(venue.Phone)}</strong> or email <strong>{WebUtility.HtmlEncode(venue.Email)}</strong>.
      </p>
    </div>
    <div class=""footer"">
      <p style=""margin:0;"">{WebUtility.HtmlEncode(venue.Name)} &bull; VenueAxe Management</p>
    </div>
  </div>
</body>
</html>";
    }

    public static string BuildTestEmailHtml(string? venueName, string server, int port, string sender)
    {
        var displayVenue = string.IsNullOrWhiteSpace(venueName) ? "VenueAxe" : venueName.Trim();
        var nowUtc = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <title>SMTP Test Email</title>
  <style>
    body {{ margin:0; padding:0; background-color:#0b0d13; font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#e2e8f0; }}
    .container {{ max-width:560px; margin:24px auto; background-color:#131722; border:1px solid #23293d; border-radius:12px; overflow:hidden; }}
    .header {{ background:linear-gradient(135deg, #1e1b4b 0%, #0f1320 100%); padding:28px 24px; text-align:center; border-bottom:1px solid #23293d; }}
    .badge {{ display:inline-block; padding:4px 12px; background-color:#6366f1; color:#ffffff; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px; border-radius:999px; margin-bottom:10px; }}
    .title {{ margin:0 0 6px; font-size:22px; font-weight:800; color:#ffffff; }}
    .content {{ padding:24px; }}
    .card {{ background-color:#1a2030; border:1px solid #28324a; border-radius:8px; padding:16px; margin-bottom:16px; }}
    .data-row {{ display:flex; justify-content:space-between; padding:8px 0; border-bottom:1px solid #28324a; font-size:13px; }}
    .data-row:last-child {{ border-bottom:none; }}
    .data-label {{ color:#94a3b8; }}
    .data-val {{ font-weight:600; color:#f8fafc; font-family:monospace; }}
    .footer {{ padding:16px 24px; text-align:center; font-size:12px; color:#64748b; border-top:1px solid #23293d; background-color:#0f1320; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      <span class=""badge"">{WebUtility.HtmlEncode(displayVenue)}</span>
      <h1 class=""title"">SMTP Connection Verified</h1>
      <p style=""margin:0; font-size:13px; color:#c7d2fe;"">Your VenueAxe transactional email pipeline is operating normally.</p>
    </div>
    <div class=""content"">
      <div class=""card"">
        <div class=""data-row"">
          <span class=""data-label"">SMTP Mail Server</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(server)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">SMTP Port / Security</span>
          <span class=""data-val"">{port} (SSL on Connect)</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Sender Account</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(sender)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Dispatch Time</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(nowUtc)}</span>
        </div>
      </div>
      <p style=""margin:0; font-size:13px; color:#94a3b8; line-height:1.5;"">
        This test confirms that your VenueAxe installation can successfully connect, authenticate, and deliver emails through your configured mail server.
      </p>
    </div>
    <div class=""footer"">
      <p style=""margin:0;"">VenueAxe Commercial Axe Management Platform &bull; System Diagnostics</p>
    </div>
  </div>
</body>
</html>";
    }

    public static string BuildAdminReservationHtml(
        Venue venue,
        Booking booking,
        IReadOnlyList<int> allocatedLanes)
    {
        var lanesText = allocatedLanes.Count > 0 ? string.Join(", ", allocatedLanes) : "Pending Assignment";
        var dateText = booking.StartTime.ToString("dddd, MMMM d, yyyy");
        var timeText = $"{booking.StartTime:h:mm tt} - {booking.EndTime:h:mm tt}";
        var totalFormatted = $"${booking.TotalAmountCents / 100.0:F2}";
        var paidFormatted = $"${booking.PaidAmountCents / 100.0:F2}";
        var balanceFormatted = $"${Math.Max(0, booking.TotalAmountCents - booking.PaidAmountCents) / 100.0:F2}";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <title>New Reservation Alert</title>
  <style>
    body {{ margin:0; padding:0; background-color:#0b0d13; font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#e2e8f0; }}
    .container {{ max-width:600px; margin:20px auto; background-color:#131722; border:1px solid #23293d; border-radius:12px; overflow:hidden; }}
    .header {{ background:linear-gradient(135deg, #065f46 0%, #0f1320 100%); padding:28px 24px; text-align:center; border-bottom:1px solid #23293d; }}
    .badge {{ display:inline-block; padding:4px 12px; background-color:#10b981; color:#ffffff; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px; border-radius:999px; margin-bottom:10px; }}
    .title {{ margin:0 0 6px; font-size:22px; font-weight:800; color:#ffffff; }}
    .content {{ padding:24px; }}
    .card {{ background-color:#1a2030; border:1px solid #28324a; border-radius:8px; padding:16px; margin-bottom:16px; }}
    .card-title {{ margin:0 0 10px; font-size:13px; font-weight:700; color:#38bdf8; text-transform:uppercase; letter-spacing:0.5px; }}
    .data-row {{ display:flex; justify-content:space-between; padding:8px 0; border-bottom:1px solid #28324a; font-size:14px; }}
    .data-row:last-child {{ border-bottom:none; }}
    .data-label {{ color:#94a3b8; }}
    .data-val {{ font-weight:600; color:#f8fafc; text-align:right; }}
    .footer {{ padding:16px 24px; text-align:center; font-size:12px; color:#64748b; border-top:1px solid #23293d; background-color:#0f1320; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      {RenderVenueIcon(venue)}
      <span class=""badge"">{WebUtility.HtmlEncode(venue.Name)} &bull; Floor Alert</span>
      <h1 class=""title"">🚨 New Reservation Received</h1>
      <p style=""margin:0; font-size:14px; color:#a7f3d0;"">Booking #{WebUtility.HtmlEncode(booking.BookingReference)}</p>
    </div>
    <div class=""content"">
      <div class=""card"">
        <h3 class=""card-title"">Guest Contact</h3>
        <div class=""data-row"">
          <span class=""data-label"">Guest Name</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(booking.GuestFirstName)} {WebUtility.HtmlEncode(booking.GuestLastName)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Email</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(booking.GuestEmail)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Phone</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(booking.GuestPhone)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Marketing Opt-In</span>
          <span class=""data-val"">{(booking.EmailMarketingOptIn ? "Yes (Subscribed)" : "No")}</span>
        </div>
      </div>

      <div class=""card"">
        <h3 class=""card-title"">Session Details</h3>
        <div class=""data-row"">
          <span class=""data-label"">Date</span>
          <span class=""data-val"">{dateText}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Time Slot</span>
          <span class=""data-val"">{timeText}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Party Size</span>
          <span class=""data-val"">{booking.PartySize} Throwers</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Assigned Bays</span>
          <span class=""data-val"">Bays {lanesText}</span>
        </div>
      </div>

      <div class=""card"">
        <h3 class=""card-title"">Payment & Financials</h3>
        <div class=""data-row"">
          <span class=""data-label"">Total Amount</span>
          <span class=""data-val"">{totalFormatted}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Amount Collected</span>
          <span class=""data-val"" style=""color:#34d399;"">{paidFormatted}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Balance Due</span>
          <span class=""data-val"">{balanceFormatted}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Payment Status</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(booking.PaymentStatus)}</span>
        </div>
      </div>

      {(string.IsNullOrWhiteSpace(booking.Notes) ? "" : $@"<div class=""card""><h3 class=""card-title"">Special Notes</h3><p style=""margin:0; font-size:14px; color:#cbd5e1;"">{WebUtility.HtmlEncode(booking.Notes)}</p></div>")}
    </div>
    <div class=""footer"">
      <p style=""margin:0;"">VenueAxe Commercial Axe Management Platform &bull; Real-time Operations</p>
    </div>
  </div>
</body>
</html>";
    }

    public static string BuildAdminCancellationHtml(
        Venue venue,
        Booking booking)
    {
        var dateText = booking.StartTime.ToString("dddd, MMMM d, yyyy");
        var timeText = $"{booking.StartTime:h:mm tt} - {booking.EndTime:h:mm tt}";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <title>Reservation Cancelled</title>
  <style>
    body {{ margin:0; padding:0; background-color:#0b0d13; font-family:-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color:#e2e8f0; }}
    .container {{ max-width:600px; margin:20px auto; background-color:#131722; border:1px solid #23293d; border-radius:12px; overflow:hidden; }}
    .header {{ background:linear-gradient(135deg, #7f1d1d 0%, #0f1320 100%); padding:28px 24px; text-align:center; border-bottom:1px solid #23293d; }}
    .badge {{ display:inline-block; padding:4px 12px; background-color:#ef4444; color:#ffffff; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px; border-radius:999px; margin-bottom:10px; }}
    .title {{ margin:0 0 6px; font-size:22px; font-weight:800; color:#ffffff; }}
    .content {{ padding:24px; }}
    .card {{ background-color:#1a2030; border:1px solid #28324a; border-radius:8px; padding:16px; margin-bottom:16px; }}
    .card-title {{ margin:0 0 10px; font-size:13px; font-weight:700; color:#f87171; text-transform:uppercase; letter-spacing:0.5px; }}
    .data-row {{ display:flex; justify-content:space-between; padding:8px 0; border-bottom:1px solid #28324a; font-size:14px; }}
    .data-row:last-child {{ border-bottom:none; }}
    .data-label {{ color:#94a3b8; }}
    .data-val {{ font-weight:600; color:#f8fafc; text-align:right; }}
    .footer {{ padding:16px 24px; text-align:center; font-size:12px; color:#64748b; border-top:1px solid #23293d; background-color:#0f1320; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      {RenderVenueIcon(venue)}
      <span class=""badge"">{WebUtility.HtmlEncode(venue.Name)} &bull; Operations Notice</span>
      <h1 class=""title"">⚠️ Reservation Cancelled</h1>
      <p style=""margin:0; font-size:14px; color:#fca5a5;"">Booking #{WebUtility.HtmlEncode(booking.BookingReference)} has been cancelled</p>
    </div>
    <div class=""content"">
      <div class=""card"">
        <h3 class=""card-title"">Cancelled Reservation Details</h3>
        <div class=""data-row"">
          <span class=""data-label"">Guest</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(booking.GuestFirstName)} {WebUtility.HtmlEncode(booking.GuestLastName)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Email</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(booking.GuestEmail)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Phone</span>
          <span class=""data-val"">{WebUtility.HtmlEncode(booking.GuestPhone)}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Date</span>
          <span class=""data-val"">{dateText}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Time Slot</span>
          <span class=""data-val"">{timeText}</span>
        </div>
        <div class=""data-row"">
          <span class=""data-label"">Party Size</span>
          <span class=""data-val"">{booking.PartySize} Throwers</span>
        </div>
      </div>
      <p style=""margin:0; font-size:13px; color:#94a3b8; line-height:1.5;"">
        The previously assigned lanes for this booking have been released back into the availability pool.
      </p>
    </div>
    <div class=""footer"">
      <p style=""margin:0;"">VenueAxe Commercial Axe Management Platform</p>
    </div>
  </div>
</body>
</html>";
    }

    private static string RenderVenueIcon(Venue? venue, string? baseUrl = null)
    {
        if (venue == null || string.IsNullOrWhiteSpace(venue.IconUrl)) return string.Empty;
        var appUrl = (baseUrl ?? "http://localhost:5173").TrimEnd('/');
        var iconUrl = venue.IconUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? venue.IconUrl
            : $"{appUrl}{venue.IconUrl}";
        return $@"<div style=""margin-bottom:12px;""><img src=""{WebUtility.HtmlEncode(iconUrl)}"" alt=""{WebUtility.HtmlEncode(venue.Name)}"" width=""56"" height=""56"" style=""width:56px; height:56px; border-radius:12px; object-fit:contain; background:#1e293b; border:1px solid #334155; display:inline-block;"" /></div>";
    }
}
