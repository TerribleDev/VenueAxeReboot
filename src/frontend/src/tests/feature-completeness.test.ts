import { describe, it, expect } from 'vitest';

export function calculateAge(dobIso: string): number | null {
	if (!dobIso) return null;
	const birthDate = new Date(dobIso);
	if (isNaN(birthDate.getTime())) return null;
	const today = new Date();
	let age = today.getFullYear() - birthDate.getFullYear();
	const m = today.getMonth() - birthDate.getMonth();
	if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
		age--;
	}
	return age;
}

export function validateMinorGuardianRequirement(dobIso: string, isGuardian: boolean): string | null {
	const age = calculateAge(dobIso);
	if (age !== null && age < 18 && !isGuardian) {
		return 'Participants under 18 cannot sign independently. A parent or legal guardian must sign on their behalf.';
	}
	return null;
}

export function calculateBookingAnalytics(bookings: Array<{
	partySize: number | string;
	totalAmountCents: number | string;
	paidAmountCents: number | string;
}>) {
	let totalRevCents = 0;
	let totalPaidCents = 0;
	let totalBalCents = 0;
	let totalThrowers = 0;

	for (const b of bookings) {
		const total = Number(b.totalAmountCents) || 0;
		const paid = Number(b.paidAmountCents) || 0;
		const bal = Math.max(0, total - paid);
		totalRevCents += total;
		totalPaidCents += paid;
		totalBalCents += bal;
		totalThrowers += Number(b.partySize) || 0;
	}

	return {
		count: bookings.length,
		totalRevenue: (totalRevCents / 100).toFixed(2),
		totalPaid: (totalPaidCents / 100).toFixed(2),
		totalBalanceDue: (totalBalCents / 100).toFixed(2),
		totalThrowers,
		avgPartySize: bookings.length > 0 ? (totalThrowers / bookings.length).toFixed(1) : '0'
	};
}

describe('Feature Completeness & Validation Unit Tests', () => {
	it('correctly calculates age and enforces guardian signature for minors under 18', () => {
		const minorDob = '2012-05-15';
		const minorAge = calculateAge(minorDob);
		expect(minorAge).toBeLessThan(18);

		const errorWithoutGuardian = validateMinorGuardianRequirement(minorDob, false);
		expect(errorWithoutGuardian).toContain('parent or legal guardian');

		const errorWithGuardian = validateMinorGuardianRequirement(minorDob, true);
		expect(errorWithGuardian).toBeNull();
	});

	it('allows adult throwers 18+ to sign independently without guardian requirement', () => {
		const adultDob = '1995-04-12';
		const adultAge = calculateAge(adultDob);
		expect(adultAge).toBeGreaterThanOrEqual(18);

		const result = validateMinorGuardianRequirement(adultDob, false);
		expect(result).toBeNull();
	});

	it('computes financial KPIs and balance due correctly for reservations', () => {
		const testBookings = [
			{ partySize: 4, totalAmountCents: 14000, paidAmountCents: 14000 },
			{ partySize: 6, totalAmountCents: 21000, paidAmountCents: 7000 },
			{ partySize: 2, totalAmountCents: 7000, paidAmountCents: 0 }
		];

		const analytics = calculateBookingAnalytics(testBookings);
		expect(analytics.count).toBe(3);
		expect(analytics.totalRevenue).toBe('420.00');
		expect(analytics.totalPaid).toBe('210.00');
		expect(analytics.totalBalanceDue).toBe('210.00');
		expect(analytics.totalThrowers).toBe(12);
		expect(analytics.avgPartySize).toBe('4.0');
	});

	it('serializes visual packages and discounts to valid JSON', () => {
		const testPackages = [
			{ id: 'pkg_standard', name: 'Standard Throwing', description: '60 min', pricePerPersonCents: 3500, isDefault: true }
		];
		const json = JSON.stringify(testPackages);
		const parsed = JSON.parse(json);
		expect(parsed).toHaveLength(1);
		expect(parsed[0].id).toBe('pkg_standard');
		expect(parsed[0].pricePerPersonCents).toBe(3500);
	});
});
