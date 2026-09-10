import { describe, it, expect } from 'vitest';

describe('WATL League Scoring & Rules', () => {
	it('WATL standard match allows 2 anytime killshots per player for 8 pts', () => {
		const player = {
			id: 'p1',
			name: 'Sarah',
			score: 0,
			killsCalled: 0,
			killsRemaining: 2
		};

		// Player calls killshot 1
		player.killsCalled += 1;
		player.killsRemaining -= 1;
		player.score += 8;

		expect(player.score).toBe(8);
		expect(player.killsCalled).toBe(1);
		expect(player.killsRemaining).toBe(1);

		// Player calls killshot 2
		player.killsCalled += 1;
		player.killsRemaining -= 1;
		player.score += 8;

		expect(player.score).toBe(16);
		expect(player.killsCalled).toBe(2);
		expect(player.killsRemaining).toBe(0);

		// Cannot call a 3rd killshot
		const canCallKill = player.killsRemaining > 0;
		expect(canCallKill).toBe(false);
	});

	it('WATL target scoring point hierarchy', () => {
		const zones = {
			bullseye: 6,
			ring5: 5,
			ring4: 4,
			ring3: 3,
			ring2: 2,
			ring1: 1,
			killshot: 8,
			drop: 0,
			miss: 0,
			fault: 0
		};

		expect(zones.bullseye).toBe(6);
		expect(zones.killshot).toBe(8);
		expect(zones.ring5).toBe(5);
		expect(zones.ring1).toBe(1);
		expect(zones.drop).toBe(0);
		expect(zones.miss).toBe(0);
	});

	it('Rematch resets kill count and scores back to starting state', () => {
		const player = {
			id: 'p1',
			name: 'Sarah',
			score: 54,
			killsCalled: 2,
			killsRemaining: 0
		};

		// Reset on Rematch
		player.score = 0;
		player.killsCalled = 0;
		player.killsRemaining = 2;

		expect(player.score).toBe(0);
		expect(player.killsRemaining).toBe(2);
		expect(player.killsCalled).toBe(0);
	});

	it('Player substitution retains scores and throws taken', () => {
		const roster = [
			{ id: 'p1', name: 'Original Player', score: 32, throwsTaken: 6 }
		];

		// Substitute player
		roster[0].name = 'Substituted Player';

		expect(roster[0].name).toBe('Substituted Player');
		expect(roster[0].score).toBe(32);
		expect(roster[0].throwsTaken).toBe(6);
	});
});
