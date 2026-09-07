<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import WatlTarget from '$lib/components/WatlTarget.svelte';
	import MatchPodiumSummary from '$lib/components/MatchPodiumSummary.svelte';
	import {
		postApiLanesTerminalsPair,
		postApiLanesOperationsByLaneIdThrow,
		postApiLanesOperationsByLaneIdUndo,
		postApiLanesOperationsByLaneIdSkipTurn,
		postApiLanesOperationsByLaneIdStartSession
	} from '$lib/api/client';
	import { laneSignalR } from '$lib/services/signalr';
	import type { GameStateSnapshot, TerminalAuthResult } from '$lib/api/generated/types.gen';

	let pairingCode = $state('AX101');
	let terminalAuth = $state<TerminalAuthResult | null>(null);
	let gameState = $state<GameStateSnapshot | null>(null);
	let isClutchArmed = $state(false);
	let isPairing = $state(false);
	let safetyAlert = $state<string | null>(null);
	let lastThrowResult = $state<any>(null);

	onMount(async () => {
		const saved = localStorage.getItem('venueaxe_tablet_auth');
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

	async function handlePair() {
		isPairing = true;
		try {
			const res = await postApiLanesTerminalsPair({
				body: { pairingCode, terminalType: 'Tablet' }
			});
			if (res.data) {
				terminalAuth = res.data;
				localStorage.setItem('venueaxe_tablet_auth', JSON.stringify(terminalAuth));
				await initSignalR(terminalAuth.laneId);
				await loadActiveSession(terminalAuth.laneId);
			} else {
				alert('Invalid Pairing PIN');
			}
		} catch (e) {
			alert('Failed to pair tablet terminal');
		} finally {
			isPairing = false;
		}
	}

	async function initSignalR(laneId: string) {
		laneSignalR.onThrowRecorded = (state) => {
			gameState = state;
			if (state.lastThrow) {
				lastThrowResult = {
					x: Number(state.lastThrow.x ?? 0),
					y: Number(state.lastThrow.y ?? 0),
					pointsAwarded: Number(state.lastThrow.pointsAwarded ?? 0),
					zone: String(state.lastThrow.zone ?? '')
				};
			}
		};

		laneSignalR.onSafetyStopActivated = (reason) => {
			safetyAlert = reason;
		};

		try {
			await laneSignalR.connect(laneId);
		} catch (e) {
			console.error('SignalR connect error:', e);
		}
	}

	async function handleTargetThrow(payload: { x: number; y: number; isClutchCalled: boolean }) {
		if (!terminalAuth) return;

		try {
			const res = await postApiLanesOperationsByLaneIdThrow({
				path: { laneId: terminalAuth.laneId },
				body: {
					x: payload.x,
					y: payload.y,
					isClutchCalled: payload.isClutchCalled,
					manualZone: null
				}
			});
			if (res.data) {
				gameState = res.data;
				isClutchArmed = false;
			}
		} catch (e) {
			console.error(e);
		}
	}

	async function handleManualScore(points: number, isBullseye = false, isClutch = false) {
		if (!terminalAuth) return;

		let zone = 0;
		if (points === 6 || isBullseye) zone = 6;
		else if (points === 5) zone = 5;
		else if (points === 4) zone = 4;
		else if (points === 3) zone = 3;
		else if (points === 2) zone = 2;
		else if (points === 1) zone = 1;
		else if (points === 7 || isClutch) zone = 7;

		try {
			const res = await postApiLanesOperationsByLaneIdThrow({
				path: { laneId: terminalAuth.laneId },
				body: {
					x: null,
					y: null,
					manualZone: zone as any,
					isClutchCalled: isClutchArmed
				}
			});
			if (res.data) {
				gameState = res.data;
				isClutchArmed = false;
			}
		} catch (e) {
			console.error(e);
		}
	}

	function toggleClutch() {
		isClutchArmed = !isClutchArmed;
	}

	let isUndoing = $state(false);
	async function handleUndo() {
		if (!terminalAuth?.laneId || isUndoing) return;
		isUndoing = true;
		try {
			const res = await postApiLanesOperationsByLaneIdUndo({
				path: { laneId: terminalAuth.laneId }
			});
			if (res.data) {
				gameState = res.data;
				lastThrowResult = res.data.lastThrow ?? null;
			}
		} catch (e) {
			console.error('Failed to undo throw:', e);
		} finally {
			isUndoing = false;
		}
	}

	let isSkipping = $state(false);
	async function handleSkipTurn() {
		if (!terminalAuth?.laneId || isSkipping) return;
		isSkipping = true;
		try {
			const res = await postApiLanesOperationsByLaneIdSkipTurn({
				path: { laneId: terminalAuth.laneId }
			});
			if (res.data) {
				gameState = res.data;
				lastThrowResult = res.data.lastThrow ?? null;
			}
		} catch (e) {
			console.error('Failed to skip turn:', e);
		} finally {
			isSkipping = false;
		}
	}

	let isRematching = $state(false);
	async function handleRematch() {
		if (!terminalAuth?.laneId || !gameState?.players || isRematching) return;
		isRematching = true;
		try {
			const res = await postApiLanesOperationsByLaneIdStartSession({
				path: { laneId: terminalAuth.laneId },
				body: {
					sessionTitle: `Rematch: ${(gameState.players ?? []).map((p) => p.name).join(' vs ')}`,
					durationMinutes: 60,
					initialRoster: (gameState.players ?? []).map((p) => ({
						name: p.name ?? 'Thrower',
						avatarColor: p.avatarColor ?? '#f59e0b'
					})),
					bookingId: null,
					gameTypeId: gameState.gameTypeId ?? 'watl_standard'
				}
			});
			if (res.data?.currentGame) {
				gameState = res.data.currentGame;
				lastThrowResult = null;
			}
		} catch (e) {
			console.error('Failed to start rematch:', e);
		} finally {
			isRematching = false;
		}
	}

	function resetPairing() {
		localStorage.removeItem('venueaxe_tablet_auth');
		terminalAuth = null;
		gameState = null;
		laneSignalR.disconnect();
	}
</script>

<div class="tablet-viewport">
	{#if !terminalAuth}
		<!-- PAIRING SCREEN -->
		<div class="pair-card glass-panel">
			<div class="pair-header">
				<span class="icon">📱</span>
				<h1 class="title font-display">In-Lane Tablet Console</h1>
				<p class="subtitle">Enter the 6-character Pairing PIN displayed on the Lane Management dashboard.</p>
			</div>

			<div class="form-group">
				<label class="form-label" for="pairing-code">Pairing PIN</label>
				<input id="pairing-code" type="text" class="form-input pin-input font-display" bind:value={pairingCode} placeholder="AX101" />
			</div>

			<button class="btn btn-primary btn-block" disabled={isPairing} onclick={handlePair}>
				{isPairing ? 'Connecting...' : 'Connect to Lane Terminal'}
			</button>
		</div>
	{:else}
		<!-- ACTIVE CONSOLE -->
		<div class="console-layout">
			<!-- Top HUD Bar -->
			<div class="hud-bar">
				<div class="hud-lane">
					<span class="badge badge-active">{terminalAuth.laneName}</span>
					{#if gameState}
						<span class="game-title font-display">{gameState.gameName}</span>
					{/if}
				</div>

				<div class="hud-status">
					{#if gameState}
						<span class="round-counter font-display">
							Round {gameState.currentRound} / {gameState.totalRounds}
						</span>
					{/if}
					<button class="btn-disconnect" onclick={resetPairing}>Unpair</button>
				</div>
			</div>

			<!-- Safety Alert Modal -->
			{#if safetyAlert}
				<div class="safety-banner">
					<span class="safety-icon">⚠️</span>
					<div>
						<h2 class="font-display">SAFETY PAUSE ACTIVATED</h2>
						<p>{safetyAlert} - Please step behind the safety line.</p>
					</div>
					<button class="btn btn-secondary btn-sm" onclick={() => (safetyAlert = null)}>Dismiss</button>
				</div>
			{/if}

			{#if gameState && gameState.players && gameState.players.length > 0}
				{#if gameState.status === 2 || String(gameState.status).toLowerCase() === 'finished'}
					<!-- POST-MATCH PODIUM & SCATTER HEATMAP SUMMARY -->
					<MatchPodiumSummary
						{gameState}
						targetType={gameState.gameTypeId === 'iatf_standard' ? 'iatf' : 'watl'}
						onrematch={handleRematch}
						onundo={handleUndo}
					/>
				{:else}
					{@const activeIdx = Number(gameState.currentPlayerIndex ?? 0)}
					{@const activePlayer = gameState.players[activeIdx] ?? gameState.players[0]}

					<!-- Active Thrower Banner -->
					<div class="player-banner">
						<div class="player-identity">
							<div class="player-avatar" style="background-color: {activePlayer.avatarColor ?? '#f59e0b'}">
								{(activePlayer.name ?? 'T').charAt(0)}
							</div>
							<div>
								<div class="up-next-label">CURRENT THROWER</div>
								<h2 class="player-name font-display">{activePlayer.name}</h2>
							</div>
						</div>

						<div class="player-stats">
							<div class="stat-box">
								<span class="stat-val font-display">{activePlayer.score ?? 0}</span>
								<span class="stat-lbl">Points</span>
							</div>
							<div class="stat-box">
								<span class="stat-val font-display">{activePlayer.streak ?? 0}🔥</span>
								<span class="stat-lbl">Streak</span>
							</div>
						</div>
					</div>

					<!-- Main Interactive Scoring Arena -->
					<div class="arena-grid">
						<!-- Interactive WATL SVG Target -->
						<div class="target-card glass-panel">
							<WatlTarget
								interactive={true}
								isClutchCalled={isClutchArmed}
								targetType={gameState?.gameTypeId === 'iatf_standard' ? 'iatf' : 'watl'}
								lastThrow={lastThrowResult}
								onthrow={handleTargetThrow}
							/>
							<p class="target-hint">🎯 Tap the exact spot on the board where the axe landed</p>
						</div>

						<!-- Tactile Control Console -->
						<div class="controls-card glass-panel">
							<h3 class="controls-title font-display">Quick Touch Scoring</h3>

							<!-- CALL SPECIAL BUTTON (WATL Killshot 8 pts / IATF Clutch 7 pts) -->
							<button
								class="btn btn-clutch"
								class:armed={isClutchArmed}
								onclick={toggleClutch}
							>
								⚡ {isClutchArmed 
									? (gameState?.gameTypeId === 'iatf_standard' ? 'CLUTCH ARMED (7 PTS)' : 'KILLSHOT ARMED (8 PTS)') 
									: (gameState?.gameTypeId === 'iatf_standard' ? 'CALL CLUTCH (7 PTS)' : 'CALL KILLSHOT (8 PTS)')}
							</button>

							<!-- Number Scoring Grid -->
							<div class="touch-keypad">
								{#if gameState?.gameTypeId === 'iatf_standard'}
									<button class="key-btn key-bull" onclick={() => handleManualScore(5, true)}>
										5<small>Bull</small>
									</button>
									<button class="key-btn" onclick={() => handleManualScore(3)}>
										3<small>Middle</small>
									</button>
									<button class="key-btn" onclick={() => handleManualScore(1)}>
										1<small>Outer</small>
									</button>
									<button class="key-btn key-miss" onclick={() => handleManualScore(0)}>
										0<small>Drop/Miss</small>
									</button>
									<button class="key-btn key-fault" onclick={() => handleManualScore(0)}>
										Fault
									</button>
								{:else}
									<button class="key-btn key-bull" onclick={() => handleManualScore(6, true)}>
										6<small>Bull</small>
									</button>
									<button class="key-btn" onclick={() => handleManualScore(5)}>5</button>
									<button class="key-btn" onclick={() => handleManualScore(4)}>4</button>
									<button class="key-btn" onclick={() => handleManualScore(3)}>3</button>
									<button class="key-btn" onclick={() => handleManualScore(2)}>2</button>
									<button class="key-btn" onclick={() => handleManualScore(1)}>1</button>
									<button class="key-btn key-miss" onclick={() => handleManualScore(0)}>
										0<small>Drop/Miss</small>
									</button>
									<button class="key-btn key-fault" onclick={() => handleManualScore(0)}>
										Fault
									</button>
								{/if}
							</div>

							<!-- Turn Control Actions: Undo & Pass -->
							<div class="turn-actions">
								<button
									class="btn btn-secondary btn-action font-display"
									disabled={isUndoing || !gameState.allThrows || gameState.allThrows.length === 0}
									onclick={handleUndo}
									title="Undo previous throw and revert turn"
								>
									{isUndoing ? 'Undoing...' : '↩️ Undo Throw'}
								</button>
								<button
									class="btn btn-outline btn-action font-display"
									disabled={isSkipping}
									onclick={handleSkipTurn}
									title="Pass / Skip this thrower's turn"
								>
									{isSkipping ? 'Passing...' : '⏭️ Pass Turn'}
								</button>
							</div>

							<!-- Match Leaderboard Mini -->
							<div class="mini-roster">
								<h4 class="roster-title font-display">Thrower Leaderboard</h4>
								{#each gameState.players as p, idx (p.id ?? idx)}
									<div class="roster-row" class:active-row={idx === activeIdx}>
										<span class="p-name">{p.name}</span>
										<span class="p-score font-display">{p.score ?? 0} pts</span>
									</div>
								{/each}
							</div>
						</div>
					</div>
				{/if}
			{:else}
				<div class="empty-state glass-panel">
					<h2 class="font-display">Waiting for Game Session</h2>
					<p class="text-secondary">Please start a throwing session from the Venue Admin operations console.</p>
				</div>
			{/if}
		</div>
	{/if}
</div>

<style>
	.tablet-viewport {
		max-width: 1400px;
		margin: 0 auto;
		padding: 1rem 1.5rem 3rem;
		min-height: calc(100vh - 80px);
	}

	.pair-card {
		max-width: 460px;
		margin: 4rem auto;
		padding: 2.5rem;
		text-align: center;
	}

	.pin-input {
		font-size: 1.8rem;
		text-align: center;
		letter-spacing: 0.15em;
		font-weight: 800;
		color: var(--accent-amber);
	}

	.console-layout {
		display: flex;
		flex-direction: column;
		gap: 1rem;
	}

	.hud-bar {
		display: flex;
		justify-content: space-between;
		align-items: center;
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		padding: 0.75rem 1.25rem;
		border-radius: var(--radius-md);
	}

	.hud-lane {
		display: flex;
		align-items: center;
		gap: 1rem;
	}

	.game-title {
		font-size: 1.1rem;
		font-weight: 700;
		color: var(--text-primary);
	}

	.round-counter {
		font-size: 1.1rem;
		font-weight: 800;
		color: var(--accent-amber);
		margin-right: 1rem;
	}

	.btn-disconnect {
		background: transparent;
		border: 1px solid var(--border-color);
		color: var(--text-muted);
		padding: 0.3rem 0.6rem;
		border-radius: var(--radius-sm);
		font-size: 0.75rem;
		cursor: pointer;
	}

	.player-banner {
		background: linear-gradient(135deg, #182030, #10141f);
		border: 1px solid var(--border-highlight);
		border-radius: var(--radius-lg);
		padding: 1.25rem 2rem;
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.player-identity {
		display: flex;
		align-items: center;
		gap: 1.25rem;
	}

	.player-avatar {
		width: 58px;
		height: 58px;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 1.8rem;
		font-weight: 900;
		color: #000;
	}

	.up-next-label {
		font-size: 0.75rem;
		font-weight: 700;
		color: var(--accent-amber);
		letter-spacing: 0.1em;
	}

	.player-name {
		font-size: 2rem;
		font-weight: 800;
	}

	.player-stats {
		display: flex;
		gap: 1.5rem;
	}

	.stat-box {
		text-align: center;
	}

	.stat-val {
		font-size: 2.2rem;
		font-weight: 900;
		color: var(--accent-amber);
		display: block;
		line-height: 1;
	}

	.stat-lbl {
		font-size: 0.75rem;
		color: var(--text-secondary);
		text-transform: uppercase;
	}

	.arena-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1.5rem;
	}

	.target-card {
		padding: 1.5rem;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
	}

	.target-hint {
		font-size: 0.85rem;
		color: var(--text-secondary);
		margin-top: 0.75rem;
		text-align: center;
	}

	.controls-card {
		padding: 1.5rem;
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
	}

	.controls-title {
		font-size: 1.1rem;
		text-transform: uppercase;
	}

	.btn-clutch {
		background: rgba(6, 182, 212, 0.15);
		border: 2px solid var(--accent-cyan);
		color: var(--accent-cyan);
		font-family: var(--font-display);
		font-weight: 800;
		font-size: 1.1rem;
		padding: 1rem;
		border-radius: var(--radius-md);
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.btn-clutch.armed {
		background: var(--accent-cyan);
		color: #000;
		box-shadow: 0 0 25px var(--accent-cyan-glow);
		animation: pulse-clutch 1s infinite alternate;
	}

	.touch-keypad {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: 0.75rem;
	}

	.key-btn {
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		color: var(--text-primary);
		font-family: var(--font-display);
		font-size: 1.6rem;
		font-weight: 800;
		padding: 1rem 0;
		border-radius: var(--radius-md);
		cursor: pointer;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
	}

	.key-btn small {
		font-size: 0.65rem;
		font-weight: 600;
		color: var(--text-muted);
		text-transform: uppercase;
	}

	.key-btn:hover {
		background: var(--bg-surface-elevated);
		border-color: var(--border-highlight);
	}

	.key-bull {
		border-color: var(--accent-amber);
		color: var(--accent-amber);
	}

	.key-miss {
		color: #f87171;
	}

	.mini-roster {
		border-top: 1px solid var(--border-color);
		padding-top: 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.turn-actions {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 0.75rem;
		margin-top: 0.25rem;
	}

	.btn-action {
		padding: 0.75rem;
		font-size: 0.95rem;
		font-weight: 700;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.35rem;
	}

	.btn-action:disabled {
		opacity: 0.4;
		cursor: not-allowed;
	}

	.roster-title {
		font-size: 0.85rem;
		color: var(--text-secondary);
		text-transform: uppercase;
	}

	.roster-row {
		display: flex;
		justify-content: space-between;
		padding: 0.4rem 0.75rem;
		border-radius: var(--radius-sm);
		background: var(--bg-surface);
		font-size: 0.9rem;
	}

	.active-row {
		background: rgba(245, 158, 11, 0.2);
		border: 1px solid var(--accent-amber);
	}

	.safety-banner {
		background: rgba(239, 68, 68, 0.25);
		border: 2px solid var(--accent-crimson);
		color: #fff;
		padding: 1rem 1.5rem;
		border-radius: var(--radius-md);
		display: flex;
		align-items: center;
		gap: 1rem;
	}

	.safety-icon {
		font-size: 2rem;
	}

	.empty-state {
		padding: 4rem 2rem;
		text-align: center;
	}

	@media (max-width: 900px) {
		.arena-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
