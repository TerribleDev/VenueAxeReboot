# Venue Management

## 1. Overview & Business Value
Venue Management encompasses the configuration of physical venue properties, branding, operating hours, business hours schedules, staff management, and Square payment integration. Each venue operates under a tenant and is the primary organizational unit for lanes, bookings, and waivers.

### Target Roles
- **Owner**: Full venue configuration access
- **Manager**: Venue settings and staff management

## 2. Technical Architecture & Data Model

### Key Entity: `Venue` (extends `TenantEntity`)
| Property | Type | Purpose |
|:---------|:-----|:--------|
| `Name` | string | Venue display name |
| `Slug` | string | URL-safe identifier for public routes |
| `AddressLine1/2`, `City`, `State`, `PostalCode`, `Country` | string | Physical address |
| `Timezone` | string | IANA timezone ID (e.g., `America/New_York`) |
| `Currency` | string | ISO currency code (e.g., `USD`) |
| `BusinessHoursJson` | JSONB | Weekly operating hours + closed dates |
| `BrandingConfigJson` | JSONB | Theme colors, logos, Square config |
| `IconUrl` | string | Venue icon/logo URL |
| `Email`, `Phone`, `Website` | string | Contact information |

### Timezone Handling: `VenueTimeZoneHelper`
All time calculations respect the venue's configured timezone:
- `GetTimeZone(timezoneId)` — resolves IANA timezone with fallback to `America/New_York`
- `GetUtcDayRange(date, tz)` — converts venue-local date to UTC start/end bounds
- `ToVenueDateTimeOffset(date, hour, minute, tz)` — constructs UTC-aware timestamps
- `ConvertToVenueTime(dto, tz)` — converts UTC to venue-local time

### Core Services
- `IVenueService` → `VenueService` — CRUD for venues, branding, icons, business hours, Square config
- Square payment configuration is stored in `BrandingConfigJson` under `squareConfig`

## 3. API Specifications

### Admin Endpoints
| Method | Route | Purpose |
|:-------|:------|:--------|
| GET | `/api/admin/venues` | List venues for tenant |
| GET | `/api/admin/venues/{id}` | Get venue details |
| PUT | `/api/admin/venues/{id}` | Update venue |
| PUT | `/api/admin/venues/{id}/branding` | Update branding config |
| PUT | `/api/admin/venues/{id}/business-hours` | Update operating hours |
| PUT | `/api/admin/venues/{id}/icon` | Upload venue icon |
| GET | `/api/admin/venues/{id}/square-config` | Get Square payment config |
| PUT | `/api/admin/venues/{id}/square-config` | Update Square config |
| GET | `/api/admin/venues/{id}/reports/*` | Revenue, booking, waiver reports |

## 4. Testing Strategy

### Backend
- **Unit Tests**: `VenueIconAndMarketingTests.cs`, `VenueSquareConfigTests.cs`, `VenueTimeZoneAndScheduleMatrixTests.cs`, `ReportsAndVenueFeaturesTests.cs`, `OperatingHoursAndScheduleTests.cs`
- **BDD**: `VenueBrandingAndMarketing.feature`

### Frontend
- **Tests**: `square-config.test.ts`, `timezone-dateTime.test.ts`, `reports-and-rules.test.ts`

## 5. Manual Verification
1. Open `http://localhost:5173/admin/settings`
2. Verify venue name, address, timezone display correctly
3. Update operating hours → verify slots change in booking wizard
4. Upload a venue icon → verify it appears in header and booking page
