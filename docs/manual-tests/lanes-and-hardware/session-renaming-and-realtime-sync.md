# Manual Test: Live Session Renaming & Real-Time Dual-Screen Sync

**Feature**: Lane Management & Real-Time Telemetry  
**Test ID**: `LANE-002`  
**Target URLs**:  
- `http://localhost:5173/admin/lanes`  
- `http://localhost:5173/tablet`  
- `http://localhost:5173/screen`  

---

## 1. Purpose & Objectives
Validate that an active session's title can be modified after launch directly from the Lane Editor modal on `/admin/lanes`, and that the updated session title propagates instantaneously ($< 50\text{ms}$) via SignalR WebSockets to both the in-lane tablet console and the overhead TV broadcast screen without requiring a page reload.

---

## 2. Prerequisites
1. Port 5280 (backend) and port 5173 (frontend) active.
2. Lane 01 paired on `/tablet` (PIN `100001`) and `/screen` (PIN `200001`).
3. Side-by-side Chrome windows open for `/admin/lanes`, `/tablet`, and `/screen`.

---

## 3. Step-by-Step Test Procedure
1. In the `/admin/lanes` window, locate **Lane 01**.
2. If no active session exists:
   - Click **+ Start Session**.
   - Set Match Title: `Friday Night Throwdown`.
   - Players: `Sarah Connor, Marcus Wright`.
   - Game: `WATL Standard Match`.
   - Click **Launch Match**.
3. Inspect `/tablet`:
   - Verify the top HUD bar displays `🎯 Friday Night Throwdown` in an amber badge.
4. Inspect `/screen`:
   - Verify the broadcast header displays `🎯 Friday Night Throwdown`.
5. Return to the `/admin/lanes` window:
   - On the Lane 01 card, click the **Edit Lane** button (pencil icon).
   - In the modal dialog, locate the **Active Session Title** field.
   - Change the title to: `🏆 Championship Grand Finals 2026`.
   - Click **Save Changes**.
6. Observe the `/tablet` and `/screen` windows *without clicking refresh*.

---

## 4. What to Look For
- Visual check on `/admin/lanes`: Modal saves cleanly and lane card updates immediately with the new title.
- Visual check on `/tablet`: The amber session badge in the HUD changes in real time to `🎯 🏆 Championship Grand Finals 2026`.
- Visual check on `/screen`: The overhead display title updates synchronously to `🎯 🏆 Championship Grand Finals 2026`.
- Inspect DevTools Network tab: Verify SignalR WebSocket frames received on `/hubs/lane`.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Active session title input is available and editable in the Lane Editor modal when a session is in progress.
- [ ] Saving the modal dispatches `PUT /api/admin/lanes/{id}` with `sessionTitle`.
- [ ] Backend updates the `lane_sessions` record and broadcasts `OnLaneStateChanged` to groups `"admin"` and `"lane_{id}"`.
- [ ] Both `/tablet` and `/screen` reflect the new title in $< 50\text{ms}$ with zero page reload.
- [ ] Zero console errors on all three windows.
