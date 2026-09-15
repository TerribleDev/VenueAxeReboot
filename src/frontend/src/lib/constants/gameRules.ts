export interface GameRuleDetails {
	gameTypeId: string;
	displayName: string;
	icon: string;
	badge: string;
	defaultRounds: number;
	objective: string;
	scoringRules: string[];
	specialRules: string[];
	tips: string[];
}

export const GAME_RULES: Record<string, GameRuleDetails> = {
	watl_standard: {
		gameTypeId: 'watl_standard',
		displayName: 'WATL Standard Match',
		icon: '🎯',
		badge: '10 Throws • Regulation',
		defaultRounds: 10,
		objective: 'Score the highest aggregate point total over 10 rounds of competitive axe throwing.',
		scoringRules: [
			'Bullseye: 6 points (inner center ring)',
			'Ring 5: 5 points',
			'Ring 4: 4 points',
			'Ring 3: 3 points',
			'Ring 2: 2 points',
			'Ring 1: 1 point (outer boundary ring)',
			'Miss / Drop / Fault: 0 points',
			'Line-Breaking Rule: If the axe blade breaks any line, the higher point value is awarded'
		],
		specialRules: [
			'Killshots (8 Points): Each player is granted exactly 2 Killshots per match that can be called on ANY throw.',
			'Call Before Throwing: A Killshot must be verbally or digitally armed before the throw.',
			'Uncalled Hits: An uncalled hit on a Killshot corner circle awards strictly 0 points.',
			'Undoing a throw that consumed a Killshot restores your Killshot quota.'
		],
		tips: [
			'Save your Killshots for when you need a comeback or want to seal a lead in the final frames.',
			'Focus on consistent rotation and release from 12-15 feet behind the throwing line.'
		]
	},
	kill_hunter: {
		gameTypeId: 'kill_hunter',
		displayName: 'Kill Hunter',
		icon: '💀',
		badge: 'High Precision • 10 Rounds',
		defaultRounds: 10,
		objective: 'An elite accuracy shootout! Only Bullseyes and Killshots award points; all outer rings score zero.',
		scoringRules: [
			'Killshots (Upper Left / Right): 8 points',
			'Bullseye (Center): 6 points',
			'Rings 1 through 5: 0 points (Zero!)',
			'Misses & Drops: 0 points'
		],
		specialRules: [
			'Both Killshots are permanently armed for the entire match—no calls required!',
			'Any throw landing outside the Bullseye or Killshots awards zero points.'
		],
		tips: [
			'Choose whether you want the higher consistency of the 6-point Bullseye or the high reward of 8-point Killshots.'
		]
	},
	around_the_world: {
		gameTypeId: 'around_the_world',
		displayName: 'Around The World',
		icon: '🌍',
		badge: 'Sequential Milestones',
		defaultRounds: 15,
		objective: 'Hit every target ring in strict sequence from the outside ring all the way to the center Bullseye and Clutch!',
		scoringRules: [
			'Milestone Sequence: Ring 1 (1) → Ring 2 (2) → Ring 3 (3) → Ring 4 (4) → Ring 5 (5) → Bullseye (6) → Clutch (7)',
			'Each successful milestone hit advances your target marker to the next ring.'
		],
		specialRules: [
			'You must hit your current active milestone ring to advance.',
			'Throws landing on non-target rings do not advance your milestone.',
			'First thrower to complete all 7 target rings wins the match immediately!'
		],
		tips: [
			'Start by throwing wide to clip Ring 1, then progressively tighten your grouping toward center.'
		]
	},
	axe_tictactoe: {
		gameTypeId: 'axe_tictactoe',
		displayName: 'Axe Tic-Tac-Toe',
		icon: '❌',
		badge: '3x3 Territory Grid',
		defaultRounds: 9,
		objective: 'Claim 3 territory cells in a line (horizontal, vertical, or diagonal) across the interactive 3x3 grid.',
		scoringRules: [
			'A hit inside any open 3x3 sector locks and claims that square for your team (Team X or Team O).',
			'Throws outside the 3x3 grid do not claim any cells.'
		],
		specialRules: [
			'Once a square is claimed, it is permanently locked and cannot be stolen.',
			'Connecting 3 claimed squares in a row triggers an immediate knockout victory!',
			'If all 9 squares are claimed without 3-in-a-row, the team holding the most squares wins.'
		],
		tips: [
			'Claim the center square early to maximize your connection paths and block your opponent.'
		]
	},
	first_to_21: {
		gameTypeId: 'first_to_21',
		displayName: 'First to 21',
		icon: '🃏',
		badge: 'Target Exactly 21',
		defaultRounds: 10,
		objective: 'Throw to accumulate points aiming for exactly 21 without going over! Reaching 21 wins immediately.',
		scoringRules: [
			'Bullseye: 6 points',
			'Ring 5: 5 points',
			'Ring 4: 4 points',
			'Ring 3: 3 points',
			'Ring 2: 2 points',
			'Ring 1: 1 point',
			'Killshot (when armed): 8 points'
		],
		specialRules: [
			'Bust Penalty: If any throw takes your score over 21, you BUST! Your score drops back to 13.',
			'First player to hit exactly 21 wins instantly.',
			'If rounds expire, the player closest to 21 without busting wins.'
		],
		tips: [
			'When you reach 15-20, carefully aim for the outer 1, 2, or 3 rings rather than high-scoring centers.'
		]
	},
	blackjack_21: {
		gameTypeId: 'blackjack_21',
		displayName: 'First to 21',
		icon: '🃏',
		badge: 'Target Exactly 21',
		defaultRounds: 10,
		objective: 'Throw to accumulate points aiming for exactly 21 without going over! Reaching 21 wins immediately.',
		scoringRules: [
			'Bullseye: 6 points',
			'Ring 5: 5 points',
			'Ring 4: 4 points',
			'Ring 3: 3 points',
			'Ring 2: 2 points',
			'Ring 1: 1 point',
			'Killshot (when armed): 8 points'
		],
		specialRules: [
			'Bust Penalty: If any throw takes your score over 21, you BUST! Your score drops back to 13.',
			'First player to hit exactly 21 wins instantly.',
			'If rounds expire, the player closest to 21 without busting wins.'
		],
		tips: [
			'When you reach 15-20, carefully aim for the outer 1, 2, or 3 rings rather than high-scoring centers.'
		]
	},
	countdown_603: {
		gameTypeId: 'countdown_603',
		displayName: 'Countdown 603',
		icon: '⏱️',
		badge: 'Darts Style • Countdown to 0',
		defaultRounds: 15,
		objective: 'Start at 603 points and subtract your throw scores on every turn. First player to reach exactly 0 wins!',
		scoringRules: [
			'Bullseye: -6 points',
			'Rings 1-5: -1 to -5 points',
			'Killshots: -8 points',
			'Miss / Drop: 0 point deduction'
		],
		specialRules: [
			'Bust Rule: If a throw reduces your score below 0 (negative) or to 1, you BUST!',
			'On a bust, your turn ends immediately and your score reverts to the start of that turn.',
			'You must hit the exact score needed to reach 0.'
		],
		tips: [
			'Score heavily early on with Bullseyes and Killshots, then plan your outs carefully as you approach single digits.'
		]
	},
	countdown_301: {
		gameTypeId: 'countdown_301',
		displayName: 'Countdown 603',
		icon: '⏱️',
		badge: 'Darts Style • Countdown to 0',
		defaultRounds: 15,
		objective: 'Start at 603 points and subtract your throw scores on every turn. First player to reach exactly 0 wins!',
		scoringRules: [
			'Bullseye: -6 points',
			'Rings 1-5: -1 to -5 points',
			'Killshots: -8 points',
			'Miss / Drop: 0 point deduction'
		],
		specialRules: [
			'Bust Rule: If a throw reduces your score below 0 (negative) or to 1, you BUST!',
			'On a bust, your turn ends immediately and your score reverts to the start of that turn.',
			'You must hit the exact score needed to reach 0.'
		],
		tips: [
			'Score heavily early on with Bullseyes and Killshots, then plan your outs carefully as you approach single digits.'
		]
	}
};

export function getGameRules(gameTypeId: string): GameRuleDetails {
	return GAME_RULES[gameTypeId] || GAME_RULES['watl_standard'];
}
