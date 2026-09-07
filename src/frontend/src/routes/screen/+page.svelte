<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import WatlTarget from '$lib/components/WatlTarget.svelte';
	import { postApiLanesTerminalsPair } from '$lib/api/client';
	import { laneSignalR } from '$lib/services/signalr';
	import type { GameStateSnapshot, TerminalAuthResult } from '$lib/api/generated/types.gen';

	let pairingCode = $state('TV101');
	let terminalAuth = $state<TerminalAuthResult | null>(null);
	let gameState = $state<GameStateSnapshot | null>(null);
	let isPairing = $state(false);
	let showBullseyeCelebration = $state(false);
	let showClutchCelebration = $state(false);
	let clutchAlert = $state<string | null>(null);

	onMount(async () => {
		const saved = localStorage.getItem('venueaxe_screen_auth');
		if (saved) {
			try {
				terminalAuth = JSON.parse(saved);
				if (terminalAuth) {
					await initSignalR(terminalAuth.laneId);
					await loadActiveSession(terminalAuth.laneId);
				}
			} catch (e) {
				// ignore
			}
		}
	});

	onDestroy(() => {
		laneSignalR.disconnect();
	});

	async function loadActiveSession(laneId: string) {
		try {
			const res = await fetch(`/api/lanes/operations/${laneId}/active-session`, {
				credentials: 'include'
			});
			if (res.ok) {
				const session = await res.json();
				if (session && session.currentGame) {
					gameState = session.currentGame;
				}
			}
		} catch (e) {
			console.error('Failed to load active session:', e);
		}
	}

	function resetPairing() {
		localStorage.removeItem('venueaxe_screen_auth');
		terminalAuth = null;
		gameState = null;
		laneSignalR.disconnect();
	}

	async function handlePair() {
		isPairing = true;
		try {
			const res = await postApiLanesTerminalsPair({
				body: { pairingCode, terminalType: 'Screen' }
			});
			if (res.data) {
				terminalAuth = res.data;
				localStorage.setItem('venueaxe_screen_auth', JSON.stringify(terminalAuth));
				await initSignalR(terminalAuth.laneId);
				await loadActiveSession(terminalAuth.laneId);
			} else {
				alert('Invalid TV Pairing PIN');
			}
		} catch (e) {
			alert('Failed to connect TV screen');
		} finally {
			isPairing = false;
		}
	}

	async function initSignalR(laneId: string) {
		laneSignalR.onThrowRecorded = (state) => {
			gameState = state;
			if (state.lastThrow?.isBullseye) {
				triggerBullseyeVFX();
			} else if (state.lastThrow?.isClutchCalled && (state.lastThrow.pointsAwarded === 8 || state.lastThrow.pointsAwarded === 7)) {
				triggerSpecialVFX();
			}
		};

		laneSignalR.onClutchCalled = (playerId, side) => {
			const label = gameState?.gameTypeId === 'iatf_standard' ? 'CLUTCH' : 'KILLSHOT';
			clutchAlert = `⚡ ${label} CALLED (${side.toUpperCase()}) ⚡`;
			setTimeout(() => (clutchAlert = null), 6000);
		};

		try {
			await laneSignalR.connect(laneId);
		} catch (e) {
			console.error(e);
		}
	}

	function triggerBullseyeVFX() {
		showBullseyeCelebration = true;
		setTimeout(() => (showBullseyeCelebration = false), 3500);
	}

	function triggerSpecialVFX() {
		showClutchCelebration = true;
		setTimeout(() => (showClutchCelebration = false), 4000);
	}
</script>

<div class="screen-viewport">
	{#if !terminalAuth}
		<!-- TV PAIRING SCREEN -->
		<div class="tv-pair-card glass-panel">
			<span class="tv-icon">📺</span>
			<h1 class="font-display tv-title">Overhead Lane Monitor</h1>
			<p class="tv-subtitle">Enter the TV Display PIN to connect this screen.</p>
			<input type="text" class="form-input tv-pin-input font-display" bind:value={pairingCode} placeholder="TV101" />
			<button class="btn btn-primary btn-lg font-display" disabled={isPairing} onclick={handlePair}>
				{isPairing ? 'Connecting...' : 'Connect Overhead TV Display'}
			</button>
		</div>
	{:else}
		<!-- BROADCAST TV INTERFACE -->
		<div class="broadcast-container">
			<!-- Header Broadcast Bar -->
			<div class="broadcast-header">
				<div class="brand-zone">
					<span class="axe-icon">🪓</span>
					<span class="lane-title font-display">{terminalAuth.laneName}</span>
				</div>

				{#if gameState}
					<div class="match-center">
						<span class="match-mode font-display">{gameState.gameName}</span>
						<span class="round-badge font-display">ROUND {gameState.currentRound} OF {gameState.totalRounds}</span>
					</div>
				{/if}

				<div class="sponsor-zone">
					<span class="live-tag">● LIVE SCORING</span>
					<button class="btn-unpair font-display" onclick={resetPairing} title="Unpair TV Screen">
						Unpair
					</button>
				</div>
			</div>

			<!-- Clutch Alert Banner -->
			{#if clutchAlert}
				<div class="clutch-broadcast-banner font-display">
					{clutchAlert}
				</div>
			{/if}

			{#if gameState && gameState.players && gameState.players.length > 0 && gameState.currentPlayerIndex !== undefined}
				{@const players = gameState.players}
				{@const activePlayer = players[Number(gameState.currentPlayerIndex)]}
				{@const sortedLeaderboard = [...players].sort((a, b) => Number(b.score) - Number(a.score))}

				<div class="tv-main-grid">
					<!-- Left: Active Thrower Card & Live Target Visualizer -->
					<div class="tv-left-column">
						<!-- Thrower Showcase -->
						<div class="thrower-showcase glass-panel">
							<div class="showcase-header">
								<span class="badge badge-active">CURRENT THROWER</span>
								<span class="streak-flame font-display">
									{Number(activePlayer.streak || 0) > 0 ? `🔥 ${activePlayer.streak} IN A ROW` : ''}
								</span>
							</div>

							<div class="thrower-profile">
								<div class="tv-avatar" style="background-color: {activePlayer.avatarColor}">
									{(activePlayer.name || 'P').charAt(0)}
								</div>
								<div class="tv-name-box">
									<h2 class="tv-thrower-name font-display">{activePlayer.name || 'Thrower'}</h2>
									<span class="tv-throw-count">Throws: {activePlayer.throwsTaken ?? 0}</span>
								</div>
								<div class="tv-score-box">
									<span class="tv-score-val font-display">{activePlayer.score}</span>
									<span class="tv-score-lbl">TOTAL PTS</span>
								</div>
							</div>
						</div>

						<!-- Target Hit Visualizer -->
						<div class="tv-target-card glass-panel">
							<WatlTarget
								interactive={false}
								lastThrow={gameState.lastThrow as any}
							/>
						</div>
					</div>

					<!-- Right: Broadcast Scoreboard Leaderboard -->
					<div class="tv-right-column glass-panel">
						<div class="board-header">
							<h3 class="board-title font-display">LEADERBOARD</h3>
							<span class="board-sub">Official Target Match Rankings</span>
						</div>

						<div class="leaderboard-list">
							{#each sortedLeaderboard as p, i (p.id)}
								<div class="leaderboard-row" class:row-active={p.id === activePlayer.id}>
									<div class="rank-col font-display">#{i + 1}</div>
									<div class="name-col">
										<span class="p-dot" style="background-color: {p.avatarColor}"></span>
										<span class="p-name font-display">{p.name}</span>
									</div>
									<div class="stats-col">
										<span class="bull-stat">🎯 {p.bullseyesHit} Bulls</span>
									</div>
									<div class="score-col font-display">{p.score}</div>
								</div>
							{/each}
						</div>

						{#if Number(gameState.status) === 2 || (gameState.status as any) === 'Finished'}
							<div class="winner-celebration-card">
								<span class="trophy">🏆</span>
								<div>
									<h2 class="font-display">MATCH WINNER</h2>
									<p class="winner-name font-display">{gameState.winnerName} takes 1st place!</p>
								</div>
							</div>
						{/if}
					</div>
				</div>
			{:else}
				<div class="tv-attract-loop glass-panel">
					<span class="big-axe">🪓</span>
					<h1 class="font-display attract-title">WELCOME TO {terminalAuth.laneName}</h1>
					<p class="attract-sub">Axe Coach will launch your match shortly. Prepare to throw!</p>
				</div>
			{/if}
		</div>

		<!-- Bullseye Celebration VFX Modal -->
		{#if showBullseyeCelebration}
			<div class="vfx-overlay bullseye-vfx">
				<div class="vfx-card">
					<h1 class="vfx-title font-display">🎯 BULLSEYE!</h1>
					<p class="vfx-points font-display">+6 POINTS</p>
				</div>
			</div>
		{/if}

		<!-- Special Hit Celebration VFX Modal (Killshot / Clutch) -->
		{#if showClutchCelebration}
			{@const isIatf = gameState?.gameTypeId === 'iatf_standard'}
			<div class="vfx-overlay clutch-vfx">
				<div class="vfx-card">
					<h1 class="vfx-title font-display">⚡ {isIatf ? 'CLUTCH NAILED!' : 'KILLSHOT NAILED!'} ⚡</h1>
					<p class="vfx-points font-display">+{isIatf ? '7' : '8'} POINTS</p>
				</div>
			</div>
		{/if}
	{/if}
</div>

<style>
	.screen-viewport {
		min-height: 100vh;
		background: #06080c;
		color: #f8fafc;
		display: flex;
		flex-direction: column;
	}

	.tv-pair-card {
		max-width: 540px;
		margin: 6rem auto;
		padding: 3rem;
		text-align: center;
	}

	.tv-icon { font-size: 3rem; margin-bottom: 1rem; display: block; }
	.tv-title { font-size: 2rem; font-weight: 800; margin-bottom: 0.5rem; }
	.tv-subtitle { color: var(--text-secondary); margin-bottom: 2rem; }
	.tv-pin-input { font-size: 2rem; text-align: center; letter-spacing: 0.2em; font-weight: 900; color: var(--accent-amber); margin-bottom: 1.5rem; }

	.broadcast-container {
		display: flex;
		flex-direction: column;
		height: 100vh;
		padding: 1.25rem 2rem 2rem;
		gap: 1.25rem;
	}

	.broadcast-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		background: #10141d;
		border: 2px solid var(--border-color);
		border-radius: var(--radius-lg);
		padding: 0.75rem 2rem;
	}

	.brand-zone {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}

	.axe-icon { font-size: 2rem; }
	.lane-title { font-size: 2rem; font-weight: 900; color: var(--accent-amber); }

	.match-center {
		display: flex;
		align-items: center;
		gap: 1.5rem;
	}

	.match-mode { font-size: 1.4rem; font-weight: 800; }
	.round-badge {
		background: rgba(245, 158, 11, 0.2);
		border: 1px solid var(--accent-amber);
		color: var(--accent-amber);
		padding: 0.35rem 1rem;
		border-radius: 9999px;
		font-size: 1.1rem;
		font-weight: 800;
	}

	.sponsor-zone {
		display: flex;
		align-items: center;
		gap: 1.25rem;
	}

	.live-tag {
		color: #ef4444;
		font-weight: 800;
		font-size: 0.9rem;
		letter-spacing: 0.1em;
	}

	.btn-unpair {
		background: rgba(239, 68, 68, 0.12);
		border: 1px solid rgba(239, 68, 68, 0.35);
		color: #fca5a5;
		padding: 0.3rem 0.75rem;
		border-radius: var(--radius-sm);
		font-size: 0.85rem;
		font-weight: 700;
		cursor: pointer;
		transition: all 0.2s ease;
	}

	.btn-unpair:hover {
		background: rgba(239, 68, 68, 0.25);
		border-color: #ef4444;
		color: #fff;
	}

	.clutch-broadcast-banner {
		background: linear-gradient(90deg, #0891b2, #06b6d4, #0891b2);
		color: #000;
		font-size: 1.8rem;
		font-weight: 900;
		text-align: center;
		padding: 0.75rem;
		border-radius: var(--radius-md);
		letter-spacing: 0.1em;
		animation: pulse-clutch 0.8s infinite alternate;
	}

	.tv-main-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1.5rem;
		flex: 1;
	}

	.tv-left-column {
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
	}

	.thrower-showcase {
		padding: 1.5rem 2rem;
	}

	.showcase-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 1rem;
	}

	.streak-flame {
		color: var(--accent-amber);
		font-size: 1.2rem;
		font-weight: 900;
	}

	.thrower-profile {
		display: flex;
		align-items: center;
		gap: 1.5rem;
	}

	.tv-avatar {
		width: 72px;
		height: 72px;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 2.5rem;
		font-weight: 900;
		color: #000;
	}

	.tv-name-box {
		flex: 1;
	}

	.tv-thrower-name {
		font-size: 2.8rem;
		font-weight: 900;
		line-height: 1;
	}

	.tv-throw-count {
		font-size: 1.1rem;
		color: var(--text-secondary);
	}

	.tv-score-box {
		text-align: right;
	}

	.tv-score-val {
		font-size: 3.8rem;
		font-weight: 900;
		color: var(--accent-amber);
		line-height: 1;
		display: block;
	}

	.tv-score-lbl {
		font-size: 0.85rem;
		color: var(--text-secondary);
		text-transform: uppercase;
		letter-spacing: 0.1em;
	}

	.tv-target-card {
		flex: 1;
		padding: 1.5rem;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.tv-right-column {
		padding: 2rem;
		display: flex;
		flex-direction: column;
	}

	.board-header {
		margin-bottom: 1.5rem;
	}

	.board-title {
		font-size: 1.8rem;
		font-weight: 900;
		letter-spacing: 0.05em;
	}

	.board-sub {
		color: var(--text-secondary);
		font-size: 0.95rem;
	}

	.leaderboard-list {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
		flex: 1;
	}

	.leaderboard-row {
		display: grid;
		grid-template-columns: 60px 1fr 140px 100px;
		align-items: center;
		background: #11151f;
		border: 1px solid var(--border-color);
		padding: 1rem 1.5rem;
		border-radius: var(--radius-md);
		font-size: 1.25rem;
	}

	.rank-col {
		font-size: 1.6rem;
		font-weight: 900;
		color: var(--text-secondary);
	}

	.name-col {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		font-weight: 800;
	}

	.p-dot {
		width: 12px;
		height: 12px;
		border-radius: 50%;
	}

	.stats-col {
		font-size: 1rem;
		color: var(--text-muted);
	}

	.score-col {
		font-size: 2.2rem;
		font-weight: 900;
		text-align: right;
		color: var(--accent-amber);
	}

	.tv-attract-loop {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		text-align: center;
		padding: 4rem;
	}

	.big-axe { font-size: 5rem; margin-bottom: 1.5rem; }
	.attract-title { font-size: 3rem; font-weight: 900; margin-bottom: 1rem; color: var(--accent-amber); }
	.attract-sub { font-size: 1.5rem; color: var(--text-secondary); max-width: 700px; }

	/* VFX Overlays */
	.vfx-overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.85);
		backdrop-filter: blur(12px);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 200;
		animation: pop-in 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
	}

	.vfx-card {
		text-align: center;
	}

	.vfx-title {
		font-size: 5rem;
		font-weight: 900;
		letter-spacing: 0.05em;
	}

	.bullseye-vfx .vfx-title { color: #f59e0b; text-shadow: 0 0 40px rgba(245, 158, 11, 0.8); }
	.clutch-vfx .vfx-title { color: #06b6d4; text-shadow: 0 0 40px rgba(6, 182, 212, 0.8); }

	.vfx-points {
		font-size: 3rem;
		font-weight: 900;
		color: #ffffff;
		margin-top: 1rem;
	}

	@keyframes pop-in {
		0% { transform: scale(0.7); opacity: 0; }
		100% { transform: scale(1); opacity: 1; }
	}
</style>
