# Manual Test: Venue Icon Upload, Dimension Validation & Multi-Surface Branding

**Feature**: Venue Management, Asset Storage & Multi-Surface Branding  
**Test ID**: `VEN-001`  
**Target URLs**:  
- `http://localhost:5173/admin/settings`  
- `http://localhost:5173/tablet`  
- `http://localhost:5173/screen`  
- `http://localhost:5173/book/downtown`  
- `http://localhost:5173/sign/downtown`  

---

## 1. Purpose & Objectives
Validate the venue icon upload system:
1. Enforces **512x512 px square (1:1 aspect ratio)** dimensions with client-side and server-side validation, rejecting non-square or out-of-bounds files.
2. Supports PNG, WebP, SVG, and JPEG up to 2MB.
3. Saves to local filesystem storage in development (`/uploads/venue-icons/`) and Backblaze B2 S3 storage in production.
4. Renders the uploaded venue icon across all venue surfaces:
   - Venue Settings preview & Top Navigation selector
   - Lane Tablet console HUD and idle attract screen
   - Overhead TV display broadcast header and idle attract loop
   - Customer booking wizard header
   - Digital waiver kiosk header
   - Transactional HTML email headers
5. Verifies graceful fallback to standard branding (axe emoji/badge) when an icon is removed or not uploaded.

---

## 2. Prerequisites
1. Backend running with `Storage:Provider = "LocalStorage"`.
2. Operator logged in at `http://localhost:5173/admin/settings`.
3. Test image assets:
   - `test-icon-square.png` (512x512 px square PNG, < 2MB)
   - `test-icon-wide.jpg` (800x400 px rectangular image)
   - `test-icon-large.png` (2048x2048 px image)

---

## 3. Step-by-Step Test Procedure

### Part A: Dimension & Aspect Ratio Validation
1. Navigate to `http://localhost:5173/admin/settings`.
2. Locate the **🎨 Venue Icon & Multi-Surface Branding** card.
3. Attempt to select `test-icon-wide.jpg` (800x400 px).
   - Observe error banner: `Image must be a square (1:1 aspect ratio). Detected 800x400px. Please crop or resize to 512x512 px.`
   - Confirm file input resets and Upload button remains disabled.
4. Attempt to select `test-icon-large.png` (2048x2048 px).
   - Observe error banner: `Image is too large (2048x2048px). Maximum size is 1024x1024 px.`
5. Select `test-icon-square.png` (512x512 px).
   - Confirm preview thumbnail appears in the 80x80 preview box.
   - Confirm error banners clear and **⬆️ Upload Icon** activates.

### Part B: Upload & Multi-Surface Rendering
6. Click **⬆️ Upload Icon**:
   - Verify success toast: `Venue icon uploaded and updated across all displays successfully!`.
   - Verify the image persists and displays in the preview box.
7. Inspect the top navigation bar:
   - Verify the 26x26 icon appears next to the venue name dropdown.
8. Open `http://localhost:5173/tablet`:
   - Verify the venue icon appears in the idle attract screen (or HUD bar).
9. Open `http://localhost:5173/screen`:
   - Verify the venue icon appears in the broadcast header brand zone and the large attract loop.
10. Open `http://localhost:5173/book/downtown`:
    - Verify the venue icon displays above the venue title in the booking wizard header.
11. Open `http://localhost:5173/sign/downtown`:
    - Verify the venue icon displays in the waiver header card.

### Part C: Removal & Fallback
12. Return to `http://localhost:5173/admin/settings`.
13. Click **🗑️ Remove**:
    - Confirm the browser prompt.
    - Confirm the preview reverts to the fallback `🪓` icon.
14. Refresh `/tablet`, `/screen`, `/book/downtown`, and `/sign/downtown`:
    - Verify all surfaces gracefully fall back to the default axe icon with zero visual breakage.

---

## 4. What to Look For
- Dimension rejection occurs immediately on client selection before wasting network bandwidth.
- Backend API (`POST /api/admin/venues/{id}/icon`) re-validates stream headers for security.
- Zero CORS or 404 broken image icons across all surfaces.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Non-square images (aspect ratio $\ne 1:1 \pm 5\%$) are rejected with informative error messages.
- [ ] Files $> 2\text{MB}$ or with invalid extensions are rejected.
- [ ] Uploaded 512x512 icons render crisply with proper aspect ratios (`object-fit: contain`).
- [ ] Storage service writes to `/uploads/venue-icons/` and returns accessible static file URL.
- [ ] Removing the icon sets `IconUrl = null` in the database and restores fallback branding.
- [ ] Zero console errors on all tested surfaces.
