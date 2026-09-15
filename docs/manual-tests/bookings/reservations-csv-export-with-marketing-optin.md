# Manual Test: Reservations CSV Export with Marketing Opt-In

**Feature**: Reservations Management & Marketing Compliance  
**Test ID**: `BOOK-002`  
**Target URL**: `http://localhost:5173/admin/bookings`  

---

## 1. Purpose & Objectives
Validate that the customer reservations CSV export properly includes the `Marketing Opt-In` column, correctly reflects whether each guest opted into promotional materials (`Yes` or `No`), and conforms to RFC 4180 CSV specifications (proper quoting, delimiters, and financial calculations).

---

## 2. Prerequisites
1. Operator logged in at `http://localhost:5173/admin/bookings`.
2. Existing reservations with both opted-in (`EmailMarketingOptIn = true`) and opted-out (`EmailMarketingOptIn = false`) guests.

---

## 3. Step-by-Step Test Procedure
1. Navigate to `http://localhost:5173/admin/bookings`.
2. Ensure the reservations table displays active bookings.
3. Locate the **📥 Export CSV** button in the top action bar.
4. Click **📥 Export CSV**.
5. The browser will download a file named `venueaxe-bookings-{date}.csv`.
6. Open the downloaded CSV file in a text editor or spreadsheet application (Excel / Google Sheets).
7. Inspect the header row and data rows.

---

## 4. What to Look For
- Check the CSV header row: Confirm `Marketing Opt-In` is present as the final column.
- Check data rows: Confirm values in the `Marketing Opt-In` column are strictly `"Yes"` or `"No"`.
- Check financial columns: Total Amount, Paid Amount, and Balance Due match values displayed on the admin table.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Header contains: `Booking Reference,Guest First Name,Guest Last Name,Email,Phone,Party Size,Start Time (UTC),Duration (min),Assigned Lanes,Total Amount ($),Paid Amount ($),Balance Due ($),Status,Marketing Opt-In`.
- [ ] Row values for guests with `EmailMarketingOptIn = true` export as `"Yes"`.
- [ ] Row values for guests with `EmailMarketingOptIn = false` export as `"No"`.
- [ ] Double quotes in guest names or notes are properly escaped according to RFC 4180.
- [ ] Export triggers with zero console errors.
