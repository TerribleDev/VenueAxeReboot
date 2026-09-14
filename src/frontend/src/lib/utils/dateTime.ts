/**
 * VenueAxe Timezone & DateTime Utilities
 * Ensures consistent handling of venue operating timezones across all scheduling,
 * booking modals, public wizards, and live timers.
 */

/**
 * Returns today's date formatted as YYYY-MM-DD in the venue's timezone (or browser local).
 * Avoids the common bug where new Date().toISOString() rolls over to tomorrow's date
 * when it is evening in the Americas (EDT, CDT, MDT, PDT).
 */
export function getTodayDateString(timeZone?: string | null): string {
	const d = new Date();
	try {
		if (timeZone) {
			const parts = new Intl.DateTimeFormat('en-US', {
				timeZone,
				year: 'numeric',
				month: '2-digit',
				day: '2-digit'
			}).formatToParts(d);
			const y = parts.find((p) => p.type === 'year')?.value;
			const m = parts.find((p) => p.type === 'month')?.value;
			const day = parts.find((p) => p.type === 'day')?.value;
			if (y && m && day) {
				return `${y}-${m}-${day}`;
			}
		}
	} catch {
		// Fallback to local
	}
	const y = d.getFullYear();
	const m = String(d.getMonth() + 1).padStart(2, '0');
	const day = String(d.getDate()).padStart(2, '0');
	return `${y}-${m}-${day}`;
}

/**
 * Converts a venue-local date string (YYYY-MM-DD) and time string (HH:mm)
 * into a precise UTC ISO-8601 string (e.g. 2026-09-12T03:00:00.000Z).
 */
export function localSlotToUtcIso(dateStr: string, timeStr: string, timeZone?: string | null): string {
	if (!timeZone) {
		return new Date(`${dateStr}T${timeStr}:00`).toISOString();
	}
	try {
		const time = timeStr.length === 5 ? `${timeStr}:00` : timeStr;
		const approx = new Date(`${dateStr}T${time}Z`);
		const parts = new Intl.DateTimeFormat('en-US', {
			timeZone,
			year: 'numeric',
			month: '2-digit',
			day: '2-digit',
			hour: '2-digit',
			minute: '2-digit',
			second: '2-digit',
			hour12: false
		}).formatToParts(approx);
		const m: Record<string, string> = {};
		for (const p of parts) m[p.type] = p.value;
		const tzHour = m.hour === '24' ? '00' : m.hour;
		const asInTz = new Date(`${m.year}-${m.month}-${m.day}T${tzHour}:${m.minute}:${m.second}Z`);
		const offsetMs = approx.getTime() - asInTz.getTime();
		return new Date(approx.getTime() + offsetMs).toISOString();
	} catch {
		return new Date(`${dateStr}T${timeStr}:00`).toISOString();
	}
}

/**
 * Extracts the hour and minute for a date/timestamp in the venue's timezone.
 */
export function getTimeInVenueTz(
	dateInput: Date | string,
	timeZone?: string | null
): { hours: number; minutes: number } {
	const d = typeof dateInput === 'string' ? new Date(dateInput) : dateInput;
	try {
		if (timeZone) {
			const parts = new Intl.DateTimeFormat('en-US', {
				timeZone,
				hour: 'numeric',
				minute: 'numeric',
				hour12: false
			}).formatToParts(d);
			let h = parseInt(parts.find((p) => p.type === 'hour')?.value ?? '0', 10);
			if (h === 24) h = 0;
			const m = parseInt(parts.find((p) => p.type === 'minute')?.value ?? '0', 10);
			return { hours: h, minutes: m };
		}
	} catch {
		// Fallback
	}
	return { hours: d.getHours(), minutes: d.getMinutes() };
}

/**
 * Formats a timestamp into a time string (e.g. "10:00 PM") in the venue's timezone.
 */
export function formatTimeInTz(
	dateInput: Date | string,
	timeZone?: string | null,
	options: Intl.DateTimeFormatOptions = { hour: 'numeric', minute: '2-digit' }
): string {
	const d = typeof dateInput === 'string' ? new Date(dateInput) : dateInput;
	try {
		return d.toLocaleTimeString([], {
			...options,
			timeZone: timeZone || undefined
		});
	} catch {
		return d.toLocaleTimeString([], options);
	}
}

/**
 * Formats a timestamp into a date string (e.g. "Sep 11, 2026") in the venue's timezone.
 */
export function formatDateInTz(
	dateInput: Date | string,
	timeZone?: string | null,
	options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric', year: 'numeric' }
): string {
	const d = typeof dateInput === 'string' ? new Date(dateInput) : dateInput;
	try {
		return d.toLocaleDateString([], {
			...options,
			timeZone: timeZone || undefined
		});
	} catch {
		return d.toLocaleDateString([], options);
	}
}
