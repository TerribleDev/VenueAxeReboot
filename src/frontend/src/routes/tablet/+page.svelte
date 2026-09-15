<script lang="ts">
	import { onMount, onDestroy } from "svelte";
	import WatlTarget from "$lib/components/WatlTarget.svelte";
	import MatchPodiumSummary from "$lib/components/MatchPodiumSummary.svelte";
	import GameRulesModal from "$lib/components/GameRulesModal.svelte";
	import {
		postApiLanesTerminalsPair,
		postApiLanesOperationsByLaneIdThrow,
		postApiLanesOperationsByLaneIdUndo,
		postApiLanesOperationsByLaneIdSkipTurn,
		postApiLanesOperationsByLaneIdStartSession,
	} from "$lib/api/client";
	import { laneSignalR } from "$lib/services/signalr";
	import type {
		GameStateSnapshot,
		TerminalAuthResult,
	} from "$lib/api/generated/types.gen";

	let pairingCode = $state("100001");
	let terminalAuth = $state<TerminalAuthResult | null>(null);
	let gameState = $state<GameStateSnapshot | null>(null);
	let showGameRulesModal = $state(false);
	let activeSessionData = $state<any>(null);
	let inLobby = $state(false);
	let sessionRemainingSeconds = $state(0);
	let timerInterval: any = null;
	let isClutchArmed = $state(false);
	let isPairing = $state(false);
	let safetyAlert = $state<string | null>(null);
	let lastThrowResult = $state<any>(null);

	let hitBadgeText = $state<string | null>(null);
	let hitBadgeType = $state<string>("");
	let hitBadgeTimer: any = null;

	function triggerHitBadge(
		points: number,
		isBull = false,
		isKill = false,
		isDrop = false,
		isFault = false,
	) {
		if (hitBadgeTimer) clearTimeout(hitBadgeTimer);
		if (isKill || points === 8) {
			hitBadgeText = "+8 KILLSHOT! 🎯";
			hitBadgeType = "hit-killshot";
		} else if (isBull || points === 6) {
			hitBadgeText = "+6 BULLSEYE! 🎯";
			hitBadgeType = "hit-bullseye";
		} else if (isFault) {
			hitBadgeText = "FAULT (0 PTS) ⚠️";
			hitBadgeType = "hit-fault";
		} else if (isDrop) {
			hitBadgeText = "DROP (0 PTS) ❌";
			hitBadgeType = "hit-drop";
		} else if (points > 0) {
			hitBadgeText = `+${points} POINTS!`;
			hitBadgeType = "hit-points";
		} else {
			hitBadgeText = "MISS (0 PTS)";
			hitBadgeType = "hit-miss";
		}
		hitBadgeTimer = setTimeout(() => {
			hitBadgeText = null;
		}, 900);
	}

	onMount(async () => {
		const saved = localStorage.getItem("venueaxe_tablet_auth");
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
		if (timerInterval) clearInterval(timerInterval);
		laneSignalR.disconnect();
	});

	function startSessionTimer(expiresAtIso: string) {
		if (timerInterval) clearInterval(timerInterval);
		const update = () => {
			if (!expiresAtIso) {
				sessionRemainingSeconds = 0;
				return;
			}
			const diffMs = new Date(expiresAtIso).getTime() - Date.now();
			sessionRemainingSeconds = Math.max(0, Math.floor(diffMs / 1000));
		};
		update();
		timerInterval = setInterval(update, 1000);
	}

	function formatTimer(seconds: number): string {
		const mins = Math.floor(seconds / 60);
		const secs = seconds % 60;
		if (mins >= 60) {
			const hrs = Math.floor(mins / 60);
			const remMins = mins % 60;
			return `${hrs}:${remMins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
		}
		return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
	}

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
				if (session && session.currentGame) {
					const isNewSession = !activeSessionData || activeSessionData.sessionId !== session.sessionId;
					activeSessionData = session;
					gameState = session.currentGame;
					startSessionTimer(session.expiresAt);
					if (isNewSession) {
						inLobby = true;
					}
					return;
				}
			}
			activeSessionData = null;
			gameState = null;
			inLobby = false;
			if (timerInterval) clearInterval(timerInterval);
		} catch (e) {
			console.error("Failed to load active session:", e);
		}
	}

	async function handlePair() {
		isPairing = true;
		try {
			const res = await postApiLanesTerminalsPair({
				body: { pairingCode, terminalType: "Tablet" },
			});
			if (res.data) {
				terminalAuth = res.data;
				localStorage.setItem(
					"venueaxe_tablet_auth",
					JSON.stringify(terminalAuth),
				);
				await initSignalR(terminalAuth.laneId);
				await loadActiveSession(terminalAuth.laneId);
			} else {
				alert("Invalid Pairing PIN");
			}
		} catch (e) {
			alert("Failed to pair tablet terminal");
		} finally {
			isPairing = false;
		}
	}

	async function initSignalR(laneId: string) {
		laneSignalR.onStateChanged = async (_laneOrId: any, status?: string) => {
			const s = String(status ?? '').toLowerCase();
			if (s === 'turnaround' || s === 'available' || s === 'completed') {
				gameState = null;
				activeSessionData = null;
				inLobby = false;
				if (timerInterval) clearInterval(timerInterval);
			} else if (terminalAuth?.laneId) {
				await loadActiveSession(terminalAuth.laneId);
			}
		};

		laneSignalR.onThrowRecorded = async (state) => {
			gameState = state;
			if (!activeSessionData && terminalAuth?.laneId) {
				await loadActiveSession(terminalAuth.laneId);
			}
			if (state.lastThrow) {
				lastThrowResult = {
					x: Number(state.lastThrow.x ?? 0),
					y: Number(state.lastThrow.y ?? 0),
					pointsAwarded: Number(state.lastThrow.pointsAwarded ?? 0),
					zone: String(state.lastThrow.zone ?? ""),
				};
				triggerHitBadge(
					Number(state.lastThrow.pointsAwarded ?? 0),
					Boolean(state.lastThrow.isBullseye),
					Boolean(state.lastThrow.isKillshot),
					String(state.lastThrow.zone).toLowerCase() === "drop",
					String(state.lastThrow.zone).toLowerCase() === "fault",
				);
			}
		};

		laneSignalR.onSessionExtended = (extraMinutes) => {
			if (activeSessionData && activeSessionData.expiresAt) {
				const currentExp = new Date(activeSessionData.expiresAt).getTime();
				const newExp = new Date(currentExp + Number(extraMinutes) * 60000).toISOString();
				activeSessionData.expiresAt = newExp;
				startSessionTimer(newExp);
			} else if (terminalAuth?.laneId) {
				loadActiveSession(terminalAuth.laneId);
			}
		};

		laneSignalR.onSafetyStopActivated = (reason) => {
			safetyAlert = reason;
		};

		try {
			await laneSignalR.connect(laneId);
		} catch (e) {
			console.error("SignalR connect error:", e);
		}
	}

	async function handleTargetThrow(payload: {
		x: number;
		y: number;
		isClutchCalled: boolean;
		manualZone?: number;
		isLineBreak?: boolean;
	}) {
		if (!terminalAuth) return;

		try {
			const res = await postApiLanesOperationsByLaneIdThrow({
				path: { laneId: terminalAuth.laneId },
				body: {
					x: payload.manualZone ? null : payload.x,
					y: payload.manualZone ? null : payload.y,
					isClutchCalled: payload.isClutchCalled,
					manualZone: payload.manualZone
						? (payload.manualZone as any)
						: null,
				},
			});
			if (res.data) {
				gameState = res.data;
				isClutchArmed = false;
				if (res.data.lastThrow) {
					lastThrowResult = {
						x: Number(res.data.lastThrow.x ?? 0),
						y: Number(res.data.lastThrow.y ?? 0),
						pointsAwarded: Number(res.data.lastThrow.pointsAwarded ?? 0),
						zone: String(res.data.lastThrow.zone ?? ""),
					};
					triggerHitBadge(
						Number(res.data.lastThrow.pointsAwarded ?? 0),
						Boolean(res.data.lastThrow.isBullseye),
						Boolean(res.data.lastThrow.isKillshot),
						String(res.data.lastThrow.zone).toLowerCase() === "drop",
						String(res.data.lastThrow.zone).toLowerCase() === "fault",
					);
				}
			}
		} catch (e) {
			console.error(e);
		}
	}

	async function handleManualScore(
		points: number,
		isBullseye = false,
		isKill = false,
		isDrop = false,
		isFault = false,
	) {
		if (!terminalAuth) return;

		// When Killshot is called, Bullseye cannot be selected
		if (isClutchArmed && (points === 6 || isBullseye)) {
			return;
		}

		let zone = 0;
		if (isKill || (isClutchArmed && points === 8))
			zone = 7; // Kill Left or Right
		else if (points === 6 || isBullseye) zone = 6;
		else if (points === 5) zone = 5;
		else if (points === 4) zone = 4;
		else if (points === 3) zone = 3;
		else if (points === 2) zone = 2;
		else if (points === 1) zone = 1;
		else if (isDrop)
			zone = 10; // Drop
		else if (isFault)
			zone = 99; // Fault
		else zone = 0; // Miss / 0

		try {
			const res = await postApiLanesOperationsByLaneIdThrow({
				path: { laneId: terminalAuth.laneId },
				body: {
					x: null,
					y: null,
					manualZone: zone as any,
					isClutchCalled: isClutchArmed || isKill,
				},
			});
			if (res.data) {
				gameState = res.data;
				isClutchArmed = false;
				triggerHitBadge(points, isBullseye, isKill, isDrop, isFault);
			}
		} catch (e) {
			console.error(e);
		}
	}

	function toggleKillshot() {
		const idx = Number(gameState?.currentPlayerIndex ?? 0);
		const activePlayer = gameState?.players?.[idx];
		const remaining = Number(activePlayer?.killsRemaining ?? 2);
		if (remaining <= 0 && !isClutchArmed) {
			alert("All 2 killshots have already been used for this match!");
			return;
		}
		isClutchArmed = !isClutchArmed;
	}

	let isUndoing = $state(false);
	async function handleUndo() {
		if (!terminalAuth?.laneId || isUndoing) return;
		isUndoing = true;
		try {
			const res = await postApiLanesOperationsByLaneIdUndo({
				path: { laneId: terminalAuth.laneId },
			});
			if (res.data) {
				gameState = res.data;
				lastThrowResult = res.data.lastThrow ?? null;
			}
		} catch (e) {
			console.error("Failed to undo throw:", e);
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
				path: { laneId: terminalAuth.laneId },
			});
			if (res.data) {
				gameState = res.data;
				lastThrowResult = res.data.lastThrow ?? null;
			}
		} catch (e) {
			console.error("Failed to skip turn:", e);
		} finally {
			isSkipping = false;
		}
	}

	let isRematching = $state(false);
	async function handleRematch() {
		if (!terminalAuth?.laneId || isRematching) return;
		isRematching = true;
		try {
			const res = await fetch(
				`/api/lanes/operations/${terminalAuth.laneId}/rematch`,
				{
					method: "POST",
					credentials: "include",
				},
			);
			if (res.ok) {
				const newState = await res.json();
				gameState = newState;
				lastThrowResult = null;
				isClutchArmed = false;
			}
		} catch (e) {
			console.error("Failed to rematch:", e);
		} finally {
			isRematching = false;
		}
	}

	// Derived Tic-Tac-Toe 3x3 Grid ('X', 'O', or null)
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

	// Change Game Modal (Pit 5)
	let showSwitchGameModal = $state(false);
	let isSwitchingGame = $state(false);
	const gameCatalog = [
		{
			id: "watl_standard",
			name: "WATL Regulation",
			desc: "10 rounds, 2 killshots anytime (8 pts)",
		},
		{
			id: "axe_tictactoe",
			name: "Axe Tic-Tac-Toe",
			desc: "3x3 territory battle on the target board",
		},
		{
			id: "countdown_603",
			name: "Countdown 603",
			desc: "Exact zero victory from 603 with bust protection",
		},
		{
			id: "first_to_21",
			name: "First to 21",
			desc: "Hit 21 exactly — bust resets to 13",
		},
		{
			id: "around_the_world",
			name: "Around The World",
			desc: "Ascending ring progression 1 through 7",
		},
		{
			id: "kill_hunter",
			name: "Killshot Hunter",
			desc: "Only Killshots (8) and Bullseyes (6) score",
		},
	];

	async function handleSwitchGame(newGameTypeId: string) {
		if (!terminalAuth?.laneId || isSwitchingGame) return;
		isSwitchingGame = true;
		try {
			const res = await fetch(
				`/api/lanes/operations/${terminalAuth.laneId}/switch-game`,
				{
					method: "POST",
					headers: { "Content-Type": "application/json" },
					credentials: "include",
					body: JSON.stringify({ gameTypeId: newGameTypeId }),
				},
			);
			if (res.ok) {
				gameState = await res.json();
				lastThrowResult = null;
				isClutchArmed = false;
				showSwitchGameModal = false;
			} else {
				alert("Failed to switch game mode.");
			}
		} catch (e) {
			console.error("Failed to switch game:", e);
		} finally {
			isSwitchingGame = false;
		}
	}

	// End Session Modal Safeguard (Pit 4)
	let showEndSessionModal = $state(false);
	let isEndingSession = $state(false);
	let coachPinInput = $state("");
	let endSessionError = $state<string | null>(null);

	async function confirmEndSession() {
		if (!terminalAuth?.laneId || isEndingSession) return;
		if (coachPinInput && coachPinInput !== "1234") {
			endSessionError = "Invalid Coach PIN (default is 1234)";
			return;
		}
		isEndingSession = true;
		endSessionError = null;
		try {
			const res = await fetch(
				`/api/lanes/operations/${terminalAuth.laneId}/end-session`,
				{
					method: "POST",
					credentials: "include",
				},
			);
			if (res.ok) {
				gameState = null;
				lastThrowResult = null;
				showEndSessionModal = false;
				coachPinInput = "";
			} else {
				endSessionError = "Failed to end session.";
			}
		} catch (e) {
			console.error("Failed to end session:", e);
			endSessionError = "Network error while ending session.";
		} finally {
			isEndingSession = false;
		}
	}

	// Pre-Session Lobby state (Pit 3)
	let lobbyP1 = $state("Player 1");
	let lobbyP2 = $state("Player 2");
	let lobbyGame = $state("watl_standard");
	let isLaunchingLobby = $state(false);

	async function handleLaunchLobbySession() {
		if (!terminalAuth?.laneId || isLaunchingLobby) return;
		isLaunchingLobby = true;
		try {
			const res = await fetch(
				`/api/lanes/operations/${terminalAuth.laneId}/start-session`,
				{
					method: "POST",
					headers: { "Content-Type": "application/json" },
					credentials: "include",
					body: JSON.stringify({
						sessionTitle: `${lobbyP1} vs ${lobbyP2}`,
						durationMinutes: 60,
						gameTypeId: lobbyGame,
						initialRoster: [
							{ name: lobbyP1.trim() || "Player 1" },
							{ name: lobbyP2.trim() || "Player 2" },
						],
					}),
				},
			);
			if (res.ok) {
				const summary = await res.json();
				if (summary?.currentGame) {
					gameState = summary.currentGame;
				}
			} else {
				alert("Failed to launch match. Please check lane status.");
			}
		} catch (e) {
			console.error("Failed to start session from lobby:", e);
		} finally {
			isLaunchingLobby = false;
		}
	}

	// Player substitution modal state
	let showSubstituteModal = $state(false);
	let subPlayerId = $state("");
	let subNewName = $state("");
	let isSubmittingSub = $state(false);

	function openSubstituteModal(player: any) {
		subPlayerId = player.id;
		subNewName = player.name;
		showSubstituteModal = true;
	}

	async function submitSubstitution() {
		if (
			!terminalAuth?.laneId ||
			!subPlayerId ||
			!subNewName.trim() ||
			isSubmittingSub
		)
			return;
		isSubmittingSub = true;
		try {
			const res = await fetch(
				`/api/lanes/operations/${terminalAuth.laneId}/substitute`,
				{
					method: "POST",
					headers: { "Content-Type": "application/json" },
					credentials: "include",
					body: JSON.stringify({
						playerId: subPlayerId,
						newName: subNewName.trim(),
					}),
				},
			);
			if (res.ok) {
				showSubstituteModal = false;
				await loadActiveSession(terminalAuth.laneId);
			} else {
				alert("Failed to substitute player.");
			}
		} catch (e) {
			console.error(e);
		} finally {
			isSubmittingSub = false;
		}
	}

	function resetPairing() {
		if (timerInterval) clearInterval(timerInterval);
		localStorage.removeItem("venueaxe_tablet_auth");
		terminalAuth = null;
		activeSessionData = null;
		gameState = null;
		inLobby = false;
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
				<p class="subtitle">
					Enter the 6-digit Pairing PIN displayed on the Lane
					Management dashboard.
				</p>
			</div>

			<div class="form-group">
				<label class="form-label" for="pairing-code">Pairing PIN</label>
				<input
					id="pairing-code"
					type="text"
					class="form-input pin-input font-display"
					bind:value={pairingCode}
					placeholder="100001"
					maxlength="6"
				/>
			</div>

			<button
				class="btn btn-primary btn-block"
				disabled={isPairing}
				onclick={handlePair}
			>
				{isPairing ? "Connecting..." : "Connect to Lane Terminal"}
			</button>
		</div>
	{:else if !gameState || !gameState.players || gameState.players.length === 0}
		<!-- IDLE SCREEN: WHEN THERE IS NO SESSION, JUST SHOW THE LANE NUMBER -->
		<div class="idle-container">
			<div class="idle-header">
				<div class="idle-pill">
					<span class="pulse-dot"></span>
					<span>TERMINAL PAIRED</span>
				</div>
				<button type="button" class="btn-disconnect" onclick={resetPairing}>Unpair</button>
			</div>

			<div class="idle-main">
				{#if terminalAuth.venueIconUrl}
					<img src={terminalAuth.venueIconUrl} alt={terminalAuth.venueName || 'Venue'} style="width: 72px; height: 72px; border-radius: 16px; object-fit: contain; margin-bottom: 0.75rem; background: rgba(15, 23, 42, 0.6); border: 1.5px solid rgba(255, 255, 255, 0.15); padding: 4px;" />
				{:else}
					<div class="idle-axe-icon">🪓</div>
				{/if}
				<h1 class="idle-lane-number font-display">{terminalAuth.laneName}</h1>
				{#if activeSessionData?.sessionTitle}
					<div class="font-display" style="font-size: 1.25rem; font-weight: 800; color: var(--accent-amber); margin: 0.5rem 0 0.25rem; letter-spacing: 0.05em; text-transform: uppercase;">
						🎯 {activeSessionData.sessionTitle}
					</div>
				{/if}
				<div class="idle-status-line">READY FOR MATCH</div>
				<p class="idle-status-sub">Matches are launched by lane coaches from the Lane Management console.</p>
			</div>
		</div>
	{:else}
		<!-- ACTIVE CONSOLE -->
		<div class="console-layout">
			<!-- Top HUD Bar -->
			<div class="hud-bar">
				<div class="hud-lane">
					{#if terminalAuth.venueIconUrl}
						<img src={terminalAuth.venueIconUrl} alt={terminalAuth.venueName || 'Venue'} style="width: 28px; height: 28px; border-radius: 6px; object-fit: contain; vertical-align: middle; border: 1px solid rgba(255, 255, 255, 0.2); margin-right: 0.4rem;" />
					{/if}
					<span class="badge badge-active">{terminalAuth.laneName}</span>
					{#if activeSessionData?.sessionTitle}
						<span class="session-badge font-display" style="color: var(--accent-amber); font-weight: 700; font-size: 0.85rem; padding: 0.15rem 0.5rem; background: rgba(245, 158, 11, 0.15); border-radius: 4px; border: 1px solid rgba(245, 158, 11, 0.3);">
							🎯 {activeSessionData.sessionTitle}
						</span>
					{/if}
					{#if gameState}
						<span class="game-title font-display">{gameState.gameName}</span>
					{/if}
				</div>

				<!-- Live Session Countdown Timer (ticking continuously) -->
				<div class="hud-timer font-display" class:timer-urgent={sessionRemainingSeconds < 300}>
					<span class="timer-icon">⏱️</span>
					<span class="timer-digits">{formatTimer(sessionRemainingSeconds)}</span>
				</div>

				<div class="hud-status">
					{#if inLobby}
						<button
							type="button"
							class="btn btn-primary btn-xs font-display"
							onclick={() => (inLobby = false)}
						>
							🎯 Enter Match
						</button>
					{:else}
						<span class="round-counter font-display">
							Rnd {gameState.currentRound}/{gameState.totalRounds}
						</span>
						<button
							type="button"
							class="btn btn-secondary btn-xs font-display"
							onclick={() => (inLobby = true)}
							title="View Lobby and Thrower Roster"
						>
							📋 Lobby
						</button>
						<button
							type="button"
							class="btn btn-secondary btn-xs font-display"
							onclick={() => (showGameRulesModal = true)}
							title="View Game Rules & Scoring"
						>
							❓ Rules
						</button>
						<button
							type="button"
							class="btn btn-secondary btn-xs font-display"
							onclick={() => (showSwitchGameModal = true)}
							title="Switch Active Game Engine"
						>
							🎮 Game
						</button>
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
						<p>
							{safetyAlert} - Please step behind the safety line.
						</p>
					</div>
					<button
						class="btn btn-secondary btn-sm"
						onclick={() => (safetyAlert = null)}>Dismiss</button
					>
				</div>
			{/if}

			<!-- Low Time Warning Banner (Within 5 minutes of lane closing) -->
			{#if sessionRemainingSeconds > 0 && sessionRemainingSeconds <= 300}
				<div class="low-time-warning-banner">
					<span class="warning-icon pulse-alert">⏳</span>
					<div class="warning-text">
						<strong class="font-display">LOW TIME WARNING: UNDER {Math.ceil(sessionRemainingSeconds / 60)} MINUTES REMAINING ({formatTimer(sessionRemainingSeconds)})</strong>
						<span>Session closing soon. Please wrap up your match or notify your lane coach to extend.</span>
					</div>
				</div>
			{/if}

			{#if inLobby}
				<!-- LOBBY VIEW (WHEN SESSION STARTS) -->
				<div class="lobby-panel glass-panel">
					<div class="lobby-header">
						<div class="lobby-badge">SESSION ACTIVE</div>
						<h2 class="font-display lobby-title">{activeSessionData?.sessionTitle || 'Match Session Lobby'}</h2>
						<div class="lobby-timer-card font-display" class:timer-urgent={sessionRemainingSeconds < 300}>
							<span class="lobby-timer-label">SESSION TIME REMAINING</span>
							<span class="lobby-timer-clock">⏱️ {formatTimer(sessionRemainingSeconds)}</span>
							<span class="lobby-timer-sub">Timer ticking down from session start</span>
						</div>
					</div>

					<div class="lobby-grid">
						<!-- Thrower Roster -->
						<div class="lobby-section">
							<div class="lobby-section-header">
								<h4 class="section-heading">Thrower Roster ({gameState.players.length})</h4>
								<span class="lobby-hint-text">Tap ✏️ to rename thrower</span>
							</div>
							<div class="lobby-roster-list">
								{#each gameState.players as p, idx}
									<div class="lobby-player-card">
										<div class="lobby-player-avatar" style="background-color: {p.avatarColor ?? '#f59e0b'};">
											{(p.name ?? 'T').charAt(0)}
										</div>
										<div class="lobby-player-info">
											<span class="lobby-player-order">Thrower {idx + 1}</span>
											<span class="lobby-player-name">{p.name}</span>
										</div>
										<button
											type="button"
											class="btn-rename-player"
											onclick={() => openSubstituteModal(p)}
											title="Rename thrower"
											aria-label="Rename thrower"
										>
											✏️
										</button>
									</div>
								{/each}
							</div>
						</div>

						<!-- Selected Game Mode -->
						<div class="lobby-section">
							<div class="lobby-section-header">
								<h4 class="section-heading">Game Mode</h4>
								<button
									type="button"
									class="btn btn-secondary btn-xs font-display"
									onclick={() => (showSwitchGameModal = true)}
								>
									Change Game
								</button>
							</div>

							<div class="lobby-mode-card">
								<div class="lobby-mode-icon">🎯</div>
								<div class="lobby-mode-content">
									<h3 class="lobby-mode-title font-display">{gameState.gameName}</h3>
									<p class="lobby-mode-desc">
										{#if gameState.gameTypeId === 'countdown' || gameState.gameTypeId === 'countdown_603' || gameState.gameTypeId === 'countdown_301'}
											Countdown 603 match. Race to deduct points and hit exact zero!
										{:else if gameState.gameTypeId === 'axe-blackjack'}
											Aim for 21 without busting! High card risk and precision throws.
										{:else if gameState.gameTypeId === 'axe-tic-tac-toe'}
											Hit target zones to claim territory in a 3x3 battle grid.
										{:else}
											10-round official WATL match scoring (Bullseye 6, Rings 5-1, Clutch 7/8).
										{/if}
									</p>
									<div class="lobby-mode-pills">
										<span class="mode-pill">{gameState.totalRounds} Rounds</span>
										<span class="mode-pill">Live Telemetry</span>
										<span class="mode-pill">Overhead TV Synced</span>
										<button
											type="button"
											class="btn-rules-icon font-display"
											style="padding: 0.15rem 0.55rem; font-size: 0.75rem;"
											onclick={() => (showGameRulesModal = true)}
											title="View Game Rules"
										>
											❓ View Rules
										</button>
									</div>
								</div>
							</div>
						</div>
					</div>

					<div class="lobby-footer">
						<button
							type="button"
							class="btn btn-primary btn-launch-match font-display"
							onclick={() => (inLobby = false)}
						>
							🎯 Start Throwing / Enter Game
						</button>
					</div>
				</div>
			{:else if gameState.status === 2 || String(gameState.status).toLowerCase() === "finished"}
					<!-- POST-MATCH PODIUM & SCATTER HEATMAP SUMMARY -->
					<MatchPodiumSummary
						{gameState}
						onrematch={handleRematch}
						onundo={handleUndo}
						onswitchformat={() => (showSwitchGameModal = true)}
					/>
				{:else}
					{@const activeIdx = Number(
						gameState.currentPlayerIndex ?? 0,
					)}
					{@const activePlayer =
						gameState.players[activeIdx] ?? gameState.players[0]}

					<!-- Active Thrower Banner -->
					<div class="player-banner">
						<div class="player-identity">
							<div
								class="player-avatar"
								style="background-color: {activePlayer.avatarColor ??
									'#f59e0b'}"
							>
								{(activePlayer.name ?? "T").charAt(0)}
							</div>
							<div>
								<div class="up-next-label">CURRENT THROWER</div>
								<h2 class="player-name font-display">
									{activePlayer.name}
								</h2>
							</div>
						</div>

						<div class="player-stats">
							<div class="stat-box">
								<span class="stat-val font-display"
									>{activePlayer.score ?? 0}</span
								>
								<span class="stat-lbl">
									{#if gameState.gameTypeId === "around_the_world"}
										Step / 7
									{:else if gameState.gameTypeId === "axe_tictactoe"}
										Tiles Claimed
									{:else}
										Points
									{/if}
								</span>
							</div>
							{#if gameState.gameTypeId === "around_the_world"}
								{@const steps = [
									"Ring 1",
									"Ring 2",
									"Ring 3",
									"Ring 4",
									"Ring 5",
									"Bullseye",
									"Clutch",
								]}
								{@const pScore = Number(
									activePlayer.score ?? 0,
								)}
								<div class="stat-box arcade-objective">
									<span
										class="stat-val font-display"
										style="color: var(--color-cyan, #06b6d4);"
									>
										{steps[Math.min(pScore, 6)]}
									</span>
									<span class="stat-lbl">Target Needed</span>
								</div>
							{:else if gameState.gameTypeId === "blackjack_21" || gameState.gameTypeId === "first_to_21"}
								{@const pScore = Number(
									activePlayer.score ?? 0,
								)}
								<div class="stat-box arcade-objective">
									<span
										class="stat-val font-display"
										style="color: {21 - pScore <= 5
											? '#f59e0b'
											: '#10b981'};"
									>
										{21 - pScore > 0 ? 21 - pScore : "21!"}
									</span>
									<span class="stat-lbl">To Reach 21</span>
								</div>
							{:else}
								<div class="stat-box">
									<span class="stat-val font-display"
										>{activePlayer.streak ?? 0}🔥</span
									>
									<span class="stat-lbl">Streak</span>
								</div>
							{/if}
							<button
								type="button"
								class="quick-undo-btn font-display"
								disabled={isUndoing ||
									!gameState.allThrows ||
									gameState.allThrows.length === 0}
								onclick={handleUndo}
								title="Undo last throw immediately"
							>
								{isUndoing ? "⏳..." : "↩️ Undo"}
							</button>
						</div>
					</div>

					<!-- Main Interactive Scoring Arena -->
					<div class="arena-grid">
						<!-- Interactive WATL SVG Target -->
						<div class="target-card glass-panel" style="position: relative;">
							{#if hitBadgeText}
								<div class="hit-toast-badge font-display {hitBadgeType}">
									{hitBadgeText}
								</div>
							{/if}
							<WatlTarget
								interactive={true}
								isClutchCalled={isClutchArmed}
								lastThrow={lastThrowResult}
								overlayMode={gameState.gameTypeId ===
								"axe_tictactoe"
									? "tic_tac_toe"
									: "standard"}
								{tttGrid}
								onthrow={handleTargetThrow}
							/>
							<p class="target-hint">
								🎯 Tap the exact spot on the board where the axe
								landed
							</p>
						</div>

						<!-- Tactile Control Console -->
						<div class="controls-card glass-panel">
							<div class="controls-header-row">
								<h3 class="controls-title font-display">
									Touch Scoring
								</h3>
								<div class="quick-match-btns">
									<button
										type="button"
										class="btn-micro"
										onclick={handleRematch}
										title="Restart match with same throwers"
									>
										<span>🔄</span>
										<span>Rematch</span>
									</button>
									<button
										type="button"
										class="btn-micro btn-micro-cyan"
										onclick={() =>
											(showSwitchGameModal = true)}
										title="Switch active game mode"
									>
										<span>🎮</span>
										<span>Game</span>
									</button>
								</div>
							</div>

							<!-- CALL KILLSHOT BUTTON (2 anytime per player, 8 pts) -->
							<button
								class="btn btn-clutch"
								class:armed={isClutchArmed}
								disabled={Number(
									gameState.players[activeIdx]
										?.killsRemaining ?? 2,
								) <= 0 && !isClutchArmed}
								onclick={toggleKillshot}
							>
								⚡ {isClutchArmed
									? "🎯 KILLSHOT ARMED (8 PTS)"
									: Number(
												gameState.players[activeIdx]
													?.killsRemaining ?? 2,
										  ) > 0
										? `🎯 CALL KILLSHOT (8 PTS) [${gameState.players[activeIdx]?.killsRemaining ?? 2} Left]`
										: "🎯 KILLSHOTS EXHAUSTED [0 Left]"}
							</button>

							<!-- Number Scoring Grid: 6, 5, 4, 3, 2, 1, 0, Drop, Miss, Fault, and Undo -->
							<div class="touch-keypad watl-touch-keypad">
								{#if isClutchArmed}
									<button
										class="key-btn key-kill"
										onclick={() =>
											handleManualScore(8, false, true)}
									>
										8<small>Killshot</small>
									</button>
								{/if}
								<button
									class="key-btn key-bull"
									class:btn-disabled={isClutchArmed}
									disabled={isClutchArmed}
									title={isClutchArmed
										? "Bullseye cannot be selected when Killshot is called"
										: "Bullseye (6 pts)"}
									onclick={() => handleManualScore(6, true)}
								>
									6<small>{isClutchArmed ? "Disabled" : "Bull"}</small>
								</button>
								<button
									class="key-btn"
									onclick={() => handleManualScore(5)}
									>5</button
								>
								<button
									class="key-btn"
									onclick={() => handleManualScore(4)}
									>4</button
								>
								<button
									class="key-btn"
									onclick={() => handleManualScore(3)}
									>3</button
								>
								<button
									class="key-btn"
									onclick={() => handleManualScore(2)}
									>2</button
								>
								<button
									class="key-btn"
									onclick={() => handleManualScore(1)}
									>1</button
								>
								<button
									class="key-btn"
									onclick={() => handleManualScore(0)}
									>0</button
								>
								<button
									class="key-btn key-drop"
									onclick={() =>
										handleManualScore(
											0,
											false,
											false,
											true,
											false,
										)}
								>
									<small>Drop</small>
								</button>
								<button
									class="key-btn key-miss"
									onclick={() =>
										handleManualScore(
											0,
											false,
											false,
											false,
											false,
										)}
								>
									<small>Miss</small>
								</button>
								<button
									class="key-btn key-fault"
									onclick={() =>
										handleManualScore(
											0,
											false,
											false,
											false,
											true,
										)}
								>
									<small>Fault</small>
								</button>
								<button
									class="key-btn key-undo"
									disabled={isUndoing ||
										!gameState.allThrows ||
										gameState.allThrows.length === 0}
									onclick={handleUndo}
									title="Undo previous throw and revert turn"
								>
									<span>↩️</span>
									<small>{isUndoing ? "Undoing..." : "Undo Throw"}</small>
								</button>
							</div>

							<!-- Match Leaderboard Mini (Horizontal / Responsive Pills) -->
							<div class="mini-roster">
								<div class="roster-header-row">
									<h4 class="roster-title font-display">
										Thrower Leaderboard
									</h4>
									<span class="kills-quota-hint"
										>2 Kills Max</span
									>
								</div>
								<div class="roster-pills-list">
									{#each gameState.players as p, idx (p.id ?? idx)}
										<div
											class="roster-pill"
											class:active-row={idx === activeIdx}
										>
											<div class="roster-p-info">
												<span class="p-name">{p.name}</span>
												<span class="p-kills-tag"
													>{p.killsRemaining ?? 2}k</span
												>
											</div>
											<div class="roster-p-right">
												<span class="p-score font-display"
													>{p.score ?? 0}</span
												>
												<button
													type="button"
													class="btn-rename-player"
													onclick={() =>
														openSubstituteModal(p)}
													title="Rename thrower"
													aria-label="Rename thrower"
													>✏️</button
												>
											</div>
										</div>
									{/each}
								</div>
							</div>
						</div>
					</div>
				{/if}
		</div>
	{/if}

	<!-- Rename Thrower Modal -->
	{#if showSubstituteModal}
		<div
			class="modal-backdrop"
			role="button"
			tabindex="0"
			onclick={() => (showSubstituteModal = false)}
			onkeydown={(e) => {
				if (e.key === "Escape") showSubstituteModal = false;
			}}
		>
			<div
				class="sub-modal-content glass-panel"
				role="dialog"
				aria-modal="true"
				tabindex="-1"
				onclick={(e) => e.stopPropagation()}
				onkeydown={(e) => e.stopPropagation()}
			>
				<div class="modal-header-row">
					<h3 class="font-display sub-modal-title">Rename Thrower</h3>
					<button
						type="button"
						class="btn-close"
						onclick={() => (showSubstituteModal = false)}
						title="Close">✕</button
					>
				</div>
				<p class="sub-modal-subtitle">
					Update this thrower's display name on the roster. Match scores and
					throw history will be retained.
				</p>
				<form onsubmit={(e) => { e.preventDefault(); submitSubstitution(); }}>
					<div class="form-group">
						<label class="form-label" for="sub-name"
							>Thrower Name</label
						>
						<input
							id="sub-name"
							class="form-input"
							bind:value={subNewName}
							placeholder="Enter thrower name"
							required
						/>
					</div>
					<div class="modal-actions">
						<button
							type="button"
							class="btn btn-secondary font-display"
							onclick={() => (showSubstituteModal = false)}
							>Cancel</button
						>
						<button
							type="submit"
							class="btn btn-primary font-display"
							disabled={isSubmittingSub || !subNewName.trim()}
						>
							{isSubmittingSub ? "Saving..." : "Save Name"}
						</button>
					</div>
				</form>
			</div>
		</div>
	{/if}

	<!-- Switch Game Engine Modal -->
	{#if showSwitchGameModal}
		<div
			class="modal-backdrop"
			role="button"
			tabindex="0"
			onclick={() => (showSwitchGameModal = false)}
			onkeydown={(e) => {
				if (e.key === "Escape") showSwitchGameModal = false;
			}}
		>
			<div
				class="sub-modal-content switch-modal-content glass-panel"
				role="dialog"
				aria-modal="true"
				tabindex="-1"
				onclick={(e) => e.stopPropagation()}
				onkeydown={(e) => e.stopPropagation()}
			>
				<div class="modal-header-row">
					<h3 class="font-display sub-modal-title">
						Switch Game Engine
					</h3>
					<button
						type="button"
						class="btn-close"
						onclick={() => (showSwitchGameModal = false)}
						title="Close">✕</button
					>
				</div>
				<p class="sub-modal-subtitle">
					Switch the active target engine mid-session. Session timer
					and thrower roster will be seamlessly preserved.
				</p>
				<div class="games-selector-grid">
					{#each gameCatalog as g}
						<button
							type="button"
							class="game-card-btn"
							class:current-game={gameState?.gameTypeId === g.id}
							disabled={isSwitchingGame}
							onclick={() => handleSwitchGame(g.id)}
						>
							<div class="game-card-name">{g.name}</div>
							<div class="game-card-desc">{g.desc}</div>
							{#if gameState?.gameTypeId === g.id}
								<span class="badge-active-game">ACTIVE</span>
							{/if}
						</button>
					{/each}
				</div>
				<div class="modal-actions mt-4">
					<button
						type="button"
						class="btn btn-secondary font-display"
						onclick={() => (showSwitchGameModal = false)}
						>Cancel</button
					>
				</div>
			</div>
		</div>
	{/if}

	<GameRulesModal
		gameTypeId={gameState?.gameTypeId || 'watl_standard'}
		isOpen={showGameRulesModal}
		onClose={() => (showGameRulesModal = false)}
	/>
</div>

<style>
	.btn-rules-icon {
		background: rgba(245, 158, 11, 0.15);
		border: 1px solid rgba(245, 158, 11, 0.4);
		color: #f59e0b;
		padding: 0.25rem 0.65rem;
		border-radius: 9999px;
		font-size: 0.8rem;
		font-weight: 700;
		cursor: pointer;
		transition: all 0.15s ease;
		display: inline-flex;
		align-items: center;
		gap: 0.3rem;
	}

	.btn-rules-icon:hover {
		background: rgba(245, 158, 11, 0.3);
		border-color: #f59e0b;
		color: #fff;
		transform: translateY(-1px);
	}

	.tablet-viewport {
		max-width: 1200px;
		margin: 0 auto;
		padding: 0.45rem 0.75rem 0.75rem;
		min-height: 100vh;
		box-sizing: border-box;
	}

	.pair-card {
		max-width: 440px;
		margin: 2.5rem auto;
		padding: 2rem;
		text-align: center;
	}

	.pin-input {
		font-size: 1.8rem;
		text-align: center;
		letter-spacing: 0.16em;
		font-weight: 800;
		color: var(--accent-amber);
		width: 100%;
		max-width: 320px;
		margin-left: auto;
		margin-right: auto;
		padding: 0.65rem 1rem;
		box-sizing: border-box;
	}

	.console-layout {
		display: flex;
		flex-direction: column;
		gap: 0.45rem;
	}

	.hud-bar {
		display: flex;
		justify-content: space-between;
		align-items: center;
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		padding: 0.35rem 0.75rem;
		border-radius: var(--radius-sm);
		gap: 0.5rem;
	}

	.hud-lane {
		display: flex;
		align-items: center;
		gap: 0.65rem;
	}

	.game-title {
		font-size: 0.95rem;
		font-weight: 700;
		color: var(--text-primary);
	}

	.round-counter {
		font-size: 0.9rem;
		font-weight: 800;
		color: var(--accent-amber);
		margin-right: 0.35rem;
	}

	.btn-disconnect {
		background: transparent;
		border: 1px solid var(--border-color);
		color: var(--text-muted);
		padding: 0.25rem 0.5rem;
		border-radius: var(--radius-sm);
		font-size: 0.72rem;
		cursor: pointer;
	}

	.player-banner {
		background: linear-gradient(135deg, #182030, #10141f);
		border: 1px solid var(--border-highlight);
		border-radius: var(--radius-md);
		padding: 0.45rem 0.85rem;
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.player-identity {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}

	.player-avatar {
		width: 36px;
		height: 36px;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 1.15rem;
		font-weight: 900;
		color: #000;
		flex-shrink: 0;
	}

	.up-next-label {
		font-size: 0.62rem;
		font-weight: 700;
		color: var(--accent-amber);
		letter-spacing: 0.08em;
	}

	.player-name {
		font-size: 1.25rem;
		font-weight: 800;
		margin: 0;
		line-height: 1.2;
	}

	.player-stats {
		display: flex;
		align-items: center;
		gap: 1.25rem;
	}

	.stat-box {
		text-align: center;
	}

	.stat-val {
		font-size: 1.4rem;
		font-weight: 900;
		color: var(--accent-amber);
		display: block;
		line-height: 1;
	}

	.stat-lbl {
		font-size: 0.62rem;
		color: var(--text-secondary);
		text-transform: uppercase;
	}

	.arena-grid {
		display: grid;
		grid-template-columns: 1fr 1.05fr;
		gap: 0.65rem;
		align-items: start;
	}

	.target-card {
		padding: 0.55rem;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		border-radius: var(--radius-md);
		--target-max-size: min(440px, 54vh);
		--target-max-height: min(440px, 54vh);
	}

	.target-hint {
		font-size: 0.75rem;
		color: var(--text-secondary);
		margin-top: 0.35rem;
		text-align: center;
	}

	.controls-card {
		padding: 0.65rem 0.85rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		border-radius: var(--radius-md);
	}

	.controls-header-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 0.1rem;
	}

	.quick-match-btns {
		display: flex;
		gap: 0.45rem;
		align-items: center;
	}

	.btn-micro {
		background: linear-gradient(180deg, rgba(30, 41, 59, 0.85) 0%, rgba(15, 23, 42, 0.95) 100%);
		border: 1px solid rgba(255, 255, 255, 0.15);
		color: #f1f5f9;
		font-family: var(--font-display);
		font-size: 0.78rem;
		font-weight: 700;
		padding: 0.35rem 0.65rem;
		border-radius: 6px;
		cursor: pointer;
		display: inline-flex;
		align-items: center;
		gap: 0.3rem;
		transition: all 0.15s ease;
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
	}

	.btn-micro:hover {
		background: linear-gradient(180deg, rgba(51, 65, 85, 0.95) 0%, rgba(30, 41, 59, 1) 100%);
		border-color: rgba(245, 158, 11, 0.6);
		color: #ffffff;
	}

	.btn-micro:active {
		transform: translateY(1px);
	}

	.btn-micro-cyan {
		background: linear-gradient(180deg, rgba(6, 182, 212, 0.18) 0%, rgba(8, 51, 68, 0.45) 100%);
		border-color: rgba(6, 182, 212, 0.5);
		color: #67e8f9;
	}

	.btn-micro-cyan:hover {
		background: linear-gradient(180deg, rgba(6, 182, 212, 0.3) 0%, rgba(8, 51, 68, 0.7) 100%);
		border-color: var(--accent-cyan);
		color: #ffffff;
	}

	.controls-title {
		font-size: 0.95rem;
		font-weight: 800;
		letter-spacing: 0.05em;
		text-transform: uppercase;
		color: #ffffff;
		margin: 0;
	}

	.btn-clutch {
		background: rgba(6, 182, 212, 0.15);
		border: 1.5px solid var(--accent-cyan);
		color: var(--accent-cyan);
		font-family: var(--font-display);
		font-weight: 800;
		font-size: 0.9rem;
		padding: 0.45rem 0.75rem;
		border-radius: 8px;
		cursor: pointer;
		transition: all 0.15s ease;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.35rem;
	}

	.btn-clutch.armed {
		background: var(--accent-cyan);
		color: #000;
		box-shadow: 0 0 20px var(--accent-cyan-glow);
		animation: pulse-clutch 1s infinite alternate;
	}

	.touch-keypad {
		display: grid;
		grid-template-columns: repeat(4, 1fr);
		gap: 0.4rem;
	}

	.key-btn {
		background: linear-gradient(180deg, rgba(30, 41, 59, 0.7) 0%, rgba(15, 23, 42, 0.85) 100%);
		border: 1px solid rgba(255, 255, 255, 0.12);
		color: var(--text-primary);
		font-family: var(--font-display);
		font-size: 1.25rem;
		font-weight: 800;
		padding: 0.45rem 0;
		min-height: 44px;
		border-radius: 8px;
		cursor: pointer;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.25);
		transition: all 0.12s cubic-bezier(0.4, 0, 0.2, 1);
		user-select: none;
	}

	.key-btn small {
		font-size: 0.58rem;
		font-weight: 700;
		color: var(--text-muted);
		text-transform: uppercase;
		letter-spacing: 0.04em;
		margin-top: 0.05rem;
	}

	.key-btn:hover {
		background: linear-gradient(180deg, rgba(51, 65, 85, 0.85) 0%, rgba(30, 41, 59, 0.95) 100%);
		border-color: rgba(255, 255, 255, 0.3);
		transform: translateY(-1px);
	}

	.key-btn:active {
		transform: translateY(1px);
	}

	.key-undo {
		grid-column: span 2;
		flex-direction: row;
		gap: 0.4rem;
		font-size: 0.9rem;
		background: linear-gradient(180deg, rgba(51, 65, 85, 0.6) 0%, rgba(30, 41, 59, 0.8) 100%);
		border-color: rgba(255, 255, 255, 0.2);
	}

	.key-undo:hover:not(:disabled) {
		background: rgba(239, 68, 68, 0.25);
		border-color: rgba(239, 68, 68, 0.6);
		color: #fca5a5;
	}

	.key-undo:disabled {
		opacity: 0.35;
		cursor: not-allowed;
	}

	.key-kill {
		border-color: rgba(6, 182, 212, 0.7);
		color: #38bdf8;
		background: linear-gradient(180deg, rgba(6, 182, 212, 0.25) 0%, rgba(2, 132, 199, 0.35) 100%);
	}

	.key-bull {
		border-color: rgba(245, 158, 11, 0.65);
		color: #fbbf24;
		background: linear-gradient(180deg, rgba(245, 158, 11, 0.2) 0%, rgba(180, 83, 9, 0.3) 100%);
	}

	.key-bull:disabled,
	.key-bull.btn-disabled {
		opacity: 0.28;
		cursor: not-allowed;
		pointer-events: none;
		border-color: rgba(239, 68, 68, 0.3) !important;
		background: rgba(15, 23, 42, 0.6) !important;
		color: #64748b !important;
		filter: grayscale(1);
	}

	.key-miss,
	.key-drop,
	.key-fault {
		border-color: rgba(239, 68, 68, 0.3);
		color: #f87171;
		background: linear-gradient(180deg, rgba(239, 68, 68, 0.1) 0%, rgba(153, 27, 27, 0.2) 100%);
	}

	.btn-rename-player {
		background: rgba(255, 255, 255, 0.08);
		border: 1px solid rgba(255, 255, 255, 0.18);
		color: #cbd5e1;
		width: 24px;
		height: 24px;
		border-radius: 4px;
		font-size: 0.75rem;
		cursor: pointer;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		transition: all 0.15s ease;
		line-height: 1;
		padding: 0;
	}

	.btn-rename-player:hover {
		background: rgba(245, 158, 11, 0.25);
		border-color: var(--accent-amber);
		color: #ffffff;
	}

	.mini-roster {
		margin-top: 0.15rem;
	}

	.roster-header-row {
		display: flex;
		justify-content: space-between;
		align-items: baseline;
		margin-bottom: 0.2rem;
	}

	.roster-title {
		font-size: 0.72rem;
		color: var(--text-secondary);
		text-transform: uppercase;
		margin: 0;
		letter-spacing: 0.05em;
	}

	.kills-quota-hint {
		font-size: 0.65rem;
		color: var(--accent-amber);
	}

	.roster-pills-list {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(130px, 1fr));
		gap: 0.35rem;
		max-height: 75px;
		overflow-y: auto;
	}

	.roster-pill {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 0.3rem 0.55rem;
		border-radius: var(--radius-sm);
		background: rgba(255, 255, 255, 0.04);
		border: 1px solid rgba(255, 255, 255, 0.08);
		font-size: 0.82rem;
	}

	.roster-pill.active-row {
		background: rgba(245, 158, 11, 0.18);
		border-color: var(--accent-amber);
	}

	.roster-p-info {
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}

	.p-name {
		font-weight: 700;
		color: #fff;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		max-width: 80px;
	}

	.p-kills-tag {
		font-size: 0.62rem;
		color: var(--accent-cyan);
	}

	.roster-p-right {
		display: flex;
		align-items: center;
		gap: 0.35rem;
	}

	.p-score {
		font-weight: 800;
		color: var(--accent-amber);
	}

	.safety-banner {
		background: rgba(239, 68, 68, 0.25);
		border: 2px solid var(--accent-crimson);
		color: #fff;
		padding: 0.6rem 1rem;
		border-radius: var(--radius-md);
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}

	.safety-icon {
		font-size: 1.5rem;
	}


	.idle-container {
		display: flex;
		flex-direction: column;
		min-height: 100vh;
		background: radial-gradient(circle at center, #1a2234 0%, #0a0e17 100%);
		padding: 1.5rem 2rem;
		box-sizing: border-box;
	}

	.idle-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		width: 100%;
	}

	.idle-pill {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.35rem 0.85rem;
		border-radius: 9999px;
		background: rgba(16, 185, 129, 0.15);
		border: 1px solid rgba(16, 185, 129, 0.35);
		color: #34d399;
		font-size: 0.8rem;
		font-weight: 700;
		letter-spacing: 0.05em;
	}

	.pulse-dot {
		width: 8px;
		height: 8px;
		border-radius: 50%;
		background: #10b981;
		box-shadow: 0 0 10px #10b981;
		animation: pulseAnimation 2s infinite ease-in-out;
	}

	@keyframes pulseAnimation {
		0%, 100% { opacity: 1; transform: scale(1); }
		50% { opacity: 0.4; transform: scale(0.85); }
	}

	.idle-main {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		text-align: center;
		padding: 2rem 1rem;
	}

	.idle-axe-icon {
		font-size: 3.5rem;
		margin-bottom: 1rem;
		filter: drop-shadow(0 4px 15px rgba(245, 158, 11, 0.3));
		animation: idleFloat 3s ease-in-out infinite alternate;
	}

	@keyframes idleFloat {
		from { transform: translateY(0px); }
		to { transform: translateY(-8px); }
	}

	.idle-lane-number {
		font-size: 6rem;
		line-height: 1;
		font-weight: 900;
		letter-spacing: -0.02em;
		color: #ffffff;
		text-shadow: 0 4px 30px rgba(0, 0, 0, 0.8), 0 0 40px rgba(245, 158, 11, 0.25);
		margin: 0 0 1.25rem 0;
		text-transform: uppercase;
	}

	.idle-status-line {
		font-size: 1.25rem;
		font-weight: 800;
		letter-spacing: 0.15em;
		color: var(--accent-amber, #f59e0b);
		text-transform: uppercase;
		margin-bottom: 0.75rem;
	}

	.idle-status-sub {
		font-size: 0.95rem;
		color: var(--text-secondary, #94a3b8);
		max-width: 480px;
		margin: 0;
		line-height: 1.5;
	}

	.hud-timer {
		display: flex;
		align-items: center;
		gap: 0.45rem;
		padding: 0.35rem 0.85rem;
		border-radius: var(--radius-sm);
		background: rgba(245, 158, 11, 0.15);
		border: 1px solid rgba(245, 158, 11, 0.4);
		color: #fbbf24;
		font-size: 1.1rem;
		font-weight: 900;
		font-variant-numeric: tabular-nums;
	}

	.hud-timer.timer-urgent {
		background: rgba(239, 68, 68, 0.2);
		border-color: rgba(239, 68, 68, 0.5);
		color: #f87171;
	}

	.lobby-panel {
		padding: 2.25rem 2rem;
		max-width: 960px;
		margin: 1.5rem auto;
		border-radius: var(--radius-lg);
		border: 1px solid rgba(255, 255, 255, 0.12);
		background: rgba(18, 24, 38, 0.9);
		box-shadow: 0 15px 45px rgba(0, 0, 0, 0.6);
	}

	.lobby-header {
		text-align: center;
		margin-bottom: 2rem;
		display: flex;
		flex-direction: column;
		align-items: center;
	}

	.lobby-badge {
		display: inline-block;
		background: rgba(16, 185, 129, 0.2);
		color: var(--accent-emerald, #10b981);
		border: 1px solid rgba(16, 185, 129, 0.4);
		padding: 0.25rem 0.75rem;
		border-radius: 9999px;
		font-size: 0.75rem;
		font-weight: 800;
		letter-spacing: 0.08em;
		margin-bottom: 0.5rem;
	}

	.lobby-title {
		font-size: 2.4rem;
		color: #fff;
		margin: 0 0 1rem 0;
	}

	.lobby-timer-card {
		display: inline-flex;
		flex-direction: column;
		align-items: center;
		padding: 0.75rem 2rem;
		border-radius: var(--radius-md);
		background: rgba(245, 158, 11, 0.12);
		border: 1px solid rgba(245, 158, 11, 0.35);
		box-shadow: 0 0 25px rgba(245, 158, 11, 0.15);
	}

	.lobby-timer-card.timer-urgent {
		background: rgba(239, 68, 68, 0.15);
		border-color: rgba(239, 68, 68, 0.5);
		box-shadow: 0 0 25px rgba(239, 68, 68, 0.25);
	}

	.lobby-timer-label {
		font-size: 0.7rem;
		font-weight: 800;
		letter-spacing: 0.1em;
		color: var(--accent-amber);
	}

	.lobby-timer-card.timer-urgent .lobby-timer-label {
		color: #f87171;
	}

	.lobby-timer-clock {
		font-size: 2.2rem;
		font-weight: 900;
		color: #ffffff;
		letter-spacing: 0.05em;
		margin: 0.15rem 0;
		font-variant-numeric: tabular-nums;
	}

	.lobby-timer-sub {
		font-size: 0.75rem;
		color: var(--text-secondary);
	}

	.lobby-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 2rem;
		margin-bottom: 2rem;
	}

	.lobby-section-header {
		display: flex;
		justify-content: space-between;
		align-items: baseline;
		margin-bottom: 0.85rem;
	}

	.lobby-hint-text {
		font-size: 0.75rem;
		color: var(--text-secondary);
	}

	.lobby-roster-list {
		display: flex;
		flex-direction: column;
		gap: 0.65rem;
		max-height: 280px;
		overflow-y: auto;
	}

	.lobby-player-card {
		display: flex;
		align-items: center;
		gap: 0.85rem;
		padding: 0.75rem 1rem;
		border-radius: var(--radius-md);
		background: rgba(255, 255, 255, 0.04);
		border: 1px solid rgba(255, 255, 255, 0.08);
	}

	.lobby-player-avatar {
		width: 38px;
		height: 38px;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-weight: 900;
		color: #000;
		font-size: 1.05rem;
		flex-shrink: 0;
	}

	.lobby-player-info {
		flex: 1;
		display: flex;
		flex-direction: column;
	}

	.lobby-player-order {
		font-size: 0.7rem;
		font-weight: 700;
		text-transform: uppercase;
		color: var(--accent-amber);
	}

	.lobby-player-name {
		font-size: 1.05rem;
		font-weight: 700;
		color: #fff;
	}

	.lobby-mode-card {
		padding: 1.25rem;
		border-radius: var(--radius-md);
		background: rgba(255, 255, 255, 0.04);
		border: 1px solid rgba(255, 255, 255, 0.08);
		display: flex;
		gap: 1rem;
		align-items: flex-start;
	}

	.lobby-mode-icon {
		font-size: 2.2rem;
		line-height: 1;
	}

	.lobby-mode-content {
		flex: 1;
	}

	.lobby-mode-title {
		font-size: 1.2rem;
		font-weight: 800;
		color: #fff;
		margin: 0 0 0.35rem 0;
	}

	.lobby-mode-desc {
		font-size: 0.88rem;
		color: var(--text-secondary);
		line-height: 1.45;
		margin: 0 0 0.75rem 0;
	}

	.lobby-mode-pills {
		display: flex;
		flex-wrap: wrap;
		gap: 0.4rem;
	}

	.mode-pill {
		font-size: 0.72rem;
		font-weight: 700;
		padding: 0.2rem 0.55rem;
		border-radius: 9999px;
		background: rgba(255, 255, 255, 0.08);
		color: #e2e8f0;
	}

	.section-heading {
		font-size: 0.95rem;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		color: var(--accent-amber);
		margin-bottom: 0;
	}

	.lobby-footer {
		display: flex;
		justify-content: center;
	}

	.btn-launch-match {
		padding: 1rem 3rem;
		font-size: 1.15rem;
		font-weight: 800;
		box-shadow: 0 4px 20px rgba(245, 158, 11, 0.35);
	}

	.modal-backdrop {
		position: fixed;
		inset: 0;
		background: rgba(10, 15, 29, 0.88);
		backdrop-filter: blur(8px);
		-webkit-backdrop-filter: blur(8px);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 9999;
		padding: 1.5rem;
		box-sizing: border-box;
		animation: modalFadeIn 0.15s ease-out;
	}

	.sub-modal-content {
		background: #0f172a;
		border: 1px solid rgba(255, 255, 255, 0.15);
		border-radius: var(--radius-lg, 16px);
		padding: 1.75rem;
		max-width: 520px;
		width: 100%;
		box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.85);
		position: relative;
		max-height: 90vh;
		overflow-y: auto;
		animation: modalScaleIn 0.2s ease-out;
	}

	.switch-modal-content {
		max-width: 680px;
	}

	.sub-modal-title {
		font-size: 1.4rem;
		font-weight: 800;
		color: #ffffff;
		margin: 0;
	}

	.sub-modal-subtitle {
		font-size: 0.9rem;
		color: var(--text-secondary);
		margin-top: 0.4rem;
		margin-bottom: 1.25rem;
		line-height: 1.45;
	}

	.modal-actions {
		display: flex;
		justify-content: flex-end;
		gap: 0.75rem;
		margin-top: 1.5rem;
	}

	@keyframes modalFadeIn {
		from { opacity: 0; }
		to { opacity: 1; }
	}

	@keyframes modalScaleIn {
		from { opacity: 0; transform: scale(0.95); }
		to { opacity: 1; transform: scale(1); }
	}

	.modal-header-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 0.5rem;
	}

	.btn-close {
		background: transparent;
		border: none;
		color: var(--text-secondary);
		font-size: 1.25rem;
		cursor: pointer;
		padding: 0.25rem 0.5rem;
	}

	.btn-close:hover {
		color: #fff;
	}

	.games-selector-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 0.75rem;
		margin-top: 1.25rem;
	}

	.game-card-btn {
		position: relative;
		display: flex;
		flex-direction: column;
		text-align: left;
		padding: 1rem;
		border-radius: var(--radius-md);
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		cursor: pointer;
		color: #fff;
		transition: all 0.15s ease;
	}

	.game-card-btn:hover {
		border-color: var(--border-highlight);
		background: var(--bg-surface-elevated);
	}

	.game-card-btn.current-game {
		border-color: var(--accent-amber);
		background: rgba(245, 158, 11, 0.15);
	}

	.game-card-name {
		font-weight: 700;
		font-size: 1rem;
	}

	.game-card-desc {
		font-size: 0.8rem;
		color: var(--text-secondary);
		margin-top: 0.35rem;
	}

	.badge-active-game {
		position: absolute;
		top: 0.5rem;
		right: 0.5rem;
		background: var(--accent-amber);
		color: #000;
		font-size: 0.65rem;
		font-weight: 800;
		padding: 0.15rem 0.45rem;
		border-radius: 9999px;
	}

	.pin-error-text {
		color: var(--accent-crimson, #ef4444);
		font-size: 0.85rem;
		margin-top: 0.4rem;
	}

	.quick-undo-btn {
		background: rgba(255, 255, 255, 0.08);
		border: 1px solid rgba(255, 255, 255, 0.2);
		color: #fff;
		padding: 0.45rem 0.85rem;
		border-radius: var(--radius-sm);
		font-size: 0.88rem;
		font-weight: 700;
		cursor: pointer;
		display: inline-flex;
		align-items: center;
		gap: 0.35rem;
		transition: all 0.15s ease;
		height: fit-content;
		align-self: center;
	}

	.quick-undo-btn:hover:not(:disabled) {
		background: rgba(239, 68, 68, 0.25);
		border-color: rgba(239, 68, 68, 0.7);
		color: #fca5a5;
		transform: translateY(-1px);
	}

	.quick-undo-btn:disabled {
		opacity: 0.35;
		cursor: not-allowed;
	}

	.hit-toast-badge {
		position: absolute;
		top: 50%;
		left: 50%;
		transform: translate(-50%, -50%);
		z-index: 50;
		pointer-events: none;
		font-size: 1.8rem;
		font-weight: 900;
		padding: 0.75rem 1.8rem;
		border-radius: 9999px;
		color: #ffffff;
		text-shadow: 0 2px 5px rgba(0, 0, 0, 0.7);
		animation: popAndFade 0.9s cubic-bezier(0.175, 0.885, 0.32, 1.275) forwards;
		white-space: nowrap;
	}

	@keyframes popAndFade {
		0% {
			opacity: 0;
			transform: translate(-50%, -50%) scale(0.6);
		}
		25% {
			opacity: 1;
			transform: translate(-50%, -50%) scale(1.1);
		}
		70% {
			opacity: 1;
			transform: translate(-50%, -50%) scale(1);
		}
		100% {
			opacity: 0;
			transform: translate(-50%, -65%) scale(0.95);
		}
	}

	.hit-toast-badge.hit-bullseye {
		background: linear-gradient(135deg, #f59e0b, #d97706);
		box-shadow: 0 0 35px rgba(245, 158, 11, 0.8);
	}

	.hit-toast-badge.hit-killshot {
		background: linear-gradient(135deg, #06b6d4, #0284c7);
		box-shadow: 0 0 35px rgba(6, 182, 212, 0.85);
	}

	.hit-toast-badge.hit-points {
		background: linear-gradient(135deg, #10b981, #059669);
		box-shadow: 0 0 30px rgba(16, 185, 129, 0.7);
	}

	.hit-toast-badge.hit-miss,
	.hit-toast-badge.hit-drop,
	.hit-toast-badge.hit-fault {
		background: linear-gradient(135deg, #ef4444, #b91c1c);
		box-shadow: 0 0 30px rgba(239, 68, 68, 0.7);
	}

	@media (max-height: 720px) {
		.tablet-viewport {
			padding: 0.25rem 0.5rem 0.5rem;
		}
		.console-layout {
			gap: 0.3rem;
		}
		.hud-bar {
			padding: 0.25rem 0.6rem;
		}
		.player-banner {
			padding: 0.35rem 0.75rem;
		}
		.player-avatar {
			width: 32px;
			height: 32px;
			font-size: 1rem;
		}
		.player-name {
			font-size: 1.1rem;
		}
		.stat-val {
			font-size: 1.25rem;
		}
		.target-card {
			padding: 0.4rem;
			--target-max-size: min(320px, 46vh);
			--target-max-height: min(320px, 46vh);
		}
		.controls-card {
			padding: 0.5rem 0.75rem;
			gap: 0.35rem;
		}
		.btn-clutch {
			padding: 0.35rem 0.65rem;
			font-size: 0.85rem;
		}
		.touch-keypad {
			gap: 0.3rem;
		}
		.key-btn {
			padding: 0.35rem 0;
			min-height: 40px;
			font-size: 1.15rem;
		}
		.key-btn small {
			font-size: 0.55rem;
		}
		.key-undo {
			font-size: 0.85rem;
		}
		.roster-pills-list {
			max-height: 60px;
		}
		.roster-pill {
			padding: 0.2rem 0.45rem;
			font-size: 0.78rem;
		}
	}

	.low-time-warning-banner {
		display: flex;
		align-items: center;
		gap: 0.85rem;
		background: linear-gradient(90deg, rgba(245, 158, 11, 0.22), rgba(220, 38, 38, 0.22));
		border: 1.5px solid rgba(245, 158, 11, 0.6);
		border-radius: var(--radius-md, 8px);
		padding: 0.65rem 1.25rem;
		margin: 0.5rem 1rem 0;
		color: #fef08a;
		animation: pulse-border 2s infinite ease-in-out;
	}

	.low-time-warning-banner .warning-icon {
		font-size: 1.5rem;
	}

	.low-time-warning-banner .warning-text {
		display: flex;
		flex-direction: column;
		gap: 0.15rem;
	}

	.low-time-warning-banner .warning-text strong {
		font-size: 0.95rem;
		letter-spacing: 0.05em;
		color: #fde047;
	}

	.low-time-warning-banner .warning-text span {
		font-size: 0.8rem;
		color: #cbd5e1;
	}

	@keyframes pulse-border {
		0%, 100% { border-color: rgba(245, 158, 11, 0.4); box-shadow: 0 0 8px rgba(245, 158, 11, 0.2); }
		50% { border-color: rgba(239, 68, 68, 0.8); box-shadow: 0 0 16px rgba(239, 68, 68, 0.4); }
	}

	@media (max-width: 900px) and (min-height: 700px) {
		.arena-grid {
			grid-template-columns: 1fr;
		}
		.lobby-grid {
			grid-template-columns: 1fr;
		}
		.games-selector-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
