import { describe, it, expect } from 'vitest';

export interface PersonTypeItem {
	id: string;
	name: string;
	description: string;
	discountPercent: number;
	isDefault?: boolean;
}

export function autoBalanceAllocation(
	partySize: number,
	personTypes: PersonTypeItem[],
	currentCounts: Record<string, number>,
	incrementId: string
): Record<string, number> {
	const counts = { ...currentCounts };
	const total = Object.values(counts).reduce((a, b) => a + (b || 0), 0);

	if (total < partySize) {
		counts[incrementId] = (counts[incrementId] || 0) + 1;
	} else if (total === partySize) {
		if (incrementId !== 'adult' && (counts['adult'] || 0) > 0) {
			counts['adult'] = (counts['adult'] || 0) - 1;
			counts[incrementId] = (counts[incrementId] || 0) + 1;
		} else {
			const otherKey = Object.keys(counts).find((k) => k !== incrementId && (counts[k] || 0) > 0);
			if (otherKey) {
				counts[otherKey] = (counts[otherKey] || 0) - 1;
				counts[incrementId] = (counts[incrementId] || 0) + 1;
			}
		}
	}
	return counts;
}

export function calculatePersonTypeDiscounts(
	baseUnitPriceCents: number,
	personTypes: PersonTypeItem[],
	counts: Record<string, number>
) {
	let totalDiscountCents = 0;
	for (const pt of personTypes) {
		const count = counts[pt.id] || 0;
		if (count > 0 && pt.discountPercent > 0) {
			const perPersonDiscount = Math.round((baseUnitPriceCents * pt.discountPercent) / 100);
			totalDiscountCents += perPersonDiscount * count;
		}
	}
	return totalDiscountCents;
}

export function canDeletePersonType(ptId: string): boolean {
	return ptId.toLowerCase() !== 'adult';
}

describe('Person Types and Rates Configuration', () => {
	const catalog: PersonTypeItem[] = [
		{ id: 'adult', name: 'Adult', description: 'Ages 18+', discountPercent: 0, isDefault: true },
		{ id: 'minor', name: 'Minor', description: 'Ages 10-17', discountPercent: 0, isDefault: false },
		{ id: 'first_responder', name: 'First Responder', description: 'Police, Fire, EMT, Military', discountPercent: 10, isDefault: false }
	];

	it('protects Adult from deletion while allowing other types to be deleted', () => {
		expect(canDeletePersonType('adult')).toBe(false);
		expect(canDeletePersonType('Adult')).toBe(false);
		expect(canDeletePersonType('minor')).toBe(true);
		expect(canDeletePersonType('first_responder')).toBe(true);
	});

	it('auto-balances party allocation from Adult when incrementing other person types', () => {
		const partySize = 5;
		let counts: Record<string, number> = { adult: 5, minor: 0, first_responder: 0 };

		// Customer selects 1 minor
		counts = autoBalanceAllocation(partySize, catalog, counts, 'minor');
		expect(counts.adult).toBe(4);
		expect(counts.minor).toBe(1);
		expect(counts.first_responder).toBe(0);

		// Customer selects 1 first responder
		counts = autoBalanceAllocation(partySize, catalog, counts, 'first_responder');
		expect(counts.adult).toBe(3);
		expect(counts.minor).toBe(1);
		expect(counts.first_responder).toBe(1);

		// Total always matches party size
		const total = Object.values(counts).reduce((a, b) => a + b, 0);
		expect(total).toBe(5);
	});

	it('calculates person type discounts correctly', () => {
		const basePriceCents = 3500; // $35.00
		const counts = { adult: 3, minor: 1, first_responder: 1 };

		// Adult: 0%, Minor: 0%, First Responder: 10% ($3.50)
		const discount = calculatePersonTypeDiscounts(basePriceCents, catalog, counts);
		expect(discount).toBe(350); // $3.50
	});

	it('calculates multi-person discounts correctly with custom rates', () => {
		const basePriceCents = 4000; // $40.00
		const customCatalog: PersonTypeItem[] = [
			{ id: 'adult', name: 'Adult', description: '', discountPercent: 0 },
			{ id: 'minor', name: 'Minor', description: '', discountPercent: 15 }, // 15% off $40 = $6.00
			{ id: 'first_responder', name: 'First Responder', description: '', discountPercent: 20 } // 20% off $40 = $8.00
		];
		const counts = { adult: 2, minor: 2, first_responder: 1 };

		// 2 Minors * $6.00 = $12.00 (1200) + 1 FR * $8.00 = $8.00 (800) -> total $20.00 (2000)
		const discount = calculatePersonTypeDiscounts(basePriceCents, customCatalog, counts);
		expect(discount).toBe(2000);
	});
});
