import { describe, it, expect } from 'vitest';
import {
	getTodayDateString,
	localSlotToUtcIso,
	getTimeInVenueTz,
	formatTimeInTz,
	formatDateInTz
} from '$lib/utils/dateTime';

describe('VenueAxe Timezone & DateTime Utilities', () => {
	it('converts a 10 PM slot in Central Time (CDT) to the exact UTC ISO string', () => {
		// Sept 11, 2026 22:00 in America/Chicago is Sept 12, 2026 03:00 UTC (CDT is UTC-5)
		const utcIso = localSlotToUtcIso('2026-09-11', '22:00', 'America/Chicago');
		expect(utcIso).toBe('2026-09-12T03:00:00.000Z');
	});

	it('converts a 10 PM slot in Eastern Time (EDT) to the exact UTC ISO string', () => {
		// Sept 11, 2026 22:00 in America/New_York is Sept 12, 2026 02:00 UTC (EDT is UTC-4)
		const utcIso = localSlotToUtcIso('2026-09-11', '22:00', 'America/New_York');
		expect(utcIso).toBe('2026-09-12T02:00:00.000Z');
	});

	it('extracts correct venue local hour and minute from a UTC ISO timestamp', () => {
		// UTC Sept 12 03:00:00Z corresponds to 22:00 (10 PM) in America/Chicago
		const time = getTimeInVenueTz('2026-09-12T03:00:00.000Z', 'America/Chicago');
		expect(time.hours).toBe(22);
		expect(time.minutes).toBe(0);
	});

	it('formats time string accurately in the venue timezone', () => {
		const formatted = formatTimeInTz('2026-09-12T03:00:00.000Z', 'America/Chicago');
		// Should format as 10:00 PM
		expect(formatted).toMatch(/10:00\s*(PM|pm)?/);
	});

	it('formats date string accurately in the venue timezone (does not shift to next day)', () => {
		const formatted = formatDateInTz('2026-09-12T03:00:00.000Z', 'America/Chicago');
		// Should format as Sep 11, 2026 (not Sep 12)
		expect(formatted).toContain('Sep 11, 2026');
	});

	it('calculates timeline block percentage accurately for a 10 PM booking', () => {
		const startIso = '2026-09-12T03:00:00.000Z'; // 22:00 in Chicago
		const endIso = '2026-09-12T04:00:00.000Z'; // 23:00 in Chicago

		const timeInZone = getTimeInVenueTz(startIso, 'America/Chicago');
		const startMinutes = timeInZone.hours * 60 + timeInZone.minutes;
		const durationMinutes = (new Date(endIso).getTime() - new Date(startIso).getTime()) / (1000 * 60);

		const leftPct = (startMinutes / (24 * 60)) * 100;
		const widthPct = (durationMinutes / (24 * 60)) * 100;

		// 22 * 60 = 1320 minutes / 1440 = 91.666...%
		expect(leftPct).toBeCloseTo(91.67, 1);
		// 60 minutes / 1440 = 4.166...%
		expect(widthPct).toBeCloseTo(4.17, 1);
	});
});
