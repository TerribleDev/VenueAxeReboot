# Email Notifications

## 1. Overview & Business Value
VenueAxe sends transactional emails for booking confirmations, cancellations, waiver confirmations, and admin notifications. Emails are sent via SMTP using MailKit with customizable templates that include venue branding.

### Target Roles
- **Guest Thrower**: Receives booking/waiver confirmation emails
- **Owner/Manager**: Receives admin booking notifications, sends test emails

## 2. Technical Architecture

### Core Service: `SmtpEmailService` (implements `IEmailService`)
Located in `VenueAxe.Infrastructure/Email/` (namespace: `VenueAxe.Services`)

### Email Types
| Method | Purpose |
|:-------|:--------|
| `SendBookingConfirmationAsync` | Guest booking confirmation with lane numbers |
| `SendBookingCancellationAsync` | Guest cancellation notice |
| `SendWaiverConfirmationAsync` | Waiver signing confirmation |
| `SendAdminReservationNotificationAsync` | Admin alert for new bookings |
| `SendAdminCancellationNotificationAsync` | Admin alert for cancellations |
| `SendTestEmailAsync` | SMTP connectivity test |

### SMTP Configuration (`SmtpOptions`)
- Host, Port, Username, Password
- FromEmail, FromName
- EnableSsl (auto-detects: port 465 = SslOnConnect, else StartTlsWhenAvailable)

### Email Templates (`EmailTemplateBuilder`)
- Generates HTML email bodies with venue branding
- Subject lines automatically prefixed with `[VenueName]`
- Includes booking reference, dates, lane numbers, QR codes

### Admin Endpoint
| Method | Route | Purpose |
|:-------|:------|:--------|
| POST | `/api/admin/venues/{id}/test-email` | Send SMTP test email |
| PUT | `/api/admin/venues/{id}/email-config` | Update SMTP settings |

## 3. Testing Strategy

### Backend
- **Unit Tests**: `EmailServiceTests.cs`

### Frontend
- **Tests**: `email.test.ts`

## 4. Manual Verification
1. Open `http://localhost:5173/admin/emails`
2. Configure SMTP settings
3. Send test email → verify delivery
4. Create a booking with guest email → verify confirmation email sent
