# Manual Test: Digital Waiver Minor Signing with Dynamic Separate Textboxes

**Feature**: Digital Waivers & Legal Compliance  
**Test ID**: `WAIV-001`  
**Target URLs**:  
- `http://localhost:5173/sign/downtown`  
- `http://localhost:5173/admin/waivers`  

---

## 1. Purpose & Objectives
Validate that when an adult parent/guardian signs a digital safety waiver on behalf of minor children, the form dynamically provides separate textboxes via an **"+ Add Another Minor"** button (replacing comma-separated string inputs), allows removing individual minor inputs, serializes minors into a structured JSON array, renders smooth HTML5 signature vector strokes, and auto-updates the Admin Waiver Vault in real time without a manual page refresh.

---

## 2. Prerequisites
1. Backend and Frontend running.
2. Two browser tabs open side-by-side:
   - Window 1: `http://localhost:5173/sign/downtown`
   - Window 2: `http://localhost:5173/admin/waivers` (logged in as venue operator)

---

## 3. Step-by-Step Test Procedure
1. In Window 1 (`/sign/downtown`), locate the signer details section:
   - First Name: `Sarah`
   - Last Name: `Connor`
   - Email: `sarah.connor@sky.net`
   - Phone: `(555) 019-2831`
   - Date of Birth: `1985-05-12`
2. Check the box: **"I am a parent or legal guardian signing for minors"**.
3. Observe the minor entry card:
   - Confirm a single minor textbox appears labeled: `Minor #1 Full Legal Name`.
   - Fill in: `John Connor`.
4. Click **+ Add Another Minor**:
   - Confirm a second textbox appears labeled: `Minor #2 Full Legal Name`.
   - Fill in: `Timmy Connor`.
5. Click **+ Add Another Minor**:
   - Confirm a third textbox appears (`Minor #3 Full Legal Name`).
   - Click the ✕ (Remove) button next to Minor #3.
   - Confirm Minor #3 is removed and the list cleanly re-indexes to 2 minors.
6. Scroll to the digital signature pad:
   - Use mouse/touch to draw a signature on the canvas.
   - Verify smooth vector rendering with responsive stroke weight.
7. Check the legal agreement checkbox.
8. Click **Submit Signed Waiver**.
9. Observe Window 1: Confirm instant confirmation screen with QR code.
10. Observe Window 2 (`/admin/waivers`): Confirm the new waiver record appears automatically via SignalR WebSocket broadcast without refreshing.

---

## 4. What to Look For
- Visual check on `/sign/downtown`: No comma-separated instructions; clean, individual form controls with clear remove buttons.
- Validation: Empty minor boxes or invalid formats prevent submission with inline warning banners.
- Real-Time sync on `/admin/waivers`: The new row displays `Sarah Connor`, Guardian status `Parent/Guardian`, Minors `John Connor, Timmy Connor`, and verified status pill.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Clicking "+ Add Another Minor" appends a discrete textbox with independent binding.
- [ ] Individual minor textboxes can be removed without affecting other entries.
- [ ] Submitted payload formats minors into clean JSON string array `["John Connor", "Timmy Connor"]`.
- [ ] HTML5 canvas captures vector stroke coordinates and serializes to base64 PNG data URL.
- [ ] Admin Waiver Vault updates via `OnLaneStateChanged` in $< 100\text{ms}$ without page refresh.
- [ ] Zero console errors on both signer kiosk and admin vault.
