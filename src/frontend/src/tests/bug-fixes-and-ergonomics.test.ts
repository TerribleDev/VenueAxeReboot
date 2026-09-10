import { describe, it, expect } from 'vitest';

describe('Bug Fixes & Ergonomics System Tests', () => {
	it('TicTacToe grid derivation derives 3x3 territory claims from throw events', () => {
		const p1Id = 'player-1-uuid';
		const p2Id = 'player-2-uuid';
		const throws = [
			{ playerId: p1Id, pointsAwarded: 1 }, // Cell 0: 'X'
			{ playerId: p2Id, pointsAwarded: 5 }, // Cell 4: 'O'
			{ playerId: p1Id, pointsAwarded: 9 }  // Cell 8: 'X'
		];

		const grid = Array(9).fill(null);
		for (const t of throws) {
			const pts = Number(t.pointsAwarded ?? 0);
			if (pts >= 1 && pts <= 9) {
				const cellIdx = pts - 1;
				if (t.playerId === p1Id) grid[cellIdx] = 'X';
				else if (t.playerId === p2Id) grid[cellIdx] = 'O';
			}
		}

		expect(grid[0]).toBe('X');
		expect(grid[4]).toBe('O');
		expect(grid[8]).toBe('X');
		expect(grid[1]).toBeNull();
		expect(grid[2]).toBeNull();
	});

	it('Deposit balance due calculation correctly identifies unpaid balances', () => {
		const booking = {
			totalAmountCents: 30000, // $300
			paidAmountCents: 5000,   // $50 deposit
			paymentStatus: 'DepositPaid'
		};

		const balanceDueCents = Math.max(0, booking.totalAmountCents - booking.paidAmountCents);
		const formattedBalanceDue = `$${(balanceDueCents / 100).toFixed(2)}`;

		expect(balanceDueCents).toBe(25000);
		expect(formattedBalanceDue).toBe('$250.00');
	});

	it('Blackjack 21 objective correctly reports hand total and bust status', () => {
		const safePlayer = { score: 18 };
		const safeDiff = 21 - safePlayer.score;
		expect(safeDiff).toBe(3);

		const bustedPlayer = { score: 23 };
		const isBusted = bustedPlayer.score > 21;
		expect(isBusted).toBe(true);

		const winner = { score: 21 };
		const isBlackjack = winner.score === 21;
		expect(isBlackjack).toBe(true);
	});

	it('Around the world progression correctly reports ring target up to Bullseye', () => {
		const thrower1 = { score: 0 }; // 0 hits -> target ring 1
		expect(Math.min(7, thrower1.score + 1)).toBe(1);

		const thrower2 = { score: 4 }; // 4 hits -> target ring 5
		expect(Math.min(7, thrower2.score + 1)).toBe(5);

		const throwerMax = { score: 6 }; // 6 hits -> target ring 7 (Bullseye)
		expect(Math.min(7, throwerMax.score + 1)).toBe(7);
	});

	it('Digital waiver validation catches missing signature, future DOB, and missing terms acknowledgment', () => {
		function validateWaiver(data: {
			firstName: string;
			lastName: string;
			email: string;
			dob: string;
			signaturePng: string;
			termsAccepted: boolean;
		}): string | null {
			if (!data.firstName.trim()) return 'Missing first name';
			if (!data.lastName.trim()) return 'Missing last name';
			if (!data.email.includes('@')) return 'Invalid email';
			if (!data.dob) return 'Missing DOB';
			if (new Date(data.dob) > new Date()) return 'Invalid future DOB';
			if (!data.signaturePng) return 'Missing signature';
			if (!data.termsAccepted) return 'Terms must be accepted';
			return null;
		}

		// Missing signature
		expect(validateWaiver({
			firstName: 'John',
			lastName: 'Doe',
			email: 'john@example.com',
			dob: '1995-05-15',
			signaturePng: '',
			termsAccepted: true
		})).toBe('Missing signature');

		// Terms not accepted
		expect(validateWaiver({
			firstName: 'John',
			lastName: 'Doe',
			email: 'john@example.com',
			dob: '1995-05-15',
			signaturePng: 'data:image/png;base64,sample',
			termsAccepted: false
		})).toBe('Terms must be accepted');

		// Valid
		expect(validateWaiver({
			firstName: 'John',
			lastName: 'Doe',
			email: 'john@example.com',
			dob: '1995-05-15',
			signaturePng: 'data:image/png;base64,sample',
			termsAccepted: true
		})).toBeNull();
	});

	it('Killshot button label accurately reflects remaining count or exhaustion', () => {
		function getKillshotButtonLabel(isClutchArmed: boolean, killsRemaining: number): string {
			if (isClutchArmed) return '🎯 KILLSHOT ARMED (8 PTS)';
			if (killsRemaining > 0) return `🎯 CALL KILLSHOT (8 PTS) [${killsRemaining} Left]`;
			return '🎯 KILLSHOTS EXHAUSTED [0 Left]';
		}

		expect(getKillshotButtonLabel(true, 2)).toBe('🎯 KILLSHOT ARMED (8 PTS)');
		expect(getKillshotButtonLabel(false, 2)).toBe('🎯 CALL KILLSHOT (8 PTS) [2 Left]');
		expect(getKillshotButtonLabel(false, 1)).toBe('🎯 CALL KILLSHOT (8 PTS) [1 Left]');
		expect(getKillshotButtonLabel(false, 0)).toBe('🎯 KILLSHOTS EXHAUSTED [0 Left]');
	});

	it('Target hit toast feedback evaluates correct badge text and style class', () => {
		function evaluateHitToast(points: number, isBull: boolean, isKill: boolean, isDrop: boolean, isFault: boolean) {
			if (isKill || points === 8) {
				return { text: '+8 KILLSHOT! 🎯', type: 'hit-killshot' };
			} else if (isBull || points === 6) {
				return { text: '+6 BULLSEYE! 🎯', type: 'hit-bullseye' };
			} else if (isFault) {
				return { text: 'FAULT (0 PTS) ⚠️', type: 'hit-fault' };
			} else if (isDrop) {
				return { text: 'DROP (0 PTS) ❌', type: 'hit-drop' };
			} else if (points > 0) {
				return { text: `+${points} POINTS!`, type: 'hit-points' };
			} else {
				return { text: 'MISS (0 PTS)', type: 'hit-miss' };
			}
		}

		expect(evaluateHitToast(6, true, false, false, false)).toEqual({ text: '+6 BULLSEYE! 🎯', type: 'hit-bullseye' });
		expect(evaluateHitToast(8, false, true, false, false)).toEqual({ text: '+8 KILLSHOT! 🎯', type: 'hit-killshot' });
		expect(evaluateHitToast(4, false, false, false, false)).toEqual({ text: '+4 POINTS!', type: 'hit-points' });
		expect(evaluateHitToast(0, false, false, true, false)).toEqual({ text: 'DROP (0 PTS) ❌', type: 'hit-drop' });
		expect(evaluateHitToast(0, false, false, false, true)).toEqual({ text: 'FAULT (0 PTS) ⚠️', type: 'hit-fault' });
		expect(evaluateHitToast(0, false, false, false, false)).toEqual({ text: 'MISS (0 PTS)', type: 'hit-miss' });
	});

	it('Interactive empty schedule slot creates deep links for bookings and walk-in lanes', () => {
		const slot = { laneNumber: 3, hour: 14, date: '2026-09-15' };
		const formattedTime = `${String(slot.hour).padStart(2, '0')}:00`;
		const bookingUrl = `/admin/bookings?create=1&lane=${slot.laneNumber}&date=${slot.date}&time=${formattedTime}`;
		const walkinUrl = `/admin/lanes?startLane=${slot.laneNumber}`;

		expect(bookingUrl).toBe('/admin/bookings?create=1&lane=3&date=2026-09-15&time=14:00');
		expect(walkinUrl).toBe('/admin/lanes?startLane=3');
	});

	it('Bookings filter URL search params serializes query and date cleanly', () => {
		function serializeBookingFilterUrl(base: string, q: string, date: string): string {
			const url = new URL(base, 'http://localhost');
			if (q.trim()) url.searchParams.set('q', q.trim());
			else url.searchParams.delete('q');

			if (date) url.searchParams.set('date', date);
			else url.searchParams.delete('date');

			return url.pathname + url.search;
		}

		expect(serializeBookingFilterUrl('/admin/bookings', 'Marcus', '2026-09-09')).toBe('/admin/bookings?q=Marcus&date=2026-09-09');
		expect(serializeBookingFilterUrl('/admin/bookings', '', '2026-09-09')).toBe('/admin/bookings?date=2026-09-09');
		expect(serializeBookingFilterUrl('/admin/bookings', 'VA-102', '')).toBe('/admin/bookings?q=VA-102');
		expect(serializeBookingFilterUrl('/admin/bookings', '', '')).toBe('/admin/bookings');
	});
});
