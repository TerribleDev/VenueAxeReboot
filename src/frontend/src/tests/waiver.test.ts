import { describe, it, expect } from 'vitest';

export function getWaiverReadiness(signedCount: number, partySize: number): {
	status: 'complete' | 'partial' | 'missing';
	label: string;
	badgeClass: string;
} {
	if (signedCount >= partySize && partySize > 0) {
		return {
			status: 'complete',
			label: `✅ Fully Cleared (${signedCount}/${partySize})`,
			badgeClass: 'badge-waiver-complete'
		};
	}
	if (signedCount > 0) {
		return {
			status: 'partial',
			label: `⚠️ Partial (${signedCount}/${partySize})`,
			badgeClass: 'badge-waiver-partial'
		};
	}
	return {
		status: 'missing',
		label: `❌ 0/${partySize} Signed`,
		badgeClass: 'badge-waiver-missing'
	};
}

export function formatKioskCountdownText(seconds: number): string {
	return `Auto-resetting for next guest in ${seconds}s...`;
}

describe('Waiver Readiness and Kiosk Helpers', () => {
	it('returns complete when signed count matches or exceeds party size', () => {
		const result = getWaiverReadiness(6, 6);
		expect(result.status).toBe('complete');
		expect(result.badgeClass).toBe('badge-waiver-complete');
		expect(result.label).toContain('Fully Cleared');

		const exceeded = getWaiverReadiness(8, 6);
		expect(exceeded.status).toBe('complete');
	});

	it('returns partial when some throwers have signed but not all', () => {
		const result = getWaiverReadiness(3, 6);
		expect(result.status).toBe('partial');
		expect(result.badgeClass).toBe('badge-waiver-partial');
		expect(result.label).toContain('Partial (3/6)');
	});

	it('returns missing when zero throwers have signed', () => {
		const result = getWaiverReadiness(0, 4);
		expect(result.status).toBe('missing');
		expect(result.badgeClass).toBe('badge-waiver-missing');
		expect(result.label).toContain('0/4 Signed');
	});

	it('formats kiosk countdown text correctly', () => {
		expect(formatKioskCountdownText(10)).toBe('Auto-resetting for next guest in 10s...');
		expect(formatKioskCountdownText(1)).toBe('Auto-resetting for next guest in 1s...');
	});
});
