<script lang="ts">
	import WatlTarget from './WatlTarget.svelte';
	import type { GameStateSnapshot, GamePlayer } from '$lib/api/generated/types.gen';

	interface Props {
		gameState: GameStateSnapshot;
		onrematch?: () => void;
		onundo?: () => void;
		onswitchformat?: () => void;
	}

	let { gameState, onrematch, onundo, onswitchformat }: Props = $props();

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
		<div class="header-main">
			<span class="trophy-badge">🏆</span>
			<h1 class="winner-title font-display">
				{winner ? `${winner.name} Wins!` : 'Match Finished!'}
			</h1>
		</div>
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
				<h2 class="card-title font-display">Throw Scatter Heatmap</h2>
				<!-- Filter by Player -->
				<div class="filter-pills">
					<button
						class="filter-pill"
						class:active={selectedPlayerFilter === 'all'}
						onclick={() => (selectedPlayerFilter = 'all')}
					>
						All ({gameState.allThrows?.length ?? 0})
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
					scatterThrows={scatterPins}
				/>
			</div>
		</div>
	</div>

	<!-- Action Footer -->
	<div class="podium-actions">
		{#if onrematch}
			<button class="btn btn-primary btn-rematch font-display" onclick={onrematch}>
				🔄 Rematch
			</button>
		{/if}
		{#if onundo}
			<button class="btn btn-secondary btn-undo font-display" onclick={onundo}>
				↩️ Undo Throw
			</button>
		{/if}
		{#if onswitchformat}
			<button class="btn btn-outline btn-switch font-display" onclick={onswitchformat}>
				🎯 Switch Game
			</button>
		{/if}
	</div>
</div>

<style>
	.podium-container {
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
		padding: 1.5rem;
		border-radius: var(--radius-lg);
		background: radial-gradient(circle at top, rgba(245, 158, 11, 0.12) 0%, rgba(17, 24, 39, 0.95) 70%);
		border: 1px solid rgba(245, 158, 11, 0.25);
		box-shadow: 0 16px 36px rgba(0, 0, 0, 0.5);
		animation: fadeIn 0.35s ease-out;
		width: 100%;
		box-sizing: border-box;
	}

	@keyframes fadeIn {
		from { opacity: 0; transform: translateY(8px); }
		to { opacity: 1; transform: translateY(0); }
	}

	.podium-header {
		text-align: center;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.25rem;
	}

	.header-main {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.5rem;
	}

	.trophy-badge {
		font-size: 2rem;
		animation: bounce 1s infinite alternate;
		line-height: 1;
	}

	@keyframes bounce {
		0% { transform: translateY(0); }
		100% { transform: translateY(-5px); }
	}

	.winner-title {
		font-size: 1.85rem;
		font-weight: 800;
		color: #ffffff;
		letter-spacing: 0.04em;
		text-transform: uppercase;
		text-shadow: 0 0 16px rgba(245, 158, 11, 0.5);
		margin: 0;
	}

	.winner-subtitle {
		font-size: 0.9rem;
		color: var(--color-text-muted);
		text-transform: uppercase;
		letter-spacing: 0.08em;
		margin: 0;
	}

	/* Podium Stands */
	.podium-stage {
		display: flex;
		align-items: flex-end;
		justify-content: center;
		gap: 0.75rem;
		padding: 0.5rem 0;
	}

	.podium-column {
		display: flex;
		flex-direction: column;
		align-items: center;
		width: 120px;
	}

	.crown-icon {
		font-size: 1.25rem;
		margin-bottom: -4px;
		line-height: 1;
	}

	.podium-avatar {
		width: 40px;
		height: 40px;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 1.2rem;
		margin-bottom: 0.35rem;
	}

	.podium-avatar.gold {
		background: linear-gradient(135deg, #f59e0b, #d97706);
		box-shadow: 0 0 14px rgba(245, 158, 11, 0.5);
	}

	.podium-avatar.silver {
		background: linear-gradient(135deg, #94a3b8, #64748b);
		box-shadow: 0 0 10px rgba(148, 163, 184, 0.35);
	}

	.podium-avatar.bronze {
		background: linear-gradient(135deg, #b45309, #78350f);
		box-shadow: 0 0 8px rgba(180, 83, 9, 0.3);
	}

	.podium-player-name {
		font-size: 0.95rem;
		font-weight: 700;
		color: #ffffff;
		text-align: center;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		max-width: 115px;
	}

	.winner-glow {
		color: #fbbf24;
		text-shadow: 0 0 8px rgba(251, 191, 36, 0.4);
	}

	.podium-score {
		font-size: 0.9rem;
		font-weight: 700;
		color: var(--color-text-muted);
		margin-bottom: 0.35rem;
	}

	.gold-text {
		color: #f59e0b;
	}

	.podium-bar {
		width: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 6px 6px 0 0;
	}

	.gold-bar {
		height: 90px;
		background: linear-gradient(180deg, rgba(245, 158, 11, 0.5) 0%, rgba(245, 158, 11, 0.15) 100%);
		border: 1px solid rgba(245, 158, 11, 0.5);
	}

	.silver-bar {
		height: 65px;
		background: linear-gradient(180deg, rgba(148, 163, 184, 0.4) 0%, rgba(148, 163, 184, 0.1) 100%);
		border: 1px solid rgba(148, 163, 184, 0.4);
	}

	.bronze-bar {
		height: 45px;
		background: linear-gradient(180deg, rgba(180, 83, 9, 0.4) 0%, rgba(180, 83, 9, 0.1) 100%);
		border: 1px solid rgba(180, 83, 9, 0.3);
	}

	.rank-label {
		font-family: 'Chakra Petch', sans-serif;
		font-weight: 900;
		font-size: 1.05rem;
		color: #ffffff;
		letter-spacing: 0.05em;
	}

	/* Details Grid */
	.details-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1rem;
		align-items: stretch;
	}

	@media (max-width: 768px) {
		.details-grid {
			grid-template-columns: 1fr;
		}
	}

	.stats-card, .heatmap-card {
		padding: 1rem;
		border-radius: var(--radius-md);
		border: 1px solid rgba(255, 255, 255, 0.08);
		background: rgba(17, 24, 39, 0.7);
		display: flex;
		flex-direction: column;
	}

	.card-title {
		font-size: 1rem;
		font-weight: 700;
		color: #ffffff;
		margin: 0 0 0.65rem 0;
		letter-spacing: 0.03em;
		text-transform: uppercase;
	}

	.player-stats-list {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		flex: 1;
	}

	.player-stat-row {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		padding: 0.5rem 0.75rem;
		background: rgba(255, 255, 255, 0.04);
		border-radius: var(--radius-sm);
		border: 1px solid rgba(255, 255, 255, 0.05);
	}

	.row-rank {
		font-size: 1rem;
		font-weight: 800;
		color: #94a3b8;
		min-width: 26px;
	}

	.row-info {
		flex: 1;
		min-width: 0;
	}

	.row-name {
		font-size: 0.95rem;
		font-weight: 700;
		color: #ffffff;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.row-sub {
		font-size: 0.76rem;
		color: var(--color-text-muted);
		margin-top: 0.1rem;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.highlight {
		color: #f59e0b;
	}

	.highlight-cyan {
		color: #06b6d4;
	}

	.row-score {
		font-size: 1.35rem;
		font-weight: 800;
		color: #f59e0b;
	}

	/* Heatmap Card */
	.heatmap-header {
		display: flex;
		flex-direction: column;
		gap: 0.35rem;
		margin-bottom: 0.5rem;
	}

	.filter-pills {
		display: flex;
		flex-wrap: wrap;
		gap: 0.35rem;
	}

	.filter-pill {
		padding: 0.2rem 0.6rem;
		border-radius: 9999px;
		font-size: 0.75rem;
		font-weight: 600;
		background: rgba(255, 255, 255, 0.05);
		color: var(--color-text-muted);
		border: 1px solid rgba(255, 255, 255, 0.1);
		cursor: pointer;
		transition: all 0.15s ease;
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
		width: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		margin: auto 0;
		--target-max-size: 260px;
		--target-max-height: 260px;
	}

	/* Actions */
	.podium-actions {
		display: flex;
		flex-wrap: wrap;
		justify-content: center;
		gap: 0.75rem;
		padding-top: 0.5rem;
	}

	.btn-rematch {
		background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
		font-size: 0.95rem;
		padding: 0.6rem 1.5rem;
		box-shadow: 0 4px 12px rgba(245, 158, 11, 0.35);
	}

	.btn-rematch:hover {
		transform: translateY(-1px);
		box-shadow: 0 6px 16px rgba(245, 158, 11, 0.5);
	}

	.btn-undo, .btn-switch {
		font-size: 0.95rem;
		padding: 0.6rem 1.25rem;
	}

	/* =========================================================================
	   TABLET OPTIMIZATION: Mid-size Android Tablets (e.g. 1280x800 & 1024x768)
	   Height <= 820px
	   ========================================================================= */
	@media (max-height: 820px) {
		.podium-container {
			gap: 0.65rem;
			padding: 0.75rem 1rem;
		}

		.podium-header {
			gap: 0.15rem;
		}

		.trophy-badge {
			font-size: 1.5rem;
		}

		.winner-title {
			font-size: 1.35rem;
		}

		.winner-subtitle {
			font-size: 0.78rem;
		}

		.podium-stage {
			padding: 0.2rem 0;
			gap: 0.5rem;
		}

		.podium-column {
			width: 105px;
		}

		.crown-icon {
			font-size: 1rem;
			margin-bottom: -3px;
		}

		.podium-avatar {
			width: 34px;
			height: 34px;
			font-size: 1rem;
			margin-bottom: 0.2rem;
		}

		.podium-player-name {
			font-size: 0.85rem;
			max-width: 100px;
		}

		.podium-score {
			font-size: 0.8rem;
			margin-bottom: 0.2rem;
		}

		.gold-bar {
			height: 55px;
		}

		.silver-bar {
			height: 40px;
		}

		.bronze-bar {
			height: 28px;
		}

		.rank-label {
			font-size: 0.85rem;
		}

		.details-grid {
			gap: 0.65rem;
		}

		.stats-card, .heatmap-card {
			padding: 0.55rem 0.75rem;
		}

		.card-title {
			font-size: 0.85rem;
			margin-bottom: 0.35rem;
		}

		.player-stats-list {
			gap: 0.35rem;
		}

		.player-stat-row {
			padding: 0.3rem 0.55rem;
			gap: 0.5rem;
		}

		.row-rank {
			font-size: 0.85rem;
			min-width: 22px;
		}

		.row-name {
			font-size: 0.85rem;
		}

		.row-sub {
			font-size: 0.7rem;
		}

		.row-score {
			font-size: 1.15rem;
		}

		.heatmap-header {
			gap: 0.25rem;
			margin-bottom: 0.35rem;
		}

		.filter-pills {
			gap: 0.25rem;
		}

		.filter-pill {
			padding: 0.15rem 0.45rem;
			font-size: 0.7rem;
		}

		.target-scatter-wrapper {
			--target-max-size: min(160px, 23vh);
			--target-max-height: min(160px, 23vh);
		}

		.podium-actions {
			gap: 0.5rem;
			padding-top: 0.35rem;
		}

		.btn-rematch, .btn-undo, .btn-switch {
			padding: 0.45rem 1.1rem;
			font-size: 0.85rem;
			min-height: 40px;
		}
	}

	/* =========================================================================
	   EXTRA COMPACT TABLETS: Low-res & Short Viewports (e.g. 1024x600 Androids)
	   Height <= 650px
	   ========================================================================= */
	@media (max-height: 650px) {
		.podium-container {
			gap: 0.45rem;
			padding: 0.45rem 0.75rem;
		}

		.podium-header {
			flex-direction: row;
			justify-content: center;
			align-items: baseline;
			gap: 0.5rem;
		}

		.trophy-badge {
			font-size: 1.25rem;
		}

		.winner-title {
			font-size: 1.15rem;
		}

		.winner-subtitle {
			font-size: 0.72rem;
		}

		.podium-stage {
			padding: 0;
			gap: 0.4rem;
		}

		.podium-column {
			width: 90px;
		}

		.crown-icon {
			font-size: 0.9rem;
			margin-bottom: -2px;
		}

		.podium-avatar {
			width: 28px;
			height: 28px;
			font-size: 0.85rem;
			margin-bottom: 0.15rem;
		}

		.podium-player-name {
			font-size: 0.78rem;
			max-width: 85px;
		}

		.podium-score {
			font-size: 0.75rem;
			margin-bottom: 0.15rem;
		}

		.gold-bar {
			height: 42px;
		}

		.silver-bar {
			height: 30px;
		}

		.bronze-bar {
			height: 20px;
		}

		.rank-label {
			font-size: 0.75rem;
		}

		.details-grid {
			gap: 0.5rem;
		}

		.stats-card, .heatmap-card {
			padding: 0.4rem 0.6rem;
		}

		.card-title {
			font-size: 0.78rem;
			margin-bottom: 0.25rem;
		}

		.player-stats-list {
			gap: 0.25rem;
		}

		.player-stat-row {
			padding: 0.25rem 0.45rem;
			gap: 0.4rem;
		}

		.row-rank {
			font-size: 0.8rem;
			min-width: 18px;
		}

		.row-name {
			font-size: 0.8rem;
		}

		.row-sub {
			font-size: 0.68rem;
		}

		.row-score {
			font-size: 1rem;
		}

		.target-scatter-wrapper {
			--target-max-size: min(130px, 21vh);
			--target-max-height: min(130px, 21vh);
		}

		.podium-actions {
			gap: 0.4rem;
			padding-top: 0.25rem;
		}

		.btn-rematch, .btn-undo, .btn-switch {
			padding: 0.35rem 0.9rem;
			font-size: 0.8rem;
			min-height: 38px;
		}
	}

	/* =========================================================================
	   BROADCAST SCREEN / LARGE DISPLAY (Overhead TV 1920x1080)
	   Height >= 850px and Width >= 1200px
	   ========================================================================= */
	@media (min-height: 850px) and (min-width: 1200px) {
		.podium-container {
			gap: 1.5rem;
			padding: 2rem;
		}

		.trophy-badge {
			font-size: 2.75rem;
		}

		.winner-title {
			font-size: 2.35rem;
		}

		.winner-subtitle {
			font-size: 1.05rem;
		}

		.podium-stage {
			gap: 1.25rem;
			padding: 1.25rem 0;
		}

		.podium-column {
			width: 150px;
		}

		.crown-icon {
			font-size: 1.6rem;
		}

		.podium-avatar {
			width: 52px;
			height: 52px;
			font-size: 1.5rem;
			margin-bottom: 0.6rem;
		}

		.podium-player-name {
			font-size: 1.1rem;
			max-width: 140px;
		}

		.podium-score {
			font-size: 1rem;
			margin-bottom: 0.6rem;
		}

		.gold-bar {
			height: 125px;
		}

		.silver-bar {
			height: 90px;
		}

		.bronze-bar {
			height: 65px;
		}

		.rank-label {
			font-size: 1.25rem;
		}

		.details-grid {
			gap: 1.5rem;
		}

		.stats-card, .heatmap-card {
			padding: 1.5rem;
		}

		.card-title {
			font-size: 1.2rem;
			margin-bottom: 1rem;
		}

		.player-stats-list {
			gap: 0.75rem;
		}

		.player-stat-row {
			padding: 0.75rem 1rem;
			gap: 1rem;
		}

		.row-name {
			font-size: 1.1rem;
		}

		.row-sub {
			font-size: 0.85rem;
		}

		.row-score {
			font-size: 1.6rem;
		}

		.target-scatter-wrapper {
			--target-max-size: 340px;
			--target-max-height: 340px;
		}

		.btn-rematch {
			font-size: 1.1rem;
			padding: 0.8rem 2.2rem;
		}
	}
</style>
