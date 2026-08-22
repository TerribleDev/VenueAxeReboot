# Module 02: Digital Waiver Management System Specification

## 1. Overview
The Digital Waiver System protects venue owners from liability by collecting, verifying, and indexing legally binding digital signatures from every thrower before they enter the throwing lanes. The system operates with zero required customer accounts—throwers sign via mobile web, kiosk tablets, or via links sent to booking groups.

---

## 2. Legal Architecture & Waiver Builder

### 2.1 Customizable Waiver Templates
Venue owners and corporate managers can draft and update waiver agreements:
- **Rich Text / Legal Markdown Editor**: Standard formatting (bold warnings, bulleted safety protocols, acknowledgment checkboxes).
- **Dynamic Variable Placeholders**:
  - `{{VenueName}}`, `{{VenueAddress}}`, `{{SignerFullName}}`, `{{SignerDOB}}`, `{{SignerEmail}}`, `{{SignerPhone}}`, `{{CurrentDate}}`, `{{MinorNamesList}}`.
- **Mandatory Safety Clauses (Axe Throwing Specific)**:
  - Footwear acknowledgment (closed-toe shoes mandatory).
  - Substance & alcohol policy acknowledgment (venue reserves right to deny throwing to intoxicated guests).
  - Axe retrieval safety rules (one axe thrown at a time, no retrieving while partner throws).
  - Assumption of risk and covenant not to sue.
- **Semantic Versioning & Immutable Hash**:
  - Every update creates an incremental version (e.g., `v1.0`, `v1.1`, `v2.0`).
  - System generates a SHA-256 hash of the exact legal text displayed at the moment of signing.

### 2.2 Minor & Guardian Support
- **Age Verification Rule**: Configurable minimum age (e.g., 18 or 21) for independent signing.
- **Guardian Signing Workflow**:
  - If a participant is a minor (e.g. ages 8-17), a legal parent/guardian must complete the waiver.
  - The guardian enters their legal details, checks the parental indemnification clause, and lists one or more minors with their full names and dates of birth.

---

## 3. Signing Workflows & Channels

```
                           +------------------------+
                           |    Waiver Required     |
                           +-----------+------------+
                                       |
        +------------------------------+------------------------------+
        |                              |                              |
        v                              v                              v
+------------------+          +------------------+          +------------------+
| Pre-Arrival Link |          | Entrance QR Code |          | In-Venue Kiosk   |
| (From Booking)   |          | (Mobile Sign)    |          | (Station Tablet) |
+--------+---------+          +--------+---------+          +--------+---------+
         |                             |                             |
         +-----------------------------+-----------------------------+
                                       |
                                       v
                     +----------------------------------+
                     |    Interactive Signature Pad     |
                     |  - Canvas Draw / Type Signature  |
                     |  - Legal Checkboxes              |
                     |  - DOB & Identity Validation     |
                     +-----------------+----------------+
                                       |
                                       v
                     +----------------------------------+
                     |    Waiver Vault & Verification   |
                     |  - SHA-256 Audit Log Generated   |
                     |  - Linked to Booking & Lane      |
                     |  - Staff Check-In Green Light    |
                     +----------------------------------+
```

### 3.1 Signing Channels
1. **Pre-Arrival Booking Link**: Included in booking confirmations and calendar invites. Group organizers share a dedicated link: `https://sign.venueaxe.com/w/[booking-token]`.
2. **Venue Entrance QR Code**: Static posters with dynamic venue QR code (`https://sign.venueaxe.com/v/[venue-id]`) allowing walk-ins or arriving group members to sign on their smartphones in 60 seconds.
3. **Reception Tablet Kiosk Mode**: Full-screen kiosk view on front desk iPad/Android tablets that auto-clears and resets after each submission.

### 3.2 Signature Capture & Audit Metadata
- **Touch / Mouse / Stylus Canvas**: Smooth HTML5 vector drawing canvas with clear, redraw, and smoothing algorithms.
- **Audit Evidence Captured at Time of Submission**:
  - Signer Full Legal Name, Email, Phone Number, Date of Birth, Full Postal Address.
  - Minor Names & Minor DOBs (if applicable).
  - Exact UTC timestamp (`SignedAtUtc`).
  - Client IP Address and Geo-IP approximation.
  - User Agent string (Device, Browser version, OS).
  - Snapshot of the rendered agreement HTML & SHA-256 text hash.
  - Base64 PNG + SVG representation of the drawn signature.

---

## 4. Staff Verification & Lane Check-In Operations

### 4.1 Real-Time Staff Waiver Search & Verification
- Staff portal search bar with instant autocomplete:
  - Search by Signer Name, Phone Number, Email, or scanned QR pass.
- Status Badges:
  - 🟢 **Active / Valid** (Signed within valid window, e.g. last 365 days).
  - 🟡 **Needs Minor Sign-off** (Minor listed without verified parent signature).
  - 🔴 **Expired** (Signed > 365 days ago, needs re-sign).
  - ⚪ **Not Found** (Requires signing before lane entry).

### 4.2 Auto-Linking to Bookings & Lane Rosters
- When a guest signs using a Booking Token, their signed status immediately updates on the Staff Lane Manager screen.
- When launching a lane session, the Lane Master can view the green checkmark indicator (`6 of 6 waivers signed`) before giving the safety briefing and unlocking the lane tablet.
- Printable / Exportable PDF feature for insurance audits or incident reporting.
