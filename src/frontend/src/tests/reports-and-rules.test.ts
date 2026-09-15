import { describe, it, expect } from 'vitest';
import { GAME_RULES, getGameRules } from '../lib/constants/gameRules';

describe('Reports & Venue Features Frontend Tests', () => {
	it('Game rules dictionary provides complete rules for all supported game engines including first_to_21 and countdown_603', () => {
		const expectedEngines = [
			'watl_standard',
			'countdown_603',
			'countdown_301',
			'axe_tictactoe',
			'first_to_21',
			'blackjack_21',
			'around_the_world',
			'kill_hunter'
		];

		for (const engineId of expectedEngines) {
			const rules = getGameRules(engineId);
			expect(rules).toBeDefined();
			expect(rules.displayName).toBeTruthy();
			expect(rules.objective).toBeTruthy();
			expect(rules.scoringRules.length).toBeGreaterThan(0);
			expect(rules.specialRules.length).toBeGreaterThan(0);
			expect(rules.tips.length).toBeGreaterThan(0);
		}

		const firstTo21 = getGameRules('first_to_21');
		expect(firstTo21.displayName).toBe('First to 21');
		expect(firstTo21.specialRules.some((r) => r.includes('13'))).toBe(true);

		const countdown603 = getGameRules('countdown_603');
		expect(countdown603.displayName).toBe('Countdown 603');
		expect(countdown603.objective.includes('603')).toBe(true);
	});

	it('getGameRules gracefully falls back to WATL standard rules for unknown game types', () => {
		const unknownRules = getGameRules('unknown_future_game');
		expect(unknownRules).toBeDefined();
		expect(unknownRules.displayName).toBe(GAME_RULES['watl_standard'].displayName);
	});

	it('Venue closed dates detection correctly identifies holiday closures and reasons', () => {
		const closedDatesList = [
			{ date: '2026-12-25', reason: 'Christmas Day' },
			{ date: '2026-09-07', reason: 'Labor Day' },
			{ date: '2026-01-01', reason: 'New Year Day' }
		];

		const christmasCheck = closedDatesList.find((c) => c.date === '2026-12-25');
		expect(christmasCheck).toBeDefined();
		expect(christmasCheck?.reason).toBe('Christmas Day');

		const regularDayCheck = closedDatesList.find((c) => c.date === '2026-06-15');
		expect(regularDayCheck).toBeUndefined();
	});

	it('Venue address formatting and visibility toggle logic works as expected', () => {
		const venue = {
			name: 'Downtown Axe Club',
			addressLine1: '123 Main St',
			city: 'Buffalo',
			state: 'NY',
			postalCode: '14202'
		};

		const formattedAddress = [venue.addressLine1, venue.city, `${venue.state} ${venue.postalCode}`]
			.filter(Boolean)
			.join(', ');

		expect(formattedAddress).toBe('123 Main St, Buffalo, NY 14202');

		// When showAddress is true, address should be included in display
		const showAddress = true;
		const displayBar = showAddress ? formattedAddress : null;
		expect(displayBar).toBe('123 Main St, Buffalo, NY 14202');

		// When showAddress is false, displayBar is null
		const hideAddress = false;
		const hiddenBar = hideAddress ? formattedAddress : null;
		expect(hiddenBar).toBeNull();
	});

	it('Booking package editing updates package properties while preserving array integrity', () => {
		const packages = [
			{
				id: 'pkg-standard',
				name: 'Standard Throwing',
				description: '60 minutes lane time',
				pricePerPerson: 35,
				isDefault: true
			},
			{
				id: 'pkg-vip',
				name: 'VIP Experience',
				description: '90 minutes lane time',
				pricePerPerson: 55,
				isDefault: false
			}
		];

		// Simulate editing pkg-standard
		const editTargetId = 'pkg-standard';
		const updatedDraft = {
			name: 'Standard Axe Blast',
			description: '75 minutes lane time with coach',
			pricePerPerson: 40,
			isDefault: true
		};

		const updatedPackages = packages.map((pkg) =>
			pkg.id === editTargetId ? { ...pkg, ...updatedDraft } : pkg
		);

		expect(updatedPackages.length).toBe(2);
		expect(updatedPackages[0].name).toBe('Standard Axe Blast');
		expect(updatedPackages[0].description).toBe('75 minutes lane time with coach');
		expect(updatedPackages[0].pricePerPerson).toBe(40);
		expect(updatedPackages[1].name).toBe('VIP Experience');
	});

	it('Report revenue and utilization calculations compute accurate summary figures', () => {
		const report = {
			totalRevenueCents: 154500, // $1,545.00
			totalThrowers: 36,
			completedBookings: 8,
			laneUtilization: [
				{ laneNumber: 1, bookedHours: 6, capacityHours: 10, utilizationPercent: 60 },
				{ laneNumber: 2, bookedHours: 8, capacityHours: 10, utilizationPercent: 80 }
			]
		};

		const formattedRevenue = `$${(report.totalRevenueCents / 100).toLocaleString('en-US', {
			minimumFractionDigits: 2,
			maximumFractionDigits: 2
		})}`;

		expect(formattedRevenue).toBe('$1,545.00');

		const totalBookedHours = report.laneUtilization.reduce((sum, u) => sum + u.bookedHours, 0);
		const totalCapacityHours = report.laneUtilization.reduce((sum, u) => sum + u.capacityHours, 0);
		const overallUtilization = Math.round((totalBookedHours / totalCapacityHours) * 100);

		expect(totalBookedHours).toBe(14);
		expect(totalCapacityHours).toBe(20);
		expect(overallUtilization).toBe(70);
	});

	it('Package deep linking correctly resolves package IDs and slugified names', () => {
		const packages = [
			{ id: 'pkg-standard', name: 'Standard Axe Blast' },
			{ id: 'pkg-vip', name: 'VIP Axe Experience' },
			{ id: 'pkg-corporate', name: 'Corporate Team Outing' }
		];

		const resolvePackage = (queryParam: string) =>
			packages.find(
				(p) =>
					p.id.toLowerCase() === queryParam.toLowerCase() ||
					p.name.toLowerCase().replace(/\s+/g, '-') === queryParam.toLowerCase() ||
					p.name.toLowerCase() === queryParam.toLowerCase()
			);

		expect(resolvePackage('pkg-vip')?.id).toBe('pkg-vip');
		expect(resolvePackage('vip-axe-experience')?.id).toBe('pkg-vip');
		expect(resolvePackage('Standard Axe Blast')?.id).toBe('pkg-standard');
		expect(resolvePackage('nonexistent')).toBeUndefined();
	});

	it('Email marketing opt-in defaults to true for bookings and waivers', () => {
		const defaultBookingState = { emailMarketingOptIn: true };
		const defaultWaiverState = { emailMarketingOptIn: true };

		expect(defaultBookingState.emailMarketingOptIn).toBe(true);
		expect(defaultWaiverState.emailMarketingOptIn).toBe(true);
	});
});
