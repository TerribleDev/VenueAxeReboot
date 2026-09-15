# Digital Waivers

## 1. Overview & Business Value
The Digital Waiver system replaces paper liability forms with a legally robust digital signing flow. Guests sign waivers on their mobile phones (via QR code or direct link) or at a venue kiosk tablet. Waivers support guardian/minor signing, automated booking association, and an auditable waiver vault for venue staff.

### Target Roles
- **Guest Thrower**: Signs waiver via `/sign/[venueSlug]` or `/sign/w/[bookingRef]`
- **Owner/Manager**: Manages waiver templates and reviews signed waivers via `/admin/waivers`

## 2. Technical Architecture & Data Model

### Key Entities
| Entity | Base Class | Purpose |
|:-------|:-----------|:--------|
| `WaiverTemplate` | `VenueScopedEntity` | Legal text template with version tracking |
| `Waiver` | `VenueScopedEntity` | Signed waiver record |

### Waiver Properties
- `SignerFirstName`, `SignerLastName`, `SignerEmail`, `SignerPhone`
- `DateOfBirth`, `IsGuardianSigning`
- `MinorsCoveredJson` — JSONB array of minor names/ages
- `SignatureImagePngBase64` — rasterized signature image
- `SignatureVectorSvg` — vector signature path
- `SignedAtUtc`, `ExpiresAtUtc` (1 year from signing)
- `IpAddress`, `UserAgent` — audit trail
- `EmailMarketingOptIn` — marketing consent
- `BookingId` — optional link to booking (auto-matched or manual)

### Core Service: `WaiverService`
- `GetTemplateByVenueSlugAsync(slug)` — loads active template for public signing
- `GetTemplateByBookingReferenceAsync(ref)` — loads template for booking-linked waivers
- `SubmitWaiverAsync(request, ipAddress)` — signs waiver with auto-booking matching
- `SearchWaiversAsync(venueId, term, page, size)` — paginated search for admin vault
- `ExportWaiversCsvAsync(venueId)` — CSV export of all venue waivers
- `UpdateTemplateAsync(templateId, request)` — updates template text with SHA-256 hash

### Auto-Booking Association
When submitting a waiver:
1. If `BookingId` provided → direct link
2. If `BookingReference` provided → lookup by reference
3. If email matches a today's booking guest email → auto-link
4. If phone matches a today's booking guest phone → auto-link

## 3. API & Telemetry Specifications

### Public Endpoints (No Auth)
| Method | Route | Purpose |
|:-------|:------|:--------|
| GET | `/api/waivers/{slug}/template` | Get active waiver template |
| GET | `/api/waivers/booking/{ref}/template` | Get template for booking reference |
| POST | `/api/waivers/{slug}/sign` | Submit signed waiver |
| GET | `/api/waivers/booking/{ref}/count` | Count signed waivers for booking |

### Admin Endpoints (Auth Required)
| Method | Route | Purpose |
|:-------|:------|:--------|
| GET | `/api/admin/waivers/venue/{venueId}` | List waiver templates |
| PUT | `/api/admin/waivers/template/{id}` | Update template |
| GET | `/api/admin/waivers/venue/{venueId}/search` | Search signed waivers |
| GET | `/api/admin/waivers/{id}` | Get waiver details |
| GET | `/api/admin/waivers/venue/{venueId}/export-csv` | Export CSV |

## 4. Testing Strategy

### Backend
- **Unit Tests**: `WaiverAndBookingTests.cs`, `WaiverLinkingTests.cs`, `WaiverSearchPaginationTests.cs`, `WaiverPdfAndLaneOperationsTests.cs`
- **BDD**: `DigitalWaiver.feature`
- **Integration**: `WaiverAuditFunctionalTests.cs`

### Frontend
- **Tests**: `waiver.test.ts`

## 5. Manual Verification
1. Open `http://localhost:5173/sign/downtown`
2. Verify legal clauses load from database template
3. Enter signer info, add a minor
4. Draw signature on touch canvas → verify smooth strokes
5. Submit → verify confirmation with legal reference
6. In admin `/admin/waivers` → verify waiver appears with status
