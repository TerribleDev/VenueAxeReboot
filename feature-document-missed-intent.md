# VenueAxe — Missed Intent Analysis

**Audit Date**: 2026-09-14
**Purpose**: Documents cases where the code functions without errors but likely doesn't match the original design intent. These are NOT bugs (the code runs correctly), but the behavior may surprise users or diverge from what was probably intended.

---

## INTENT-001: CountdownGameEngine Bust Rule Doesn't Match Documentation

| Field | Value |
|:------|:------|
| **File** | `src/backend/VenueAxe/GameEngine/CountdownGameEngine.cs:93-102` |
| **Documented Rule** | `SpecialRules`: "If a throw exceeds your remaining balance (would take score below zero) **or leaves you with 1 point**, you BUST!" |
| **Actual Code** | `if (player.Score - points < 0)` — only checks if score would go below zero |

**Analysis**: The `SpecialRules` text explicitly says leaving 1 point is a bust (since you can't finish from 1 with any valid throw ≥ 1). However, the code only busts when the score would go negative, not when it would reach exactly 1.

**Probable Intent**: A standard darts-style "double out" rule where leaving 1 remaining is impossible to finish. The documented rule is stricter than the code.

**Impact**: Players can get stuck at Score = 1 indefinitely since no throw can produce exactly 1 point to reach 0 (Ring 1 = 1 point would bring them to 0 — wait, actually Ring 1 = 1 point WOULD finish. So this may be intentional: leaving at 1 means you can only win with Ring 1. The documentation may be wrong, not the code.)

**Verdict**: The documentation's bust-at-1 rule is questionable since Ring 1 (1 pt) would finish from 1. However, if the intent is a "double out" rule where you can only finish on a specific value, the code needs to be updated to match. **Recommend clarifying the game design intent.**

---

## INTENT-002: KillHunterEngine Killshots Require `isClutchCalled` Despite "Permanently Active" Rule

| Field | Value |
|:------|:------|
| **File** | `src/backend/VenueAxe/GameEngine/KillHunterEngine.cs:48-97` |
| **Documented Rule** | `SpecialRules`: "Both Killshot targets are permanently active for 8 points **without needing to be called**" |
| **Actual Code** | For XY-coordinate throws, killshots pass `isClutchCalled` to `WatlTargetMath.Evaluate()`. For manual zone throws, killshots score `isClutchCalled ? 8 : 0` |

**Analysis**: The engine description says killshots are "permanently active without needing to be called", implying any hit on a killshot target should score 8 points regardless of whether the thrower calls it. However:

1. **XY path**: Calls `WatlTargetMath.Evaluate(x, y, isClutchCalled)`, which only awards killshot points if `isClutchCalled` is true
2. **Manual zone path**: Explicitly checks `isClutchCalled ? 8 : 0`

**Probable Intent**: In Kill Hunter mode, killshots should always be active. The engine should force `isClutchCalled = true` internally or bypass the WatlTargetMath clutch check.

**Impact**: Players must still click "Call Killshot" in Kill Hunter mode even though the game rules say they don't need to. This contradicts the game design and creates a confusing UX.

**Verdict**: Code behavior doesn't match documented intent. The XY path should use `WatlTargetMath.Evaluate(x, y, isClutchCalled: true)` and manual zone should always award 8 for killshot zones, regardless of `isClutchCalled`.

---

## INTENT-003: Insecure `new Random()` for Pairing Codes

| Field | Value |
|:------|:------|
| **File** | `src/backend/VenueAxe.Application/Services/LaneService.cs:215,310-312` |
| **Context** | Tablet and Screen pairing codes generated as 6-digit random numbers |

**Analysis**: Pairing codes are generated with `new Random().Next(100000, 1000000)`, which uses a time-seeded PRNG. If two lanes are created in rapid succession (same millisecond), they could receive identical pairing codes. More critically, `Random` is predictable — if an attacker knows the creation timestamp, they can predict the pairing code.

**Probable Intent**: Pairing codes should be unpredictable to prevent unauthorized device pairing.

**Impact**: Low immediate risk (pairing requires physical access to the venue's admin panel), but violates defense-in-depth principles.

**Verdict**: Replace with `RandomNumberGenerator.GetInt32(100000, 1000000)` for cryptographic security, or at minimum use `Random.Shared` to avoid duplicate seeds.

---

## INTENT-004: `GameMatch` Lacks Tenant/Venue Scoping

| Field | Value |
|:------|:------|
| **File** | `src/backend/VenueAxe/Domain/Entities/GameMatch.cs` |
| **Context** | `GameMatch` extends `BaseEntity` (only Id, CreatedAt, UpdatedAt), not `VenueScopedEntity` |

**Analysis**: `GameMatch` has no `TenantId` or `VenueId`. It connects to `LaneSession` via `SessionId`, and sessions connect to lanes which have tenant/venue scoping. EF Core global query filters therefore do NOT apply to `GameMatch` queries directly.

**Probable Intent**: This is likely intentional — matches are always accessed through sessions, so the tenant filter cascade works through navigation properties. Direct `GameMatch` queries (if they existed) would bypass tenant isolation, but no such queries exist in the current codebase.

**Impact**: None currently. But if future code adds direct `GameMatch` queries without going through sessions, tenant isolation would be violated.

**Verdict**: Acceptable design decision with the current query patterns. Document as a known constraint: "GameMatch must always be queried through LaneSession to maintain tenant isolation."
