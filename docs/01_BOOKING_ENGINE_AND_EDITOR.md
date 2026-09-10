# Module 01: Booking Engine & Custom Page Editor Specification

## 1. Overview
The VenueAxe Booking Engine provides venue owners with a full-featured visual editor to create, customize, and publish online booking flows. Customers can access this booking flow either directly via a hosted venue page or embedded within the venue's existing website (WordPress, Squarespace, Wix, Webflow, custom HTML) via an `<iframe>` snippet.

---

## 2. Venue Owner Booking Page Editor (Admin Portal)

### 2.1 Visual Theme & Customization Engine
The editor provides a live side-by-side preview mode where owners adjust parameters and instantly see the rendered booking widget:
- **Branding & Assets**:
  - Venue Logo (Light & Dark variant support).
  - Background Hero Image / Video banner option with opacity / overlay gradient controls.
  - Accent, Primary, Surface, and Background color pickers with automated WCAG AA contrast validation.
  - Font Pairing (Header & Body font selection from modern web font catalog).
  - Custom Welcome Text, Subheading, and Cancellation Policy callout.
- **Display Modes & Embed Config**:
  - **Hosted Standalone Mode**: `https://[venue-slug].venueaxe.com/book` or custom domain CNAME.
  - **Embedded Mode (iFrame)**: Provides ready-to-copy HTML snippet with auto-resize JS script:
    ```html
    <iframe 
      id="venueaxe-booking" 
      src="https://book.venueaxe.com/embed/v_7f9a12c4" 
      width="100%" 
      frameborder="0" 
      scrolling="no">
    </iframe>
    <script src="https://cdn.venueaxe.com/widget-resizer.js" async></script>
    ```

### 2.2 Schedule, Durations & Capacity Rules
- **Session Durations**: Select supported durations (e.g. 60 min, 90 min, 120 min).
- **Turnaround Buffer**: 5 to 30 minute automated buffer between consecutive bookings for lane reset, safety orientation, and axe sharpening.
- **Thrower Capacity Limits**:
  - Minimum throwers per booking (e.g., 2).
  - Maximum throwers per lane (e.g., 6).
  - Multi-Lane Auto-Allocation: For party sizes exceeding single-lane capacity (e.g., 8 people), the engine automatically checks availability for and locks adjacent lanes (e.g. Lanes 3 & 4).
- **Operating Hours & Blackout Dates**:
  - Weekly recurring time slots (e.g., Mon-Thu 4pm-10pm, Fri 2pm-Midnight, Sat 11am-Midnight, Sun 12pm-9pm).
  - One-time blackout periods / holidays / private buyout locks.

### 2.3 Pricing Matrix & Package Management
- **Pricing Strategies**:
  1. *Per-Person Pricing*: e.g., $35/person for 60 min, $48/person for 90 min.
  2. *Per-Lane Flat Rate*: e.g., $180/lane for up to 6 people for 90 min.
  3. *Tiered Group Rates*: Scaled volume discounts for 12+ throwers.
  4. *Dynamic Peak vs. Off-Peak*: Automatic surcharge during high-demand windows (e.g. Friday 6pm - Saturday 10pm).
- **Packages & Experiences**:
  - Package Name (e.g. "Glow Axe Throwing", "Target League Practice", "Corporate Battle Royale").
  - Description, highlight tags, duration, minimum party size, included amenities.
- **Add-on Services Engine**:
  - Configurable add-on catalog: Drinks packages, snack platters, dedicated Axe Master coach, custom souvenir target boards, merchandise (t-shirts, throwing cards).
  - Pricing type: Per item, Per person, or Per lane.

### 2.5 Admin Walk-In & Reservation Management (`/admin/bookings`)
- **Viewport-Constrained Modal Dialog**:
  - Walk-in / new reservation modal is constrained to `max-height: 90vh; overflow-y: auto`, preventing dialogue clipping and scrolling issues across different monitor resolutions.
- **High-Contrast Validation Banner**:
  - Validation errors are presented with high-contrast red styling (`#ef4444`) and alert icons to ensure instant operator feedback.
- **Default Payment State**:
  - Admin-entered reservations default to `Payment Pending` (`Pending`), allowing staff to quickly reserve slots before processing credit card or cash payments at the venue front desk.
- **Dynamic Bookable Lane Filtering**:
  - When the date, time, or duration is adjusted, the "Assign to Lane" dropdown automatically queries the venue's active lanes (`GET /api/admin/lanes/venue/{venueId}/available-for-slot`) and displays only lanes with zero timeslot collisions.

---

## 3. Customer Booking Experience (Hosted & Embed Flow)

### 3.1 Step-by-Step Guest Checkout Wizard
1. **Experience Selection**: Customer picks desired package/experience with preview cards.
2. **Party Size Selection**: Interactive counter (+/-); displays recommended lane count and group pricing breakdown.
3. **Date & Time Picker**:
   - Interactive calendar showing available days.
   - Time slot grid indicating real-time availability (e.g., `4:00 PM - Available`, `5:30 PM - 2 spots left`, `7:00 PM - Sold Out`).
4. **Add-on Selection**: Upsell screen with item photos, descriptions, and quick-add counters.
5. **Customer Details**:
   - First Name, Last Name, Email, Mobile Phone.
   - Occasion / Special Requests notes box.
6. **Waiver Pre-Flight Notification**:
   - Explains that all throwers in the group must sign a digital waiver before throwing.
   - Provides options to enter attendee names/emails now or share the link later.
7. **Square Checkout**:
   - Embedded Square Web Payments SDK Element (Credit/Debit cards, Apple Pay, Google Pay).
   - Real-time card tokenization with secure iframe PCI-DSS compliance.
   - Deposit vs. Paid in full tracking with balance due calculation for check-in collection.
   - Webhook synchronization (`payment.updated`) for automated order fulfillment and receipt emails.
8. **Confirmation & Receipt Screen**:
   - Booking confirmation code (e.g., `#VA-84920`).
   - "Add to Google Calendar" and ".ics Calendar Event Download" buttons with venue coordinates.
   - Prominent "Share Waiver Link with your Group" card with direct copy button, WhatsApp share link, and dynamic QR code.
   - Balance due indicator for reception counter settlement.

### 3.2 Automated Communications & Notifications
- **Immediate Booking Email**: Contains reservation summary, directions, parking instructions, footwear safety warning (closed-toe shoes mandatory), and the group waiver link.
- **Pre-Arrival Reminder Email (24h and 2h before reservation)**: Automated email notification with directions, footwear checklist, and reminder to have all guests sign waivers in advance.
