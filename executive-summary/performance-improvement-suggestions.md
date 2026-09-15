# VenueAxe — Performance Improvement Suggestions (Executive Summary)

**Audit Date**: 2026-09-14
**Auditor**: Enterprise Architecture Review

---

## High Impact

### PERF-001: Game State Full Replay on Every Throw

| Field | Value |
|:------|:------|
| **Impact** | 🔴 HIGH |
| **Files** | `LaneGameService.cs:99-114`, `LaneService.cs:99-110` |

**Problem**: Every time a throw is recorded via `RecordThrowAsync`, the service:
1. Loads ALL existing `MatchThrow` entities from the database
2. Initializes a fresh `GameStateSnapshot`
3. Replays every previous throw sequentially through the engine
4. Then records the new throw

For a 10-round, 2-player WATL match, this replays up to 19 throws before recording throw #20. The same full replay occurs in `GetLanesForVenueAsync` for **every active lane** on the admin dashboard.

**Recommendation**:
- Cache the current `GameStateSnapshot` as a JSONB column on `GameMatch` (or in Redis)
- Update the cached snapshot on each throw instead of replaying from scratch
- Estimated improvement: O(1) per throw instead of O(n) where n = total throws

---

### PERF-002: Lane Grid Dashboard N+1 Game State Rebuilds

| Field | Value |
|:------|:------|
| **Impact** | 🔴 HIGH |
| **File** | `LaneService.cs:92-154` |

**Problem**: `GetLanesForVenueAsync` iterates over all lanes and for each lane with an active session, replays the entire game state. A venue with 8 active lanes × 15 throws each = 120 engine replays on a single page load.

**Recommendation**: 
- Pre-compute and cache game snapshots (see PERF-001)
- Use a single batch query to load all sessions with their throws eagerly
- Consider a dedicated read-model/projection for the admin dashboard

---

### PERF-003: Booking Availability Per-Slot Database Queries

| Field | Value |
|:------|:------|
| **Impact** | 🟠 MEDIUM-HIGH |
| **File** | `BookingService.cs:99-133` |

**Problem**: `CheckAvailabilityAsync` calls `GetOverlappingBookingsWithLanesAsync` inside a `for` loop for every hourly slot in the operating window. A venue open 12 hours with 1-hour slots = 12 separate database queries for what could be a single range query.

**Recommendation**: 
- Execute a single query for all bookings in the entire day range
- Filter overlaps in-memory per slot
- Estimated improvement: 12x reduction in database round-trips

---

## Medium Impact

### PERF-004: Non-Cryptographic `new Random()` Created Per Call

| Field | Value |
|:------|:------|
| **Impact** | 🟡 MEDIUM |
| **File** | `LaneService.cs:215,310` |

**Problem**: `new Random()` is created on each method call in `CreateLaneAsync` and `RegeneratePairingCodesAsync`. In .NET 6+, `Random.Shared` provides a thread-safe singleton that avoids repeated allocations. The pairing codes should arguably use `RandomNumberGenerator` for security (see security issues).

**Recommendation**: Replace `new Random()` with `Random.Shared` for non-security uses, or `RandomNumberGenerator.GetInt32(100000, 1000000)` for pairing codes.

---

### PERF-005: No Explicit Connection Pooling Configuration

| Field | Value |
|:------|:------|
| **Impact** | 🟡 MEDIUM |
| **File** | `Program.cs:26-30` |

**Problem**: Npgsql default connection pool settings (100 connections, no health checks) are used. For enterprise-scale load with background services, SignalR connections, and concurrent API requests, explicit tuning improves reliability.

**Recommendation**: Configure Npgsql pool size, connection lifetime, and health checks via connection string parameters:
```
;Maximum Pool Size=200;Minimum Pool Size=10;Connection Idle Lifetime=300;Keepalive=60
```

---

### PERF-006: Optional Redis Backplane for SignalR

| Field | Value |
|:------|:------|
| **Impact** | 🟡 MEDIUM |
| **File** | `Program.cs:109-117` |

**Problem**: Redis backplane for SignalR is configured only when a Redis connection string is present. In single-instance development this is fine, but for production multi-instance deployments, SignalR messages will not propagate across instances without the backplane.

**Recommendation**: Make Redis backplane mandatory in production via environment-based configuration validation. Log a warning at startup if running in production without Redis.

---

### PERF-007: No Response Caching for Static Game Engine Data

| Field | Value |
|:------|:------|
| **Impact** | 🟢 LOW |
| **File** | `LaneOperationsController.cs:27-40` |

**Problem**: `GET /api/lanes/operations/games` constructs the engine list from `GameEngineRegistry.GetAllEngines()` on every request. While fast (static in-memory), adding HTTP response caching eliminates unnecessary serialization.

**Recommendation**: Add `[ResponseCache(Duration = 3600)]` or serve from a cached JSON string.

---

### PERF-008: Background Service Polling Interval

| Field | Value |
|:------|:------|
| **Impact** | 🟢 LOW |
| **File** | `SessionLifecycleBackgroundService.cs:20` |

**Problem**: The background service polls every 15 seconds for expired sessions. This creates 5,760 database queries per day regardless of whether any sessions are active.

**Recommendation**: Consider:
- Increasing the interval to 30-60 seconds (sessions already have a 3-minute grace period)
- Using `IMemoryCache` with expiration callbacks to trigger checks only when sessions are actually near expiry
- Adding a fast-path check: skip the query if no sessions are known to be active
