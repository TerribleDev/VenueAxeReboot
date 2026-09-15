# Booking Engine

## 1. Overview & Business Value
The Booking Engine enables guests to discover available time slots, select party sizes and packages, calculate pricing with discounts, and create reservations through a public-facing booking wizard. Venue owners configure pricing, operating hours, packages, add-ons, and custom booking types through the admin portal.

### Target Roles
- **Guest Thrower**: Books sessions via `/book/[venueSlug]`
- **Owner/Manager**: Configures pricing, creates admin bookings, manages reservations via `/admin/bookings`

## 2. Technical Architecture & Data Model

### Key Entities
| Entity | Base Class | Purpose |
|:-------|:-----------|:--------|
| `Booking` | `VenueScopedEntity` | Core reservation record |
| `BookingLane` | `BaseEntity` | Many-to-many: Booking ↔ Lane allocation |
| `BookingConfig` | `VenueScopedEntity` | Venue booking rules (pricing, capacity, packages) |

### Core Service: `BookingService` (1,120 lines)
Handles the complete booking lifecycle:
- `GetPublicBookingPageAsync(slug)` — loads venue config for the booking wizard
- `CheckAvailabilityAsync(slug, query)` — returns time slots with availability and pricing
- `CalculatePricingAsync(slug, request)` — real-time pricing breakdown (base, peak, packages, add-ons, discounts)
- `CreateGuestBookingAsync(slug, request)` — public booking creation with lane allocation
- `CreateAdminBookingAsync(venueId, request)` — admin walk-in/phone reservations
- `CancelBookingAsync(bookingId)` — cancellation with email notifications

### Lane Allocation: `LaneAllocationEngine`
Enforces **contiguous adjacent lane allocation**:
- Filters active, unoccupied lanes
- Groups by consecutive `LaneNumber` sequences
- Allocates `ceil(partySize / maxThrowersPerLane)` contiguous lanes
- Rejects if no contiguous block is available

### Pricing Engine
Supports multiple pricing models:
1. **Per-Person** (`BasePriceCents × partySize`)
2. **Per-Lane** (flat rate per allocated lane)
3. **Peak surcharge** (automatic during configured peak hours)
4. **Package overrides** (fixed-price packages)
5. **Add-ons** (per-person or flat extras)
6. **Discount rules** (promo codes, group discounts, early-bird)
7. **Person types** (adult/child/senior with rate multipliers)

## 3. API & Telemetry Specifications

### Public Endpoints (No Auth)
| Method | Route | Purpose |
|:-------|:------|:--------|
| GET | `/api/public/booking/{slug}` | Load booking page config |
| POST | `/api/public/booking/{slug}/availability` | Check time slot availability |
| POST | `/api/public/booking/{slug}/calculate-price` | Real-time pricing |
| POST | `/api/public/booking/{slug}/reserve` | Create guest booking |
| GET | `/api/public/booking/{slug}/booking/{ref}` | Booking confirmation details |

### Admin Endpoints (Auth Required)
| Method | Route | Purpose |
|:-------|:------|:--------|
| GET | `/api/admin/bookings/venue/{venueId}` | List all bookings |
| POST | `/api/admin/bookings/venue/{venueId}` | Create admin booking |
| GET | `/api/admin/bookings/{id}` | Get booking details |
| PUT | `/api/admin/bookings/{id}/cancel` | Cancel booking |
| POST | `/api/admin/bookings/{id}/payment` | Record payment |

## 4. Testing Strategy

### Backend
- **Unit Tests**: `AdminBookingCreationTests.cs`, `BookingPricingAndDiscountTests.cs`, `LaneAllocationEngineTests.cs`, `OperatingHoursAndScheduleTests.cs`
- **BDD**: `BookingLifecycle.feature`, `BookingPricing.feature`
- **Integration**: `BookingAllocationFunctionalTests.cs`

### Frontend
- **Tests**: `booking-formats.test.ts`, `lane-schedule-matrix.test.ts`

## 5. Manual Verification
1. Open `http://localhost:5173/book/downtown`
2. Select party size (e.g., 6 throwers)
3. Select a date → verify time slots load with availability indicators
4. Select a time slot → verify pricing updates dynamically
5. Fill guest details → click "Complete Reservation"
6. Verify booking reference generated (e.g., `VA-84920`)
7. In admin portal `/admin/bookings` → verify booking appears
