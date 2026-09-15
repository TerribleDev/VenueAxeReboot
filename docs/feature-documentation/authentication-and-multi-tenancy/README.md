# Authentication & Multi-Tenancy

## 1. Overview & Business Value
VenueAxe uses an **Owner-Only Authentication Model** — no consumer/thrower accounts exist. End-throwers interact as guests via public booking widgets, digital waiver kiosks, and tablet scorekeeping. Logins are strictly reserved for Venue Owners, General Managers, and Lane Masters.

Multi-tenancy provides logical data isolation via `TenantId` partitioning with EF Core Global Query Filters, enabling single-location operators up to enterprise franchises.

### Target Roles
- **Owner**: Full platform access (billing, staff, config, analytics)
- **Manager**: Venue-level operations (bookings, waivers, lanes, reports)
- **LaneMaster**: Lane floor operations (sessions, scoring, check-ins)
- **Guest Thrower**: No account — interacts via public booking/waiver/tablet flows

## 2. Technical Architecture & Data Model

### Authentication Flow
1. Staff submits `POST /api/admin/auth/login` with email + password
2. `AuthService.AuthenticateAsync()` looks up user by email
3. Password verified against stored PBKDF2 hash (100K iterations, SHA-256, 32-byte output via `PasswordHelper`)
4. On success, an encrypted cookie (`VenueAxe.Auth`) is issued containing claims: `UserId`, `TenantId`, `VenueId`, `Role`, `Email`
5. Cookie properties: `HttpOnly=true`, `SameSite=Lax`, `Secure=SameAsRequest`, 7-day sliding expiration

### Key Entities
| Entity | Base Class | Purpose |
|:-------|:-----------|:--------|
| `User` | `TenantEntity` | Staff accounts (Owner/Manager/LaneMaster) |
| `Tenant` | `BaseEntity` | Top-level organization |
| `Venue` | `TenantEntity` | Physical venue location |

### Multi-Tenancy Enforcement
- `TenantEntity` adds `TenantId` to all domain objects
- `VenueScopedEntity` extends `TenantEntity` with `VenueId`
- `VenueAxeDbContext` applies `.HasQueryFilter(e => e.TenantId == _currentTenantId)` globally
- `TenantRepository<T>` automatically injects `TenantId` on inserts
- `IUserContext` (populated via `HttpContextUserContext`) provides ambient scope

### Service Layer
- `IAuthService` → `AuthService` in `VenueAxe.Application`
- `IUserContext` → `UserContext` (base) in `VenueAxe` domain
- `HttpContextUserContext` in `VenueAxe.Web.Infrastructure`

## 3. API & Telemetry Specifications

### Endpoints
| Method | Route | Auth | Purpose |
|:-------|:------|:-----|:--------|
| POST | `/api/admin/auth/login` | None | Authenticate staff |
| POST | `/api/admin/auth/logout` | Auth | Sign out |
| GET | `/api/admin/auth/me` | Auth | Current user profile |
| POST | `/api/admin/auth/change-password` | Auth | Update password |

### Security Boundaries
- Admin controllers: `[Authorize]` (any authenticated user)
- Lane operations: No authorization (tablet/TV terminals)
- Public booking/waiver: No authorization (guest flows)

> **Known Gap**: No RBAC enforcement — `[Authorize]` checks authentication only, not role-based policies. A `LaneMaster` has identical API access to an `Owner`. See `feature-document-conflicts.md` Conflict #4.

## 4. Testing Strategy

### Backend
- **Unit Tests**: `tests/VenueAxe.Tests/Unit/SecurityAndIdentityTests.cs`
- **BDD**: `tests/VenueAxe.Bdd/Features/MultiTenancy.feature`
- **Integration**: `tests/VenueAxe.Tests/Functional/MultiTenantIsolationFunctionalTests.cs`

### Frontend
- **Admin login flow**: `src/frontend/src/tests/admin-operations.test.ts`

## 5. Manual Verification
1. Open `http://localhost:5173/admin` → Redirects to login
2. Login with `owner@venueaxe.com` / `VenueAxeAdmin2026!#$`
3. Verify dashboard loads with venue data
4. Open DevTools → Application → Cookies → Verify `VenueAxe.Auth` cookie is `HttpOnly`
5. Open an incognito tab → Verify admin routes return 401
