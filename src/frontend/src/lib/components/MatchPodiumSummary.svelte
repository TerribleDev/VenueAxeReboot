<script lang="ts">
	import WatlTarget from './WatlTarget.svelte';
	import type { GameStateSnapshot, GamePlayer } from '$lib/api/generated/types.gen';

	interface Props {
		gameState: GameStateSnapshot;
		targetType?: 'watl' | 'iatf';
		onrematch?: () => void;
		onundo?: () => void;
		onswitchformat?: () => void;
	}

	let { gameState, targetType = 'watl', onrematch, onundo, onswitchformat }: Props = $props();

	let selectedPlayerFilter = $state<string>('all');

	// Sort players by score descending
	let rankedPlayers = $derived(
		[...(gameState.players ?? [])].sort((a, b) => Number(b.score ?? 0) - Number(a.score ?? 0))
	);

	let winner = $derived(rankedPlayers[0]);

	// Color palette for scatter pins
	const playerColors = ['#f59e0b', '#06b6d4', '#ec4899', '#10b981', '#8b5cf6', '#ef4444'];

	let scatterPins = $derived(
		(gameState.allThrows ?? [])
			.filter((t) => selectedPlayerFilter === 'all' || t.playerId === selectedPlayerFilter)
			.map((t) => {
				const playerIdx = (gameState.players ?? []).findIndex((p) => p.id === t.playerId);
				const color = playerIdx >= 0 ? playerColors[playerIdx % playerColors.length] : '#f59e0b';
				return {
					x: t.x != null ? Number(t.x) : null,
					y: t.y != null ? Number(t.y) : null,
					pointsAwarded: t.pointsAwarded != null ? Number(t.pointsAwarded) : undefined,
					playerName: t.playerName,
					color
				};
			})
	);

	function getAccuracy(player: GamePlayer): number {
		const throws = Number(player.throwsTaken ?? 0);
		if (throws === 0) return 0;
		return Math.round((Number(player.bullseyesHit ?? 0) / throws) * 100);
	}

	function getAvgPerThrow(player: GamePlayer): string {
		const throws = Number(player.throwsTaken ?? 0);
		if (throws === 0) return '0.0';
		return (Number(player.score ?? 0) / throws).toFixed(1);
	}
</script>

<div class="podium-container glass-panel">
	<!-- Top Celebration Banner -->
	<div class="podium-header">
		<div class="trophy-badge">🏆</div>
		<h1 class="winner-title font-display">
			{winner ? `${winner.name} Wins!` : 'Match Finished!'}
		</h1>
		<p class="winner-subtitle">
			{gameState.gameName ?? 'Regulation Axe Match'} • Final Results
		</p>
	</div>

	<!-- Podium Stage (Top 3 Players) -->
	<div class="podium-stage">
		{#if rankedPlayers.length > 1}
			<!-- 2nd Place Stand (Silver) -->
			<div class="podium-column rank-2">
				<div class="podium-avatar silver">🥈</div>
				<div class="podium-player-name">{rankedPlayers[1].name}</div>
				<div class="podium-score font-display">{rankedPlayers[1].score} pts</div>
				<div class="podium-bar silver-bar">
					<span class="rank-label">2ND</span>
				</div>
			</div>
		{/if}

		{#if rankedPlayers.length > 0}
			<!-- 1st Place Stand (Gold) -->
			<div class="podium-column rank-1">
				<div class="crown-icon">👑</div>
				<div class="podium-avatar gold">🥇</div>
				<div class="podium-player-name winner-glow">{rankedPlayers[0].name}</div>
				<div class="podium-score font-display gold-text">{rankedPlayers[0].score} pts</div>
				<div class="podium-bar gold-bar">
					<span class="rank-label">1ST</span>
				</div>
			</div>
		{/if}

		{#if rankedPlayers.length > 2}
			<!-- 3rd Place Stand (Bronze) -->
			<div class="podium-column rank-3">
				<div class="podium-avatar bronze">🥉</div>
				<div class="podium-player-name">{rankedPlayers[2].name}</div>
				<div class="podium-score font-display">{rankedPlayers[2].score} pts</div>
				<div class="podium-bar bronze-bar">
					<span class="rank-label">3RD</span>
				</div>
			</div>
		{/if}
	</div>

	<!-- Detailed Stats & Scatter Visualizer Grid -->
	<div class="details-grid">
		<!-- Left: Accuracy Leaderboard Cards -->
		<div class="stats-card glass-panel">
			<h2 class="card-title font-display">Thrower Statistics</h2>
			<div class="player-stats-list">
				{#each rankedPlayers as player, index (player.id)}
					<div class="player-stat-row">
						<div class="row-rank font-display">#{index + 1}</div>
						<div class="row-info">
							<div class="row-name">{player.name}</div>
							<div class="row-sub">
								Bullseye: <strong class="highlight">{getAccuracy(player)}%</strong> ({player.bullseyesHit}/{player.throwsTaken})
								• Clutches: <strong class="highlight-cyan">{player.clutchesHit ?? 0}</strong>
								• Avg: <strong class="highlight">{getAvgPerThrow(player)} pts</strong>
							</div>
						</div>
						<div class="row-score font-display">{player.score}</div>
					</div>
				{/each}
			</div>
		</div>

		<!-- Right: Target Heatmap Scatter Plot -->
		<div class="heatmap-card glass-panel">
			<div class="heatmap-header">
				<h2 class="card-title font-display">Axe Throw Scatter Heatmap</h2>
				<!-- Filter by Player -->
				<div class="filter-pills">
					<button
						class="filter-pill"
						class:active={selectedPlayerFilter === 'all'}
						onclick={() => (selectedPlayerFilter = 'all')}
					>
						All Throws ({gameState.allThrows?.length ?? 0})
					</button>
					{#each rankedPlayers as player}
						<button
							class="filter-pill"
							class:active={selectedPlayerFilter === player.id}
							onclick={() => (selectedPlayerFilter = player.id ?? 'all')}
						>
							{player.name}
						</button>
					{/each}
				</div>
			</div>

			<div class="target-scatter-wrapper">
				<WatlTarget
					interactive={false}
					{targetType}
					scatterThrows={scatterPins}
				/>
			</div>
		</div>
	</div>

	<!-- Action Footer -->
	<div class="podium-actions">
		{#if onrematch}
			<button class="btn btn-primary btn-rematch font-display" onclick={onrematch}>
				🔄 Rematch (Same Throwers)
			</button>
		{/if}
		{#if onundo}
			<button class="btn btn-secondary font-display" onclick={onundo}>
				↩️ Undo Final Throw
			</button>
		{/if}
		{#if onswitchformat}
			<button class="btn btn-outline font-display" onclick={onswitchformat}>
				🎯 Switch Game Mode
			</button>
		{/if}
	</div>
</div>

<style>
	.podium-container {
		display: flex;
		flex-direction: column;
		gap: 1.5rem;
		padding: 2rem;
		border-radius: var(--radius-lg);
		background: radial-gradient(circle at top, rgba(245, 158, 11, 0.12) 0%, rgba(17, 24, 39, 0.95) 70%);
		border: 1px solid rgba(245, 158, 11, 0.25);
		box-shadow: 0 20px 40px rgba(0, 0, 0, 0.6);
		animation: fadeIn 0.4s ease-out;
	}

	@keyframes fadeIn {
		from { opacity: 0; transform: translateY(12px); }
		to { opacity: 1; transform: translateY(0); }
	}

	.podium-header {
		text-align: center;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.35rem;
	}

	.trophy-badge {
		font-size: 2.5rem;
		animation: bounce 1s infinite alternate;
	}

	@keyframes bounce {
		0% { transform: translateY(0); }
		100% { transform: translateY(-8px); }
	}

	.winner-title {
		font-size: 2.2rem;
		font-weight: 800;
		color: #ffffff;
		letter-spacing: 0.05em;
		text-transform: uppercase;
		text-shadow: 0 0 20px rgba(245, 158, 11, 0.6);
	}

	.winner-subtitle {
		font-size: 1rem;
		color: var(--color-text-muted);
		text-transform: uppercase;
		letter-spacing: 0.1em;
	}

	/* Podium Stands */
	.podium-stage {
		display: flex;
		align-items: flex-end;
		justify-content: center;
		gap: 1rem;
		padding: 1rem 0;
	}

	.podium-column {
		display: flex;
		flex-direction: column;
		align-items: center;
		width: 140px;
	}

	.crown-icon {
		font-size: 1.5rem;
		margin-bottom: -6px;
	}

	.podium-avatar {
		width: 48px;
		height: 48px;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 1.4rem;
		margin-bottom: 0.5rem;
	}

	.podium-avatar.gold {
		background: linear-gradient(135deg, #f59e0b, #d97706);
		box-shadow: 0 0 15px rgba(245, 158, 11, 0.6);
	}

	.podium-avatar.silver {
		background: linear-gradient(135deg, #94a3b8, #64748b);
		box-shadow: 0 0 12px rgba(148, 163, 184, 0.4);
	}

	.podium-avatar.bronze {
		background: linear-gradient(135deg, #b45309, #78350f);
		box-shadow: 0 0 10px rgba(180, 83, 9, 0.3);
	}

	.podium-player-name {
		font-size: 1rem;
		font-weight: 700;
		color: #ffffff;
		text-align: center;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		max-width: 130px;
	}

	.winner-glow {
		color: #fbbf24;
		text-shadow: 0 0 10px rgba(251, 191, 36, 0.5);
	}

	.podium-score {
		font-size: 0.95rem;
		font-weight: 700;
		color: var(--color-text-muted);
		margin-bottom: 0.5rem;
	}

	.gold-text {
		color: #f59e0b;
	}

	.podium-bar {
		width: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 8px 8px 0 0;
	}

	.gold-bar {
		height: 120px;
		background: linear-gradient(180deg, rgba(245, 158, 11, 0.5) 0%, rgba(245, 158, 11, 0.15) 100%);
		border: 1px solid rgba(245, 158, 11, 0.5);
	}

	.silver-bar {
		height: 85px;
		background: linear-gradient(180deg, rgba(148, 163, 184, 0.4) 0%, rgba(148, 163, 184, 0.1) 100%);
		border: 1px solid rgba(148, 163, 184, 0.4);
	}

	.bronze-bar {
		height: 60px;
		background: linear-gradient(180deg, rgba(180, 83, 9, 0.4) 0%, rgba(180, 83, 9, 0.1) 100%);
		border: 1px solid rgba(180, 83, 9, 0.3);
	}

	.rank-label {
		font-family: 'Chakra Petch', sans-serif;
		font-weight: 900;
		font-size: 1.2rem;
		color: #ffffff;
		letter-spacing: 0.05em;
	}

	/* Details Grid */
	.details-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1.5rem;
	}

	@media (max-width: 860px) {
		.details-grid {
			grid-template-columns: 1fr;
		}
	}

	.stats-card, .heatmap-card {
		padding: 1.5rem;
		border-radius: var(--radius-md);
		border: 1px solid rgba(255, 255, 255, 0.08);
		background: rgba(17, 24, 39, 0.7);
	}

	.card-title {
		font-size: 1.15rem;
		font-weight: 700;
		color: #ffffff;
		margin-bottom: 1rem;
		letter-spacing: 0.03em;
		text-transform: uppercase;
	}

	.player-stats-list {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
	}

	.player-stat-row {
		display: flex;
		align-items: center;
		gap: 1rem;
		padding: 0.75rem 1rem;
		background: rgba(255, 255, 255, 0.04);
		border-radius: var(--radius-sm);
		border: 1px solid rgba(255, 255, 255, 0.05);
	}

	.row-rank {
		font-size: 1.1rem;
		font-weight: 800;
		color: #94a3b8;
		min-width: 32px;
	}

	.row-info {
		flex: 1;
	}

	.row-name {
		font-size: 1.05rem;
		font-weight: 700;
		color: #ffffff;
	}

	.row-sub {
		font-size: 0.8rem;
		color: var(--color-text-muted);
		margin-top: 0.15rem;
	}

	.highlight {
		color: #f59e0b;
	}

	.highlight-cyan {
		color: #06b6d4;
	}

	.row-score {
		font-size: 1.5rem;
		font-weight: 800;
		color: #f59e0b;
	}

	/* Heatmap Card */
	.heatmap-header {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		margin-bottom: 1rem;
	}

	.filter-pills {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
	}

	.filter-pill {
		padding: 0.3rem 0.75rem;
		border-radius: 9999px;
		font-size: 0.8rem;
		font-weight: 600;
		background: rgba(255, 255, 255, 0.05);
		color: var(--color-text-muted);
		border: 1px solid rgba(255, 255, 255, 0.1);
		cursor: pointer;
		transition: all 0.2s ease;
	}

	.filter-pill:hover {
		background: rgba(255, 255, 255, 0.12);
		color: #ffffff;
	}

	.filter-pill.active {
		background: #f59e0b;
		color: #000000;
		border-color: #f59e0b;
		font-weight: 700;
	}

	.target-scatter-wrapper {
		max-width: 340px;
		margin: 0 auto;
	}

	/* Actions */
	.podium-actions {
		display: flex;
		flex-wrap: wrap;
		justify-content: center;
		gap: 1rem;
		padding-top: 1rem;
	}

	.btn-rematch {
		background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
		font-size: 1.05rem;
		padding: 0.75rem 2rem;
		box-shadow: 0 4px 15px rgba(245, 158, 11, 0.4);
	}

	.btn-rematch:hover {
		transform: translateY(-2px);
		box-shadow: 0 6px 20px rgba(245, 158, 11, 0.6);
	}
</style>
