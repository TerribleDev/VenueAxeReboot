# VenueAxe — Security Issues (Executive Summary)

**Audit Date**: 2026-09-14
**Auditor**: Enterprise Architecture Review (40yr+ experience)

---

## Critical

### SEC-001: Hardcoded Authentication Backdoor (**FIXED**)

| Field | Value |
|:------|:------|
| **Severity** | 🔴 CRITICAL |
| **Status** | ✅ FIXED |
| **File** | `src/backend/VenueAxe.Application/Services/AuthService.cs` (lines 31-37, now removed) |

**Description**: `AuthenticateAsync()` contained a hardcoded fallback block that accepted plaintext passwords (`password123`, `VenueAxeAdmin2026!#$`) for specific email addresses (`owner@venueaxe.com`, `owner@valhallaaxe.com`) even when the stored bcrypt hash didn't match. On match, it re-hashed and **overwrote** the stored password hash, enabling:

1. **Production authentication bypass** — anyone knowing the seed emails could log in
2. **Password downgrade attack** — legitimate strong passwords silently replaced with `password123`
3. **Audit trail corruption** — `LastLoginAt` updated without valid authentication

**Resolution**: Removed the entire fallback block. The `DbInitializer` seeds accounts with properly PBKDF2-hashed passwords. Login now requires the correct stored hash.

---

## High

### SEC-002: No Rate Limiting on Login Endpoint

| Field | Value |
|:------|:------|
| **Severity** | 🟠 HIGH |
| **File** | `src/backend/VenueAxe.Web/Areas/Admin/Controllers/AuthController.cs` |

**Description**: The `POST /api/admin/auth/login` endpoint has no rate limiting, account lockout, or brute-force protection. An attacker can perform unlimited password guessing attempts against known staff email addresses.

**Recommendation**: Implement ASP.NET Core rate limiting middleware (`builder.Services.AddRateLimiter()`) with:
- Fixed window: max 5 login attempts per IP per 5-minute window
- Account lockout: temporary lock after 10 failed attempts per email address
- CAPTCHA integration for repeated failures

---

### SEC-003: LaneOperationsController Has No `[Authorize]` Attribute

| Field | Value |
|:------|:------|
| **Severity** | 🟠 HIGH |
| **File** | `src/backend/VenueAxe.Web/Areas/Lanes/Controllers/LaneOperationsController.cs` |

**Description**: The controller handles critical operations (start session, record throws, end session, safety stop, substitute players) but has **no `[Authorize]` attribute**. While this may be intentional for unauthenticated tablet terminals, it means:
- Any unauthenticated user can start/stop sessions on any lane
- Safety stops can be triggered by unauthorized parties
- Player rosters can be modified without authentication

**Recommendation**: Either:
1. Add terminal device token authentication for lane hardware
2. Add `[Authorize]` and create device service accounts
3. If intentionally open, document the security boundary explicitly and add per-lane secret tokens

---

## Medium

### SEC-004: Safety Stop Has No Persistence or Audit Trail

| Field | Value |
|:------|:------|
| **Severity** | 🟡 MEDIUM |
| **File** | `src/backend/VenueAxe.Web/Areas/Lanes/Controllers/LaneOperationsController.cs:112-117` |

**Description**: `POST /api/lanes/operations/{laneId}/safety-stop` only broadcasts a SignalR message. There is no:
- Database record of when safety stops were activated
- Record of who triggered the stop
- Audit log for liability and insurance purposes
- Lane status change to `LaneStatus.Frozen` or equivalent

For a venue with physical safety implications, regulatory compliance may require safety incident tracking.

**Recommendation**: Create a `SafetyIncident` entity persisted to the database with timestamp, triggering user, reason, lane, and resolution status.

---

### SEC-005: Stale CORS Origin

| Field | Value |
|:------|:------|
| **Severity** | 🟡 MEDIUM |
| **File** | `src/backend/VenueAxe.Web/Program.cs:124` |

**Description**: CORS policy includes `http://localhost:3000` alongside the expected dev origins (`5173`, `4173`, `127.0.0.1:5173`). Port 3000 is not used by the SvelteKit dev server and may be a leftover from a previous React/Next.js setup or another project.

**Recommendation**: Remove `http://localhost:3000` from the CORS whitelist. In production, use environment-specific CORS configuration.

---

### SEC-006: Scoped Service Leak via Task.Run in WaiverService (**FIXED**)

| Field | Value |
|:------|:------|
| **Severity** | 🟡 MEDIUM |
| **Status** | ✅ FIXED |
| **File** | `src/backend/VenueAxe.Application/Services/WaiverService.cs:164-175` |

**Description**: `Task.Run()` captured scoped `IEmailService` and entity objects from a `DbContext` that could be disposed before the background task executed, causing potential `ObjectDisposedException` errors and lost emails.

**Resolution**: Replaced with inline `await` within the same scope. Email sending is already async and non-blocking.
