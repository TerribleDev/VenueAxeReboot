# VenueAxe — Best Practice Violations (Executive Summary)

**Audit Date**: 2026-09-14
**Auditor**: Enterprise Architecture Review

---

## 1. Empty `catch {}` Blocks Swallow Exceptions

| Field | Value |
|:------|:------|
| **Standard** | .NET CA1031: Do not catch general exception types |
| **Locations** | `BookingConfig.cs:37,50`, `LaneService.cs:58-61,82-84,168-170,183-184` |

**Problem**: Multiple `catch {}` and `catch { }` blocks silently swallow exceptions with no logging, telemetry, or error handling. In `BookingConfig.ShowAddress`, a malformed JSON string is silently ignored. In `LaneService`, database query failures are silently swallowed with fallback to empty arrays.

**Impact**: Silent data corruption, invisible failures in production, impossible to diagnose issues without reproduction.

**Recommendation**: At minimum, log the exception. Replace empty catches with `catch (Exception ex) { _logger.LogWarning(ex, "..."); }` or use structured fallback with explicit documentation of why the exception is safe to ignore.

---

## 2. Domain Entity Contains Framework Dependencies

| Field | Value |
|:------|:------|
| **Standard** | Clean Architecture: Domain layer must have zero external framework dependencies |
| **Location** | `BookingConfig.cs:23-55` |

**Problem**: `BookingConfig.ShowAddress` is a `[NotMapped]` computed property that uses `System.Text.Json.JsonDocument.Parse()` and `System.Text.Json.JsonSerializer.Deserialize()` directly inside a domain entity. The domain project (`VenueAxe`) is supposed to have zero external dependencies.

**Impact**: Couples domain logic to a specific JSON library. Makes the entity harder to test in isolation and violates the stated architectural boundary.

**Recommendation**: Move JSON parsing logic to a service or value object in the Application layer. The domain entity should store the raw JSON string; parsing/interpretation should happen in services.

---

## 3. Security Claims Dependency in Domain Project

| Field | Value |
|:------|:------|
| **Standard** | Clean Architecture: Domain must not depend on ASP.NET primitives |
| **Location** | `UserContext.cs:2` (`using System.Security.Claims`) |

**Problem**: `UserContext` in the `VenueAxe` (domain) project imports `System.Security.Claims` and has a method `PopulateFromClaims(ClaimsPrincipal)`. This is an ASP.NET-specific concern that belongs in the Web/Infrastructure layer.

**Impact**: The domain project now transitively depends on the ASP.NET security stack, violating the "zero external dependencies" mandate.

**Recommendation**: Move `PopulateFromClaims` to `HttpContextUserContext` in the Web project. The domain `IUserContext` interface should remain clean. `UserContext` base class should only expose `SetManualContext`.

---

## 4. Multiple Types in Single File

| Field | Value |
|:------|:------|
| **Standard** | GEMINI backend §1.2: "Strictly one class, record, struct, interface, or enum per `.cs` file" |
| **Location** | `ReportsDtos.cs` |

**Problem**: `ReportsDtos.cs` bundles multiple DTO record types into a single file (revenue summaries, booking stats, waiver counts, etc.).

**Recommendation**: Split into separate files: `RevenueSummaryDto.cs`, `BookingStatsDto.cs`, `WaiverStatsDto.cs`, etc.

---

## 5. Namespace Doesn't Match Project/Folder Structure

| Field | Value |
|:------|:------|
| **Standard** | .NET convention: namespace should match project and folder path |
| **Locations** | `SmtpEmailService.cs` (namespace `VenueAxe.Services`), `SmtpOptions.cs`, `EmailTemplateBuilder.cs` |

**Problem**: Files in `VenueAxe.Infrastructure/Email/` use namespace `VenueAxe.Services` instead of `VenueAxe.Infrastructure.Email`. This creates confusion about which project the type lives in and can cause circular dependency issues.

**Recommendation**: Update namespaces to match the project and folder structure. Use `VenueAxe.Infrastructure.Email` for infrastructure email types.

---

## 6. Inconsistent `TimeProvider` Usage

| Field | Value |
|:------|:------|
| **Standard** | GEMINI backend §2.2: Testability through dependency injection |
| **Locations** | `WaiverService.cs`, `LaneGameService.cs`, `AuthService.cs`, `SessionLifecycleBackgroundService.cs` |

**Problem**: `BookingService` and `LaneService` properly inject `TimeProvider` and use `_timeProvider.GetUtcNow()`, but `WaiverService`, `LaneGameService`, `AuthService`, and `SessionLifecycleBackgroundService` use `DateTimeOffset.UtcNow` directly. This makes time-dependent logic untestable.

**Impact**: Cannot write deterministic unit tests for:
- Waiver expiration calculations
- Session timing
- Login timestamp recording
- Session lifecycle expiration checks

**Recommendation**: Inject `TimeProvider` into all services. Replace all `DateTimeOffset.UtcNow` with `_timeProvider.GetUtcNow()`.

---

## 7. Missing `CancellationToken` Propagation

| Field | Value |
|:------|:------|
| **Standard** | GEMINI backend §2.2: "Every asynchronous method must accept a `CancellationToken`" |
| **Locations** | `LaneGameService` (all methods), `LaneService` (most methods), `AuthService` (all methods) |

**Problem**: Most service methods do not accept or propagate `CancellationToken`. This means:
- HTTP request cancellation (client disconnect) does not stop in-flight database queries
- Long-running operations cannot be cancelled gracefully
- Memory and CPU are wasted on abandoned requests

**Recommendation**: Add `CancellationToken cancellationToken = default` parameter to all async service methods and pass through to repository/UoW calls. This is a large but mechanical refactor.
