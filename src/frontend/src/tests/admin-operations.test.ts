import { describe, it, expect } from 'vitest';

describe('Admin Operations & Schedule Matrix Calculations', () => {
	it('calculates timeline left and width percentages accurately for 14-hour window', () => {
		// Operating window: 10:00 AM to 12:00 AM (midnight) = 14 hours = 840 minutes
		const windowStartHour = 10;
		const totalWindowMinutes = 14 * 60; // 840 minutes

		// Booking: 2:00 PM (14:00) to 3:00 PM (15:00) = 60 minutes
		const bookingStartHour = 14;
		const bookingStartMin = 0;
		const durationMin = 60;

		const startOffsetMin = (bookingStartHour - windowStartHour) * 60 + bookingStartMin; // 240 min
		const leftPct = (startOffsetMin / totalWindowMinutes) * 100;
		const widthPct = (durationMin / totalWindowMinutes) * 100;

		expect(leftPct).toBeCloseTo(28.57, 1);
		expect(widthPct).toBeCloseTo(7.14, 1);
	});

	it('detects contiguous lane assignment sequences', () => {
		const isContiguous = (laneNumbers: number[]) => {
			if (laneNumbers.length <= 1) return true;
			const sorted = [...laneNumbers].sort((a, b) => a - b);
			for (let i = 1; i < sorted.length; i++) {
				if (sorted[i] !== sorted[i - 1] + 1) return false;
			}
			return true;
		};

		expect(isContiguous([1, 2])).toBe(true);
		expect(isContiguous([3, 4, 5])).toBe(true);
		expect(isContiguous([1, 3])).toBe(false);
		expect(isContiguous([2, 4, 5])).toBe(false);
	});

	it('calculates session remaining time extensions accurately', () => {
		const applyExtension = (currentMinutes: number, extraMinutes: number) => {
			return Math.max(0, currentMinutes) + extraMinutes;
		};

		expect(applyExtension(15, 15)).toBe(30);
		expect(applyExtension(5, 30)).toBe(35);
		expect(applyExtension(0, 15)).toBe(15);
	});

	it('formats booking time labels into clean 12-hour strings', () => {
		const formatHourHeader = (hour: number) => {
			if (hour > 12) return `${hour - 12} PM`;
			if (hour === 12) return '12 PM';
			return `${hour} AM`;
		};

		expect(formatHourHeader(10)).toBe('10 AM');
		expect(formatHourHeader(12)).toBe('12 PM');
		expect(formatHourHeader(14)).toBe('2 PM');
		expect(formatHourHeader(23)).toBe('11 PM');
	});
});
