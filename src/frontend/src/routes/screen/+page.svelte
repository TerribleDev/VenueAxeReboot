<script lang="ts">
	import { onMount, onDestroy } from "svelte";
	import WatlTarget from "$lib/components/WatlTarget.svelte";
	import MatchPodiumSummary from "$lib/components/MatchPodiumSummary.svelte";
	import GameRulesModal from "$lib/components/GameRulesModal.svelte";
	import QrCode from "$lib/components/QrCode.svelte";
	import { postApiLanesTerminalsPair } from "$lib/api/client";
	import { laneSignalR } from "$lib/services/signalr";
	import type {
		GameStateSnapshot,
		TerminalAuthResult,
	} from "$lib/api/generated/types.gen";

	let pairingCode = $state("200001");
	let terminalAuth = $state<TerminalAuthResult | null>(null);
	let gameState = $state<GameStateSnapshot | null>(null);
	let sessionTitle = $state<string | null>(null);
	let showGameRulesModal = $state(false);
	let isPairing = $state(false);
	let showBullseyeCelebration = $state(false);
	let showClutchCelebration = $state(false);
	let clutchAlert = $state<string | null>(null);
	let heartbeatInterval = $state<any>(null);
	let sessionRemainingSeconds = $state(0);
	let sessionTimerInterval: any = null;

	function formatTimer(totalSecs: number): string {
		const m = Math.floor(totalSecs / 60);
		const s = totalSecs % 60;
		return `${m}:${String(s).padStart(2, "0")}`;
	}

	function startSessionTimer(expiresAtIso: string) {
		if (sessionTimerInterval) clearInterval(sessionTimerInterval);
		const update = () => {
			if (!expiresAtIso) {
				sessionRemainingSeconds = 0;
				return;
			}
			const diffMs = new Date(expiresAtIso).getTime() - Date.now();
			sessionRemainingSeconds = Math.max(0, Math.floor(diffMs / 1000));
		};
		update();
		sessionTimerInterval = setInterval(update, 1000);
	}

	// Derived Tic-Tac-Toe Grid
	let tttGrid = $derived.by(() => {
		if (
			gameState?.gameTypeId !== "axe_tictactoe" ||
			!gameState.allThrows ||
			!gameState.players
		) {
			return Array(9).fill(null);
		}
		const grid = Array(9).fill(null);
		const p1Id = gameState.players[0]?.id;
		const p2Id = gameState.players[1]?.id;

		for (const t of gameState.allThrows) {
			const pts = Number(t.pointsAwarded ?? 0);
			if (pts >= 1 && pts <= 9) {
				const cellIdx = pts - 1;
				if (t.playerId === p1Id) grid[cellIdx] = "X";
				else if (t.playerId === p2Id) grid[cellIdx] = "O";
			}
		}
		return grid;
	});

	// Derived Arcade Status
	let activeThrower = $derived.by(() => {
		if (!gameState?.players || gameState.currentPlayerIndex === undefined)
			return null;
		return gameState.players[Number(gameState.currentPlayerIndex)];
	});

	let arcadeObjective = $derived.by(() => {
		if (!gameState || !activeThrower) return null;
		if (gameState.gameTypeId === "around_the_world") {
			const currentTarget = Math.min(
				7,
				Number(activeThrower.score ?? 0) + 1,
			);
			return {
				title: "AROUND THE WORLD",
				detail: `TARGET: RING ${currentTarget} (${currentTarget === 7 ? "BULLSEYE" : `${currentTarget} PT RING`})`,
				progress: `${Number(activeThrower.score ?? 0)}/7 RINGS HIT`,
			};
		}
		if (gameState.gameTypeId === "blackjack_21" || gameState.gameTypeId === "first_to_21") {
			const score = Number(activeThrower.score ?? 0);
			const diff = 21 - score;
			return {
				title: "FIRST TO 21",
				detail:
					score > 21
						? "💥 BUSTED (> 21) — RESET TO 13!"
						: score === 21
							? "🏆 21 REACHED!"
							: `CURRENT: ${score} / 21 (${diff} NEEDED)`,
				progress: score > 21 ? "BUST" : `${score} PTS`,
			};
		}
		if (gameState.gameTypeId === "countdown_301" || gameState.gameTypeId === "countdown_603") {
			const score = Number(activeThrower.score ?? 603);
			return {
				title: "COUNTDOWN 603",
				detail: `REMAINING: ${score} PTS TO ZERO`,
				progress: score === 0 ? "VICTORY" : `${score} REMAINING`,
			};
		}
		return null;
	});

	onMount(async () => {
		const saved = localStorage.getItem("venueaxe_screen_auth");
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

		// 10s Heartbeat Loop for TV screen presence telemetry
		heartbeatInterval = setInterval(async () => {
			if (terminalAuth?.laneId) {
				try {
					await laneSignalR.sendHeartbeat(terminalAuth.laneId, true);
				} catch (e) {
					// ignore
				}
			}
		}, 10000);
	});

	onDestroy(() => {
		if (heartbeatInterval) {
			clearInterval(heartbeatInterval);
			heartbeatInterval = null;
		}
		if (sessionTimerInterval) {
			clearInterval(sessionTimerInterval);
			sessionTimerInterval = null;
		}
		laneSignalR.disconnect();
	});

	async function loadActiveSession(laneId: string) {
		try {
			const res = await fetch(
				`/api/lanes/operations/${laneId}/active-session`,
				{
					credentials: "include",
				},
			);
			if (res.ok) {
				const session = await res.json();
				if (session) {
					sessionTitle = session.sessionTitle || null;
					if (session.currentGame) {
						gameState = session.currentGame;
					}
					if (session.expiresAt) {
						startSessionTimer(session.expiresAt);
					}
					return;
				}
			}
			sessionTitle = null;
		} catch (e) {
			console.error("Failed to load active session:", e);
		}
	}

	function resetPairing() {
		localStorage.removeItem("venueaxe_screen_auth");
		terminalAuth = null;
		gameState = null;
		sessionTitle = null;
		laneSignalR.disconnect();
	}

	async function handlePair() {
		isPairing = true;
		try {
			const res = await postApiLanesTerminalsPair({
				body: { pairingCode, terminalType: "Screen" },
			});
			if (res.data) {
				terminalAuth = res.data;
				localStorage.setItem(
					"venueaxe_screen_auth",
					JSON.stringify(terminalAuth),
				);
				await initSignalR(terminalAuth.laneId);
				await loadActiveSession(terminalAuth.laneId);
			} else {
				alert("Invalid TV Pairing PIN");
			}
		} catch (e) {
			alert("Failed to connect TV screen");
		} finally {
			isPairing = false;
		}
	}

	async function initSignalR(laneId: string) {
		laneSignalR.onThrowRecorded = (state) => {
			gameState = state;
			if (state.lastThrow?.isBullseye) {
				triggerBullseyeVFX();
			} else if (
				state.lastThrow?.isClutchCalled &&
				(state.lastThrow.pointsAwarded === 8 ||
					state.lastThrow.pointsAwarded === 7)
			) {
				triggerSpecialVFX();
			}
		};

		laneSignalR.onClutchCalled = (playerId, side) => {
			const label =
				gameState?.gameTypeId === "iatf_standard"
					? "CLUTCH"
					: "KILLSHOT";
			clutchAlert = `⚡ ${label} CALLED (${side.toUpperCase()}) ⚡`;
			setTimeout(() => (clutchAlert = null), 6000);
		};

		laneSignalR.onStateChanged = async () => {
			await loadActiveSession(laneId);
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
			<p class="tv-subtitle">
				Enter the 6-digit TV Display PIN to connect this screen.
			</p>
			<input
				type="text"
				class="form-input tv-pin-input font-display"
				bind:value={pairingCode}
				placeholder="200001"
				maxlength="6"
			/>
			<button
				class="btn btn-primary btn-lg font-display"
				disabled={isPairing}
				onclick={handlePair}
			>
				{isPairing ? "Connecting..." : "Connect Overhead TV Display"}
			</button>
		</div>
	{:else}
		<!-- BROADCAST TV INTERFACE -->
		<div class="broadcast-container">
			<!-- Header Broadcast Bar -->
			<div class="broadcast-header">
				<div class="brand-zone">
					{#if terminalAuth.venueIconUrl}
						<img src={terminalAuth.venueIconUrl} alt={terminalAuth.venueName || 'Venue'} style="width: 44px; height: 44px; border-radius: 10px; object-fit: contain; background: rgba(15, 23, 42, 0.6); border: 1.5px solid rgba(255, 255, 255, 0.2); padding: 3px; margin-right: 0.5rem;" />
					{:else}
						<span class="axe-icon">🪓</span>
					{/if}
					<div style="display: flex; flex-direction: column; justify-content: center; gap: 0.15rem;">
						<span class="lane-title font-display">{terminalAuth.laneName}</span>
						{#if sessionTitle}
							<span class="session-name font-display" style="font-size: 0.95rem; color: var(--accent-amber); font-weight: 800; letter-spacing: 0.05em; text-transform: uppercase; background: rgba(245, 158, 11, 0.15); border: 1px solid rgba(245, 158, 11, 0.35); padding: 0.15rem 0.55rem; border-radius: 6px; width: fit-content;">
								🎯 {sessionTitle}
							</span>
						{/if}
					</div>
				</div>

				{#if gameState}
					<div class="match-center">
						<span class="match-mode font-display"
							>{gameState.gameName}</span
						>
						<button
							type="button"
							class="btn-rules-screen font-display"
							onclick={() => (showGameRulesModal = true)}
							title="View Game Rules"
						>
							❓ Rules
						</button>
						<span class="round-badge font-display"
							>ROUND {gameState.currentRound} OF {gameState.totalRounds}</span
						>
					</div>
				{/if}

				<div class="sponsor-zone">
					{#if sessionRemainingSeconds > 0}
						<div class="screen-timer font-display" class:timer-warning={sessionRemainingSeconds <= 300}>
							<span class="timer-icon">⏱️</span>
							<span>{formatTimer(sessionRemainingSeconds)}</span>
						</div>
					{/if}
					<span class="live-tag">● LIVE SCORING</span>
					<button
						class="btn-unpair font-display"
						onclick={resetPairing}
						title="Unpair TV Screen"
					>
						Unpair
					</button>
				</div>
			</div>

			<!-- Low Time Warning Banner (Within 5 min of lane closing) -->
			{#if sessionRemainingSeconds > 0 && sessionRemainingSeconds <= 300}
				<div class="tv-low-time-banner font-display">
					<span class="pulse-alert">⚠️</span>
					<span>LOW TIME WARNING: {Math.ceil(sessionRemainingSeconds / 60)} MINUTES REMAINING ({formatTimer(sessionRemainingSeconds)}) — FINAL THROWS</span>
				</div>
			{/if}

			<!-- Clutch Alert Banner -->
			{#if clutchAlert}
				<div class="clutch-broadcast-banner font-display">
					{clutchAlert}
				</div>
			{/if}

			{#if gameState && gameState.players && gameState.players.length > 0 && gameState.currentPlayerIndex !== undefined}
				{#if Number(gameState.status) === 2 || (gameState.status as any) === "Finished"}
					<MatchPodiumSummary {gameState} />
				{:else}
					{@const players = gameState.players}
					{@const activePlayer =
						players[Number(gameState.currentPlayerIndex)]}
					{@const sortedLeaderboard = [...players].sort(
						(a, b) => Number(b.score) - Number(a.score),
					)}

					<!-- Arcade HUD Objective Bar (BUG-007) -->
					{#if arcadeObjective}
						<div class="arcade-hud-bar">
							<div class="arcade-hud-title font-display">
								{arcadeObjective.title}
							</div>
							<div class="arcade-hud-detail">
								{arcadeObjective.detail}
							</div>
							<div class="arcade-hud-progress font-display">
								{arcadeObjective.progress}
							</div>
						</div>
					{/if}

					<div class="tv-main-grid">
						<!-- Left: Active Thrower Card & Live Target Visualizer -->
						<div class="tv-left-column">
							<!-- Thrower Showcase -->
							<div class="thrower-showcase glass-panel">
								<div class="showcase-header">
									<span
										class="badge badge-active font-display"
										>ACTIVE THROWER</span
									>
									<span class="streak-flame font-display">
										{Number(activePlayer.streak || 0) > 0
											? `🔥 ${activePlayer.streak} IN A ROW`
											: ""}
									</span>
								</div>

								<div class="thrower-profile">
									<div
										class="tv-avatar"
										style="background-color: {activePlayer.avatarColor}"
									>
										{(activePlayer.name || "P").charAt(0)}
									</div>
									<div class="tv-name-box">
										<h2
											class="tv-thrower-name font-display"
										>
											{activePlayer.name || "Thrower"}
										</h2>
										<span
											class="tv-throw-count font-display"
											>Throws Taken: {activePlayer.throwsTaken ??
												0}</span
										>
									</div>
									<div class="tv-score-box">
										<span class="tv-score-val font-display"
											>{activePlayer.score}</span
										>
										<span class="tv-score-lbl">SCORE</span>
									</div>
								</div>
							</div>

							<!-- Target Hit Visualizer (BUG-006: with tttGrid) -->
							<div class="tv-target-card glass-panel">
								<WatlTarget
									interactive={false}
									overlayMode={gameState?.gameTypeId ===
									"axe_tictactoe"
										? "tic_tac_toe"
										: "standard"}
									{tttGrid}
									lastThrow={gameState.lastThrow as any}
								/>
							</div>
						</div>

						<!-- Right: Broadcast Scoreboard Leaderboard -->
						<div class="tv-right-column glass-panel">
							<h3 class="font-display tv-panel-title">
								MATCH LEADERBOARD
							</h3>

							<div class="tv-leaderboard-list">
								{#each sortedLeaderboard as p, i (p.id)}
									<div
										class="tv-leaderboard-item"
										class:leader-first={i === 0}
										class:item-active={p.id ===
											activePlayer.id}
									>
										<div class="item-rank font-display">
											#{i + 1}
										</div>
										<div
											class="item-avatar"
											style="background-color: {p.avatarColor}"
										>
											{(p.name || "P").charAt(0)}
										</div>
										<div class="item-name font-display">
											{p.name}
										</div>
										<div class="item-throws">
											Throws: {p.throwsTaken ?? 0}
										</div>
										<div class="item-score font-display">
											{p.score} pts
										</div>
									</div>
								{/each}
							</div>
						</div>
					</div>
				{/if}
			{:else}
				<!-- Rich Idle Attract Loop with Scannable QR Codes (BUG-007, Pit 7) -->
				<div class="tv-attract-loop glass-panel">
					<div class="attract-hero">
						{#if terminalAuth.venueIconUrl}
							<img src={terminalAuth.venueIconUrl} alt={terminalAuth.venueName || 'Venue'} style="width: 100px; height: 100px; border-radius: 20px; object-fit: contain; margin: 0 auto 1rem; background: rgba(15, 23, 42, 0.7); border: 2px solid rgba(255, 255, 255, 0.25); padding: 8px; box-shadow: 0 0 35px rgba(245, 158, 11, 0.3);" />
						{:else}
							<span class="big-axe">🪓</span>
						{/if}
						<h1 class="font-display attract-title">
							WELCOME TO {terminalAuth.laneName}
						</h1>
						{#if sessionTitle}
							<div class="font-display attract-session-banner" style="display: inline-flex; align-items: center; gap: 0.5rem; margin: 0.75rem auto 0; padding: 0.4rem 1.25rem; background: rgba(245, 158, 11, 0.15); border: 1.5px solid rgba(245, 158, 11, 0.4); border-radius: 9999px; color: var(--accent-amber); font-size: 1.25rem; font-weight: 800; letter-spacing: 0.05em; text-transform: uppercase;">
								<span>🎯</span> {sessionTitle}
							</div>
						{/if}
						<p class="attract-sub">
							Step up to the lane! Your axe throwing coach will
							launch the live match shortly.
						</p>
					</div>

					<div class="attract-qr-grid">
						<div class="qr-action-card glass-panel">
							<QrCode
								text="http://localhost:5173/sign/downtown"
								size={190}
							/>
							<div class="qr-card-info">
								<span class="qr-card-badge"
									>MANDATORY BEFORE THROWING</span
								>
								<h3 class="font-display qr-card-title">
									Sign Digital Waiver
								</h3>
								<p class="qr-card-desc">
									Scan with your smartphone camera to sign
									liability waiver directly on your phone.
								</p>
							</div>
						</div>

						<div class="qr-action-card glass-panel">
							<QrCode
								text="http://localhost:5173/book/downtown"
								size={190}
							/>
							<div class="qr-card-info">
								<span class="qr-card-badge qr-badge-food"
									>LANE-SIDE CONCESSIONS</span
								>
								<h3 class="font-display qr-card-title">
									Craft Beer & Snacks
								</h3>
								<p class="qr-card-desc">
									Order ice cold local draft craft beer,
									pizza, and party snacks right to Lane {terminalAuth.laneName}.
								</p>
							</div>
						</div>
					</div>

					<div class="attract-safety-bar">
						<span class="safety-rule"
							>⚠️ ONE THROWER IN LANE AT A TIME</span
						>
						<span class="safety-rule"
							>👟 CLOSED-TOE SHOES REQUIRED</span
						>
						<span class="safety-rule"
							>🪵 RETRIEVE AXES ONLY WHEN STATIONARY</span
						>
					</div>
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

		<!-- Special Hit Celebration VFX Modal (Killshot 8 pts) -->
		{#if showClutchCelebration}
			<div class="vfx-overlay clutch-vfx">
				<div class="vfx-card">
					<h1 class="vfx-title font-display">
						⚡ KILLSHOT NAILED! ⚡
					</h1>
					<p class="vfx-points font-display">+8 POINTS</p>
				</div>
			</div>
		{/if}
	{/if}

	<GameRulesModal
		gameTypeId={gameState?.gameTypeId || 'watl_standard'}
		isOpen={showGameRulesModal}
		onClose={() => (showGameRulesModal = false)}
	/>
</div>

<style>
	.btn-rules-screen {
		background: rgba(245, 158, 11, 0.2);
		border: 1px solid rgba(245, 158, 11, 0.45);
		color: #f59e0b;
		padding: 0.25rem 0.65rem;
		border-radius: 9999px;
		font-size: 0.85rem;
		font-weight: 700;
		cursor: pointer;
		transition: all 0.2s ease;
		margin: 0 0.5rem;
	}

	.btn-rules-screen:hover {
		background: rgba(245, 158, 11, 0.4);
		color: #fff;
		border-color: #f59e0b;
		transform: scale(1.05);
	}

	.screen-timer {
		display: flex;
		align-items: center;
		gap: 0.4rem;
		background: rgba(255, 255, 255, 0.08);
		border: 1px solid rgba(255, 255, 255, 0.2);
		padding: 0.35rem 0.85rem;
		border-radius: 9999px;
		font-size: 1.15rem;
		font-weight: 900;
		color: #f8fafc;
	}

	.screen-timer.timer-warning {
		background: rgba(239, 68, 68, 0.25);
		border-color: #ef4444;
		color: #fecaca;
		animation: pulse-warning 1.5s infinite alternate;
	}

	.tv-low-time-banner {
		background: linear-gradient(90deg, #b91c1c, #d97706);
		color: #ffffff;
		border: 2px solid #fef08a;
		border-radius: var(--radius-md, 8px);
		padding: 0.75rem 2rem;
		font-size: 1.35rem;
		font-weight: 900;
		letter-spacing: 0.08em;
		text-align: center;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 1rem;
		box-shadow: 0 0 24px rgba(220, 38, 38, 0.6);
		animation: pulse-tv-banner 1.5s infinite ease-in-out;
	}

	@keyframes pulse-warning {
		0% { transform: scale(1); }
		100% { transform: scale(1.05); }
	}

	@keyframes pulse-tv-banner {
		0%, 100% { box-shadow: 0 0 16px rgba(220, 38, 38, 0.5); }
		50% { box-shadow: 0 0 32px rgba(245, 158, 11, 0.8); }
	}

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

	.tv-icon {
		font-size: 3rem;
		margin-bottom: 1rem;
		display: block;
	}
	.tv-title {
		font-size: 2rem;
		font-weight: 800;
		margin-bottom: 0.5rem;
	}
	.tv-subtitle {
		color: var(--text-secondary);
		margin-bottom: 2rem;
	}
	.tv-pin-input {
		font-size: 2rem;
		text-align: center;
		letter-spacing: 0.16em;
		font-weight: 900;
		color: var(--accent-amber);
		margin-bottom: 1.5rem;
		width: 100%;
		max-width: 340px;
		margin-left: auto;
		margin-right: auto;
		padding: 0.75rem 1rem;
		box-sizing: border-box;
	}

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

	.axe-icon {
		font-size: 2rem;
	}
	.lane-title {
		font-size: 2rem;
		font-weight: 900;
		color: var(--accent-amber);
	}

	.match-center {
		display: flex;
		align-items: center;
		gap: 1.5rem;
	}

	.match-mode {
		font-size: 1.4rem;
		font-weight: 800;
	}
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

	.arcade-hud-bar {
		display: flex;
		justify-content: space-between;
		align-items: center;
		background: rgba(30, 41, 59, 0.8);
		border: 2px solid var(--accent-amber);
		padding: 0.85rem 2rem;
		border-radius: var(--radius-md);
		box-shadow: 0 0 20px rgba(245, 158, 11, 0.25);
	}

	.arcade-hud-title {
		font-size: 1.3rem;
		font-weight: 900;
		color: var(--accent-amber);
		letter-spacing: 0.05em;
	}

	.arcade-hud-detail {
		font-size: 1.5rem;
		font-weight: 800;
		color: #ffffff;
	}

	.arcade-hud-progress {
		font-size: 1.3rem;
		font-weight: 900;
		background: rgba(245, 158, 11, 0.2);
		border: 1px solid var(--accent-amber);
		color: var(--accent-amber);
		padding: 0.25rem 1rem;
		border-radius: 9999px;
	}

	.tv-panel-title {
		font-size: 2rem;
		font-weight: 900;
		letter-spacing: 0.05em;
		margin-bottom: 1.25rem;
		color: #ffffff;
	}

	.tv-leaderboard-list {
		display: flex;
		flex-direction: column;
		gap: 1rem;
		flex: 1;
	}

	.tv-leaderboard-item {
		display: grid;
		grid-template-columns: 60px 50px 1fr 130px 120px;
		align-items: center;
		background: #111622;
		border: 2px solid var(--border-color);
		padding: 1rem 1.5rem;
		border-radius: var(--radius-md);
		font-size: 1.3rem;
		transition: all 0.2s ease;
	}

	.leader-first {
		border-color: var(--accent-amber);
		background: rgba(245, 158, 11, 0.08);
	}

	.item-active {
		outline: 3px solid var(--accent-amber);
		box-shadow: 0 0 20px rgba(245, 158, 11, 0.4);
	}

	.item-rank {
		font-size: 1.8rem;
		font-weight: 900;
		color: var(--text-secondary);
	}

	.leader-first .item-rank {
		color: var(--accent-amber);
	}

	.item-avatar {
		width: 44px;
		height: 44px;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 1.4rem;
		font-weight: 900;
		color: #000;
	}

	.item-name {
		font-size: 1.8rem;
		font-weight: 900;
		color: #fff;
		padding-left: 0.75rem;
	}

	.item-throws {
		font-size: 1.1rem;
		color: var(--text-secondary);
		font-weight: 600;
	}

	.item-score {
		font-size: 2.4rem;
		font-weight: 900;
		text-align: right;
		color: var(--accent-amber);
	}

	.tv-attract-loop {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: space-between;
		text-align: center;
		padding: 2.5rem 3rem;
	}

	.attract-hero {
		display: flex;
		flex-direction: column;
		align-items: center;
	}

	.big-axe {
		font-size: 4rem;
		margin-bottom: 0.5rem;
	}
	.attract-title {
		font-size: 3.5rem;
		font-weight: 900;
		margin-bottom: 0.5rem;
		color: var(--accent-amber);
		letter-spacing: 0.02em;
	}
	.attract-sub {
		font-size: 1.4rem;
		color: var(--text-secondary);
		max-width: 800px;
	}

	.attract-qr-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 3rem;
		max-width: 1050px;
		width: 100%;
		margin: 1.5rem 0;
	}

	.qr-action-card {
		display: flex;
		align-items: center;
		gap: 1.5rem;
		padding: 1.5rem 2rem;
		background: rgba(18, 24, 38, 0.9);
		border: 2px solid var(--border-color);
		border-radius: var(--radius-lg);
		text-align: left;
	}

	.qr-card-info {
		flex: 1;
	}

	.qr-card-badge {
		display: inline-block;
		background: rgba(239, 68, 68, 0.2);
		color: #ef4444;
		border: 1px solid rgba(239, 68, 68, 0.4);
		font-size: 0.75rem;
		font-weight: 800;
		padding: 0.2rem 0.6rem;
		border-radius: 9999px;
		margin-bottom: 0.5rem;
		letter-spacing: 0.05em;
	}

	.qr-badge-food {
		background: rgba(16, 185, 129, 0.2);
		color: #10b981;
		border-color: rgba(16, 185, 129, 0.4);
	}

	.qr-card-title {
		font-size: 1.6rem;
		font-weight: 900;
		color: #ffffff;
		margin-bottom: 0.4rem;
	}

	.qr-card-desc {
		font-size: 0.95rem;
		color: var(--text-secondary);
		line-height: 1.4;
	}

	.attract-safety-bar {
		display: flex;
		justify-content: center;
		gap: 2.5rem;
		background: rgba(0, 0, 0, 0.5);
		border: 1px solid rgba(255, 255, 255, 0.1);
		padding: 0.75rem 2rem;
		border-radius: 9999px;
	}

	.safety-rule {
		font-size: 0.95rem;
		font-weight: 800;
		color: var(--accent-amber);
		letter-spacing: 0.05em;
	}

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

	.bullseye-vfx .vfx-title {
		color: #f59e0b;
		text-shadow: 0 0 40px rgba(245, 158, 11, 0.8);
	}
	.clutch-vfx .vfx-title {
		color: #06b6d4;
		text-shadow: 0 0 40px rgba(6, 182, 212, 0.8);
	}

	.vfx-points {
		font-size: 3rem;
		font-weight: 900;
		color: #ffffff;
		margin-top: 1rem;
	}

	@keyframes pop-in {
		0% {
			transform: scale(0.7);
			opacity: 0;
		}
		100% {
			transform: scale(1);
			opacity: 1;
		}
	}
</style>
