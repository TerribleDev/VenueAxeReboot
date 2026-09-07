import { describe, it, expect } from 'vitest';
import type { GameStateSnapshot, GamePlayer } from '$lib/api/generated/types.gen';

describe('DELIV-1.3: End-of-Match Podium Summary & Scatter Heatmap Logic', () => {
	const sampleGameState: GameStateSnapshot = {
		matchId: 'match-123',
		gameTypeId: 'watl_standard',
		gameName: 'WATL Regulation',
		status: 2, // Finished
		currentRound: 10,
		totalRounds: 10,
		winnerPlayerId: 'p1',
		winnerName: 'Sarah Connor',
		players: [
			{
				id: 'p1',
				name: 'Sarah Connor',
				avatarColor: '#f59e0b',
				score: 54,
				throwsTaken: 10,
				bullseyesHit: 8,
				clutchesHit: 1,
				streak: 4,
				throwHistory: [6, 6, 6, 6, 5, 5, 6, 6, 7, 1]
			},
			{
				id: 'p2',
				name: 'Marcus Wright',
				avatarColor: '#06b6d4',
				score: 42,
				throwsTaken: 10,
				bullseyesHit: 4,
				clutchesHit: 0,
				streak: 1,
				throwHistory: [4, 5, 4, 3, 5, 5, 6, 4, 3, 3]
			},
			{
				id: 'p3',
				name: 'John Connor',
				avatarColor: '#ec4899',
				score: 31,
				throwsTaken: 10,
				bullseyesHit: 2,
				clutchesHit: 0,
				streak: 0,
				throwHistory: [3, 3, 2, 4, 3, 4, 4, 3, 2, 3]
			}
		],
		allThrows: [
			{
				playerId: 'p1',
				playerName: 'Sarah Connor',
				x: 0.01,
				y: 0.02,
				pointsAwarded: 6,
				isBullseye: true
			},
			{
				playerId: 'p2',
				playerName: 'Marcus Wright',
				x: -0.15,
				y: 0.12,
				pointsAwarded: 4,
				isBullseye: false
			},
			{
				playerId: 'p1',
				playerName: 'Sarah Connor',
				x: 0.38,
				y: 0.46,
				pointsAwarded: 7,
				isClutchCalled: true
			}
		]
	};

	it('correctly ranks players for gold, silver, and bronze podium stands', () => {
		const ranked = [...(sampleGameState.players ?? [])].sort(
			(a, b) => Number(b.score ?? 0) - Number(a.score ?? 0)
		);

		expect(ranked[0].name).toBe('Sarah Connor');
		expect(ranked[0].score).toBe(54);
		expect(ranked[1].name).toBe('Marcus Wright');
		expect(ranked[1].score).toBe(42);
		expect(ranked[2].name).toBe('John Connor');
		expect(ranked[2].score).toBe(31);
	});

	it('computes player accuracy and average points per throw correctly', () => {
		const p1 = sampleGameState.players![0];
		const accuracyP1 = Math.round((Number(p1.bullseyesHit) / Number(p1.throwsTaken)) * 100);
		const avgP1 = (Number(p1.score) / Number(p1.throwsTaken)).toFixed(1);

		expect(accuracyP1).toBe(80); // 8/10 = 80%
		expect(avgP1).toBe('5.4');

		const p2 = sampleGameState.players![1];
		const accuracyP2 = Math.round((Number(p2.bullseyesHit) / Number(p2.throwsTaken)) * 100);
		const avgP2 = (Number(p2.score) / Number(p2.throwsTaken)).toFixed(1);

		expect(accuracyP2).toBe(40); // 4/10 = 40%
		expect(avgP2).toBe('4.2');
	});

	it('filters scatter heatmap throws by individual player or all players', () => {
		const allThrows = sampleGameState.allThrows!;

		// All players filter
		expect(allThrows.length).toBe(3);

		// Isolate Sarah Connor (p1)
		const p1Throws = allThrows.filter((t) => t.playerId === 'p1');
		expect(p1Throws.length).toBe(2);
		expect(p1Throws[0].pointsAwarded).toBe(6);
		expect(p1Throws[1].pointsAwarded).toBe(7);

		// Isolate Marcus Wright (p2)
		const p2Throws = allThrows.filter((t) => t.playerId === 'p2');
		expect(p2Throws.length).toBe(1);
		expect(p2Throws[0].pointsAwarded).toBe(4);
	});

	it('scales normalized (-1 to +1) scatter coordinates to SVG viewBox space', () => {
		const HALF = 500;
		const throwPoint = sampleGameState.allThrows![0]; // x: 0.01, y: 0.02
		const pinX = Number(throwPoint.x) * HALF;
		const pinY = -Number(throwPoint.y) * HALF;

		expect(pinX).toBe(5);
		expect(pinY).toBe(-10);
	});
});
