import { describe, it, expect } from 'vitest';

describe('Arcade Game Engines - Client Logic', () => {
	it('Around The World target steps evaluate correctly', () => {
		const steps = ['Ring 1', 'Ring 2', 'Ring 3', 'Ring 4', 'Ring 5', 'Bullseye', 'Clutch'];
		expect(steps[0]).toBe('Ring 1');
		expect(steps[5]).toBe('Bullseye');
		expect(steps[6]).toBe('Clutch');

		const getNextTarget = (score: number) => steps[Math.min(Math.max(0, score), 6)];
		expect(getNextTarget(0)).toBe('Ring 1');
		expect(getNextTarget(3)).toBe('Ring 4');
		expect(getNextTarget(6)).toBe('Clutch');
		expect(getNextTarget(7)).toBe('Clutch');
	});

	it('Blackjack 21 remaining points to 21 calculates properly', () => {
		const getNeedFor21 = (score: number) => 21 - score;
		expect(getNeedFor21(17)).toBe(4);
		expect(getNeedFor21(20)).toBe(1);
		expect(getNeedFor21(21)).toBe(0);
	});

	it('Axe Tic-Tac-Toe cell mapping is bounded 0 to 8', () => {
		const mapCoord = (x: number, y: number) => {
			const col = x < -0.16 ? 0 : x <= 0.16 ? 1 : 2;
			const row = y > 0.16 ? 0 : y >= -0.16 ? 1 : 2;
			return row * 3 + col;
		};

		// Top-left
		expect(mapCoord(-0.3, 0.3)).toBe(0);
		// Center
		expect(mapCoord(0, 0)).toBe(4);
		// Bottom-right
		expect(mapCoord(0.3, -0.3)).toBe(8);
	});
});
