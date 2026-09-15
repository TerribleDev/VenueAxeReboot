# Reporting & Analytics

## 1. Overview & Business Value
The reporting system provides venue owners with revenue summaries, booking statistics, waiver counts, utilization metrics, and historical trends. Reports are scoped per-venue with configurable date ranges and timezone-aware calculations.

### Target Roles
- **Owner/Manager**: Access reports via `/admin` dashboard

## 2. Technical Architecture

### Report Types (via `VenueService`)
| Report | Endpoint | Returns |
|:-------|:---------|:--------|
| Revenue Summary | `GET /api/admin/venues/{id}/reports/revenue-summary` | Total revenue, booking counts, avg per booking, peak day |
| Booking Stats | `GET /api/admin/venues/{id}/reports/booking-stats` | Bookings by status, date range, booking type |
| Waiver Stats | `GET /api/admin/venues/{id}/reports/waiver-stats` | Signed count, expired count, marketing opt-in rate |
| Lane Utilization | `GET /api/admin/venues/{id}/reports/lane-utilization` | Per-lane session counts, avg duration, peak hours |

### Query Parameters
All report endpoints accept:
- `startDate` (DateOnly) — report period start
- `endDate` (DateOnly) — report period end
- Timezone conversion via `VenueTimeZoneHelper` for UTC boundary calculation

### DTOs
Report data is returned via dedicated DTOs in `ReportsDtos.cs` (currently bundled in single file — see best practice violations).

## 3. Testing Strategy

### Backend
- **Unit Tests**: `ReportsAndVenueFeaturesTests.cs`

### Frontend
- **Tests**: `reports-and-rules.test.ts`

## 4. Manual Verification
1. Open `http://localhost:5173/admin` → navigate to reports section
2. Select date range → verify revenue figures match booking data
3. Check timezone accuracy for cross-day boundary bookings
