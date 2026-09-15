# Structured JSON Logging (Serilog)

## 1. Overview & Business Value
VenueAxe is an enterprise-grade venue management operating system designed for high availability, multi-tenancy, and cloud-native observability. In compliance with **12-Factor App Factor XI (Logs as Event Streams)**, VenueAxe utilizes **Serilog** configured with the **Compact Log Event Format (CLEF / RenderedCompactJsonFormatter)** to emit machine-readable, structured JSON logs directly to `stdout`/`stderr`.

### Business Value & Operational Benefits:
- **Centralized Log Ingestion**: Ready for zero-reformatting ingestion into Grafana Loki, Datadog, AWS CloudWatch, Elasticsearch, or Vector collectors.
- **Structured Audit Trails**: Critical security events (login successes/failures, user registrations, session completions) are enriched with discrete properties (`UserId`, `TenantId`, `UserEmail`, `Role`) allowing instantaneous filtering, alerting, and forensics.
- **Operational Diagnostics**: Lane throwing telemetry, session lifecycles, and payment events carry contextual identifiers (`LaneId`, `SessionId`, `BookingReference`, `PaymentId`) to pinpoint lane errors or dropped transactions in milliseconds.
- **High Performance & Low Overhead**: Direct serialization with bootstrap logging prevents performance degradation and captures early startup exceptions before the host DI container is fully instantiated.

---

## 2. Technical Architecture & Data Model

```
+---------------------------------------------------------------------------------------------------+
|                                      APPLICATION SERVICES LAYER                                    |
|   BookingService   -   WaiverService   -   SquarePaymentService   -   SmtpEmailService            |
|   (Injects Microsoft.Extensions.Logging.ILogger<T> with named semantic tokens)                   |
+-------------------------------------------------+-------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
|                                      SERILOG ASP.NET CORE SINK                                    |
|   - Serilog.AspNetCore (v10.0.0)                                                                  |
|   - Serilog.Formatting.Compact (RenderedCompactJsonFormatter)                                     |
|   - Enrichers: FromLogContext, WithProperty("Application", "VenueAxe")                            |
|   - Middleware: app.UseSerilogRequestLogging()                                                    |
+-------------------------------------------------+-------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
|                                      STRUCTURED JSON STREAM (stdout)                              |
|   {"@t":"...", "@m":"...", "@l":"...", "@tr":"...", "@sp":"...", "Application":"VenueAxe", ...}  |
+---------------------------------------------------------------------------------------------------+
```

### Configuration:
1. **`src/backend/VenueAxe.Web/Program.cs`**:
   - Configures `Log.Logger` with `RenderedCompactJsonFormatter` as bootstrap logger.
   - Enriches all events with `FromLogContext()` and `"Application": "VenueAxe"`.
   - Attaches `UseSerilogRequestLogging` for high-precision HTTP telemetry (method, route, status code, elapsed milliseconds).
   - Wraps host execution in `try ... catch ... finally` with `Log.Fatal(ex, ...)` and `Log.CloseAndFlush()`.
2. **`src/backend/VenueAxe.Web/appsettings.json`**:
   - Suppresses framework noise by setting `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, and `System.Net.Http.HttpClient` log levels to `Warning`.

---

## 3. Log Event Schema & Semantic Properties

### Top-Level JSON Fields (CLEF Format):
| Field | Type | Description |
|:------|:-----|:------------|
| `@t` | String (ISO 8601 UTC) | Timestamp of the log event |
| `@m` | String | Fully rendered, human-readable message string |
| `@l` | String | Log level (`Information` omitted by convention, `Warning`, `Error`, `Fatal`) |
| `@x` | String | Formatted exception message, type, and stack trace |
| `@tr` | String | W3C Distributed Trace ID |
| `@sp` | String | W3C Span ID |
| `Application` | String | Constant application identifier (`"VenueAxe"`) |
| `SourceContext` | String | Fully qualified category / class name |

### Common Domain Semantic Properties:
- **Authentication**: `UserId`, `UserEmail`, `Role`, `TenantId`
- **Bookings & Payments**: `BookingReference`, `VenueId`, `PartySize`, `PaymentId`, `AmountCents`, `PaymentStatus`, `BookingStatus`
- **Lanes & Sessions**: `LaneId`, `LaneNumber`, `SessionId`, `PlayerCount`, `GameTypeId`, `TargetZone`, `Points`, `ClutchCalled`, `SafetyReason`
- **Email & Assets**: `Recipient`, `Subject`, `Host`, `Port`, `Security`, `FilePath`, `Bucket`, `ObjectKey`, `FileUrl`

---

## 4. Testing Strategy

### Automated Test Suite:
1. **Unit Tests (`tests/VenueAxe.Tests/Unit/SerilogJsonLoggingTests.cs`)**:
   - `RenderedCompactJsonFormatter_EmitsValidJsonWithStructuredProperties`: Asserts `@t`, `@m`, and custom properties (`SessionId`, `LaneNumber`, `PartySize`) serialize to valid JSON.
   - `RenderedCompactJsonFormatter_FormatsExceptionsInJson`: Validates `@x` exception type and message formatting.
   - `MicrosoftExtensionsLoggingBridge_CapturesStructuredPropertiesInJson`: Proves `ILogger<T>` correctly bridges semantic parameters into JSON keys.
   - `LogContext_EnrichesJsonOutputWithAmbientProperties`: Verifies ambient `LogContext.PushProperty` tags.
   - `MinimumLevelOverrides_FilterMessagesCorrectly`: Ensures framework noise filtering functions properly.

### Execution Commands:
```bash
# Run backend test suite including Serilog tests
dotnet test tests/VenueAxe.Tests/VenueAxe.Tests.csproj --verbosity normal

# Run BDD feature tests
dotnet test tests/VenueAxe.Bdd/VenueAxe.Bdd.csproj --verbosity normal
```

---

## 5. Manual Verification & Observability Guide

1. Start PostgreSQL and VenueAxe Web API:
   ```bash
   dotnet run --project src/backend/VenueAxe.Web/VenueAxe.Web.csproj --urls http://localhost:5280
   ```
2. Inspect stdout stream:
   - Verify every line emitted is a valid single-line JSON object.
   - Example startup output:
     ```json
     {"@t":"2026-09-15T03:53:06.4736858Z","@m":"Starting VenueAxe API host","@i":"32bbf6fa"}
     {"@t":"2026-09-15T03:53:07.1851831Z","@m":"SessionLifecycleBackgroundService started with check interval 15s","@i":"113b96e0","CheckIntervalSeconds":15,"SourceContext":"VenueAxe.Web.BackgroundServices.SessionLifecycleBackgroundService","Application":"VenueAxe"}
     ```
3. Issue an authenticated request or trigger a failed login to view real-time audit logs:
   ```bash
   curl -X POST http://localhost:5280/api/admin/auth/login -H "Content-Type: application/json" -d "{\"email\":\"bad@example.com\",\"password\":\"Wrong123!\"}"
   ```
   Output:
   ```json
   {"@t":"2026-09-15T03:53:23.1421354Z","@m":"Failed login attempt for user \"bad@example.com\"","@i":"9f0c6aa6","@l":"Warning","UserEmail":"bad@example.com","SourceContext":"VenueAxe.Web.Areas.Admin.Controllers.AuthController","Application":"VenueAxe"}
   ```
