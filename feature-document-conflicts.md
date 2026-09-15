# VenueAxe — Feature Documentation Conflicts

**Audit Date**: 2026-09-14
**Purpose**: Documents discrepancies between what is written in existing documentation and what the code actually does.

---

## Conflict 1: JWT vs Cookie Authentication

| Source | Claim | Reality |
|:-------|:------|:--------|
| `docs/PRODUCT_SPECIFICATION.md` §2.2 | "JWT tokens with short expiry (15 mins) and sliding refresh tokens stored in HttpOnly secure cookies" | Code uses **cookie-only** authentication via `CookieAuthenticationDefaults.AuthenticationScheme`. Zero JWT implementation anywhere in the codebase. Cookie name is `VenueAxe.Auth` with 7-day sliding expiration. |
| `docs/PRODUCT_SPECIFICATION.md` §1.1 | "JWT Claims extraction for authenticated venue staff (`tenant_id`)" | `UserContext.PopulateFromClaims` reads from **cookie-stored claims**, not JWT claims. The claims (`ClaimTypes.NameIdentifier`, `TenantId`, `VenueId`, `ClaimTypes.Role`) are embedded in the encrypted cookie, not a JWT. |

**Verdict**: The documentation describes a JWT-based architecture, but the code correctly implements cookie-based auth per the GEMINI mandate. The `PRODUCT_SPECIFICATION.md` needs updating.

---

## Conflict 2: ASP.NET Core Identity vs Custom Password Hashing

| Source | Claim | Reality |
|:-------|:------|:--------|
| `docs/PRODUCT_SPECIFICATION.md` §2.2 | "ASP.NET Core Identity with PostgreSQL storage" | Code uses a **custom** `PasswordHelper` class with manual PBKDF2 (100,000 iterations, SHA-256, 32-byte output) and base64 salt.hash format. No `Microsoft.AspNetCore.Identity` package is referenced anywhere. No `IdentityDbContext`, no `UserManager<T>`, no `SignInManager<T>`. |

**Verdict**: The custom implementation is functional and uses industry-standard PBKDF2, but the documentation should be updated. If ASP.NET Core Identity was the intended design, the entire auth stack would need to be replaced.

---

## Conflict 3: Clutch vs Kill Terminology

| Source | Claim | Reality |
|:-------|:------|:--------|
| `docs/04_LANE_GAMES_AND_WATL_SCORING.md` | References "2 Anytime Kills" with "Killshot" terminology throughout | Code maintains **both** terminologies: `TargetZone.KillLeft`/`KillRight` AND `TargetZone.ClutchLeft`/`ClutchRight` as enum aliases (same integer values). `ILaneClient` has both `OnClutchCalled` and `OnKillCalled`. `LaneHub.CallKillshot()` fires both events. `GamePlayer` has `ClutchesHit` as an alias for `KillsHit`. |

**Verdict**: The code is transitioning from "Clutch" to "Kill" terminology but hasn't completed the migration. Both terms work. Documentation should acknowledge the dual terminology during transition.

---

## Conflict 4: SuperAdmin Role Referenced But Not Enforced

| Source | Claim | Reality |
|:-------|:------|:--------|
| `docs/PRODUCT_SPECIFICATION.md` §2.1 | Lists "System Admin (Superadmin)" role with "Tenant provisioning, billing oversight, platform health, global game catalog management" | `UserRole.SuperAdmin` exists in the enum, but **no controller or service** checks for it. All admin operations use `[Authorize]` (any authenticated user) without role-based policies. A `LaneMaster` has identical API access to an `Owner`. |

**Verdict**: RBAC is defined in the enum but not enforced. The documentation describes a permission model that doesn't exist in the code yet.

---

## Conflict 5: GEMINI §1 Claims UUIDv7 Uses .NET 10 Native `Guid.CreateVersion7()`

| Source | Claim | Reality |
|:-------|:------|:--------|
| Root `GEMINI.md` | "UUIDv7 Factory (`UuidV7.NewGuid()`) using .NET 10 native `Guid.CreateVersion7()`" | `UuidV7.NewGuid()` needs verification against the actual implementation to confirm it delegates to `Guid.CreateVersion7()`. |

**Verdict**: Needs code verification (file not yet reviewed in detail).

---

## Conflict 6: Terminal / Kiosk Service Account Authentication

| Source | Claim | Reality |
|:-------|:------|:--------|
| `docs/PRODUCT_SPECIFICATION.md` §2.1 | Lists "Terminal / Kiosk Service Account" with "Restricted API key token" | Terminals authenticate via pairing code + device token stored in `Lane.TabletDeviceToken`/`ScreenDeviceToken`, but the token is **never validated** on subsequent requests. `LaneOperationsController` has no `[Authorize]` and doesn't check device tokens. Terminals are effectively unauthenticated. |

**Verdict**: The documentation describes a security boundary that doesn't exist. Terminal authentication is pairing-only with no ongoing token validation.
