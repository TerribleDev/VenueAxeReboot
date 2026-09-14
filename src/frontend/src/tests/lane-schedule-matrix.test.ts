import { describe, it, expect } from 'vitest';

describe('Lane Schedule Matrix Future Slot and Modal Logic', () => {
	function isSlotInFuture(dateStr: string, hour: number, refNow: Date): boolean {
		const todayStr = refNow.toISOString().split('T')[0];
		if (dateStr > todayStr) return true;
		if (dateStr < todayStr) return false;
		const slotDate = new Date(`${dateStr}T${String(hour).padStart(2, '0')}:00:00`);
		return slotDate.getTime() > refNow.getTime();
	}

	it('correctly classifies future slots on the same day', () => {
		const now = new Date('2026-09-11T17:30:00');
		// 16:00 is in the past
		expect(isSlotInFuture('2026-09-11', 16, now)).toBe(false);
		// 17:00 is in the past (17:00 < 17:30)
		expect(isSlotInFuture('2026-09-11', 17, now)).toBe(false);
		// 18:00 is in the future (18:00 > 17:30)
		expect(isSlotInFuture('2026-09-11', 18, now)).toBe(true);
		// 22:00 is in the future
		expect(isSlotInFuture('2026-09-11', 22, now)).toBe(true);
	});

	it('correctly classifies slots on future and past dates', () => {
		const now = new Date('2026-09-11T12:00:00');
		// Yesterday: all slots are in the past
		expect(isSlotInFuture('2026-09-10', 9, now)).toBe(false);
		expect(isSlotInFuture('2026-09-10', 20, now)).toBe(false);

		// Tomorrow: all slots are in the future
		expect(isSlotInFuture('2026-09-12', 0, now)).toBe(true);
		expect(isSlotInFuture('2026-09-12', 9, now)).toBe(true);
		expect(isSlotInFuture('2026-09-12', 18, now)).toBe(true);
	});

	it('allows active lanes to be reserved when in future, and rejects inactive lanes', () => {
		const lane1 = { laneNumber: 1, isActive: true };
		const lane2 = { laneNumber: 2, isActive: false };
		const now = new Date('2026-09-11T14:00:00');

		const canBookLane1Future = lane1.isActive && isSlotInFuture('2026-09-11', 16, now);
		const canBookLane1Past = lane1.isActive && isSlotInFuture('2026-09-11', 13, now);
		const canBookLane2Future = lane2.isActive && isSlotInFuture('2026-09-11', 16, now);

		expect(canBookLane1Future).toBe(true);
		expect(canBookLane1Past).toBe(false);
		expect(canBookLane2Future).toBe(false);
	});
});
