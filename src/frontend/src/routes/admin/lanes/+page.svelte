<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { page } from '$app/state';
	import { venueState } from '$lib/stores/venueState.svelte';
	import {
		getApiAdminLanesVenueByVenueId,
		postApiAdminLanesVenueByVenueId,
		putApiAdminLanesById,
		deleteApiAdminLanesById,
		postApiAdminLanesByIdRegeneratePairing,
		postApiAdminLanesBySourceLaneIdTransferToByTargetLaneId,
		putApiAdminLanesByIdToggleActive,
		getApiAdminLanesByIdUpcomingReservations,
		postApiLanesOperationsByLaneIdStartSession,
		postApiLanesOperationsByLaneIdEndSession,
		postApiLanesOperationsByLaneIdExtend
	} from '$lib/api/client';
	import type { LaneDto, BookingDto, NextBookingSummaryDto } from '$lib/api/generated/types.gen';
	import { createAdminHubConnection } from '$lib/services/signalr';
	import { formatTimeInTz } from '$lib/utils/dateTime';

	let lanes = $state<LaneDto[]>([]);
	let isLoading = $state(true);

	// Session launcher modal
	let selectedLaneForSession = $state<LaneDto | null>(null);
	let activeBookingPromptLane = $state<LaneDto | null>(null);
	let sessionBookingId = $state<string | null>(null);
	let sessionTitle = $state('Walk-in Match');
	let playerNames = $state('Thrower 1, Thrower 2');
	let sessionDurationMinutes = $state(60);
	let gameType = $state('watl-standard');
	let isStartingSession = $state(false);

	function handleOpenStartSession(lane: LaneDto) {
		if (lane.currentBooking) {
			activeBookingPromptLane = lane;
		} else {
			startWalkInSession(lane);
		}
	}

	function startSessionWithBooking(lane: LaneDto, booking: NextBookingSummaryDto) {
		activeBookingPromptLane = null;
		selectedLaneForSession = lane;
		sessionBookingId = booking.bookingId;
		sessionTitle = `${booking.guestName} (${booking.bookingReference})`;

		const partySize = Math.max(1, Number(booking.partySize) || 2);
		playerNames = Array.from({ length: partySize }, (_, i) =>
			i === 0 ? booking.guestName : `Thrower ${i + 1}`
		).join(', ');

		try {
			const startMs = new Date(booking.startTime).getTime();
			const endMs = new Date(booking.endTime).getTime();
			const diffMins = Math.round((endMs - startMs) / 60000);
			sessionDurationMinutes = diffMins > 0 ? diffMins : 60;
		} catch {
			sessionDurationMinutes = 60;
		}
	}

	function startWalkInSession(lane: LaneDto) {
		activeBookingPromptLane = null;
		selectedLaneForSession = lane;
		sessionBookingId = null;
		sessionTitle = `Walk-in (${lane.name})`;
		playerNames = 'Thrower 1, Thrower 2';
		sessionDurationMinutes = 60;
	}

	// Lane transfer modal
	let showTransferModal = $state(false);
	let transferSourceLane = $state<LaneDto | null>(null);
	let transferTargetLaneId = $state<string>('');
	let isTransferring = $state(false);
	let transferError = $state<string | null>(null);

	// Add/Edit lane modals
	let showCreateLaneModal = $state(false);
	let newLaneNumber = $state(1);
	let newLaneName = $state('');
	let newLaneMaxThrowers = $state(6);
	let isCreatingLane = $state(false);

	let editingLane = $state<LaneDto | null>(null);
	let editLaneNumber = $state(1);
	let editLaneName = $state('');
	let editLaneMaxThrowers = $state(6);
	let isUpdatingLane = $state(false);

	let deletingLane = $state<LaneDto | null>(null);

	// Deactivation warning dialog
	let deactivatingLane = $state<LaneDto | null>(null);
	let upcomingBookingsForDeactivation = $state<BookingDto[]>([]);
	let isCheckingUpcoming = $state(false);

	function formatBookingTime(isoString: string): string {
		if (!isoString) return '';
		return formatTimeInTz(isoString, venueState.selectedVenue?.timezone);
	}

	async function handleCollectLaneBookingBalance(bookingId: string, amountCents: number, guestName: string) {
		if (!confirm(`Collect remaining balance of $${(amountCents / 100).toFixed(2)} for ${guestName}?`)) return;
		try {
			const res = await fetch(`/api/admin/bookings/${bookingId}/payment`, {
				method: 'PUT',
				headers: { 'Content-Type': 'application/json' },
				credentials: 'include',
				body: JSON.stringify({ amountCents, paymentMethod: 'Card' })
			});
			if (res.ok) {
				await loadLanes();
			} else {
				alert('Failed to collect balance payment.');
			}
		} catch (e) {
			console.error('Failed to collect balance:', e);
		}
	}

	let hubConnection: any = null;

	async function loadLanes() {
		if (!venueState.selectedVenue) {
			if (!venueState.isLoading) {
				isLoading = false;
			}
			return;
		}
		isLoading = true;
		try {
			const res = await getApiAdminLanesVenueByVenueId({
				path: { venueId: venueState.selectedVenue.id }
			});
			lanes = res.data || [];
			const startLaneParam = page.url.searchParams.get('startLane');
			if (startLaneParam && !selectedLaneForSession) {
				const laneNum = Number(startLaneParam);
				const target = lanes.find((l) => l.laneNumber === laneNum);
				if (target && target.isActive) {
					selectedLaneForSession = target;
					sessionTitle = `Walk-in (${target.name})`;
				}
			}
		} catch (err) {
			console.error('Failed to load lanes', err);
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		if (venueState.selectedVenue) {
			loadLanes();
		} else if (!venueState.isLoading) {
			isLoading = false;
		}
	});

	onMount(async () => {
		await loadLanes();

		try {
			hubConnection = createAdminHubConnection();
			await hubConnection.start();
			await hubConnection.invoke('JoinAdminDashboard');

			hubConnection.on('OnLaneStateChanged', () => {
				loadLanes();
			});
		} catch (e) {
			console.warn('SignalR admin connection warning:', e);
		}
	});

	onDestroy(() => {
		if (hubConnection) {
			hubConnection.stop();
		}
	});

	// --- START SESSION ---
	async function handleStartSession() {
		if (!selectedLaneForSession) return;
		isStartingSession = true;
		try {
			const names = playerNames
				.split(',')
				.map((n) => n.trim())
				.filter(Boolean);

			const res = await postApiLanesOperationsByLaneIdStartSession({
				path: { laneId: selectedLaneForSession.id },
				body: {
					sessionTitle: sessionTitle.trim(),
					durationMinutes: Number(sessionDurationMinutes),
					bookingId: sessionBookingId,
					gameTypeId: gameType,
					initialRoster: names.map((name) => ({ name }))
				}
			});

			if (res.data) {
				selectedLaneForSession = null;
				sessionBookingId = null;
				await loadLanes();
			}
		} catch (e) {
			console.error('Error starting session', e);
		} finally {
			isStartingSession = false;
		}
	}

	// --- STOP SESSION ---
	async function handleStopSession(lane: LaneDto) {
		if (!confirm(`Are you sure you want to stop the active session on ${lane.name}? This will complete the match and free up the lane.`)) {
			return;
		}

		try {
			await postApiLanesOperationsByLaneIdEndSession({
				path: { laneId: lane.id }
			});
			await loadLanes();
		} catch (e) {
			console.error('Failed to stop session', e);
			alert('Failed to stop session.');
		}
	}

	// --- EXTEND SESSION ---
	async function handleExtendSession(laneId: string, minutes: number) {
		try {
			await postApiLanesOperationsByLaneIdExtend({
				path: { laneId },
				body: { extraMinutes: minutes }
			});
			await loadLanes();
		} catch (e) {
			console.error('Failed to extend session', e);
		}
	}

	// --- ACTIVE / DEACTIVATE TOGGLE ---
	async function handleToggleLaneActive(lane: LaneDto) {
		if (lane.activeSession) {
			return; // In-use lanes cannot be deactivated until session is stopped
		}

		if (lane.isActive) {
			// Want to deactivate: check if there are upcoming bookings
			isCheckingUpcoming = true;
			try {
				const res = await getApiAdminLanesByIdUpcomingReservations({
					path: { id: lane.id }
				});
				const upcoming = res.data || [];
				if (upcoming.length > 0) {
					deactivatingLane = lane;
					upcomingBookingsForDeactivation = upcoming;
					return;
				}
			} catch (e) {
				console.error('Error checking upcoming reservations', e);
			} finally {
				isCheckingUpcoming = false;
			}

			// No upcoming reservations: direct deactivation
			await executeToggleActive(lane.id, false);
		} else {
			// Reactivate directly
			await executeToggleActive(lane.id, true);
		}
	}

	async function executeToggleActive(laneId: string, isActive: boolean) {
		try {
			const res = await putApiAdminLanesByIdToggleActive({
				path: { id: laneId },
				body: { isActive }
			});
			if (res.data) {
				lanes = lanes.map((l) => (l.id === laneId ? { ...l, isActive: res.data!.isActive } : l));
			}
			deactivatingLane = null;
			upcomingBookingsForDeactivation = [];
		} catch (e) {
			console.error('Failed to toggle lane active status', e);
			alert('Failed to update lane activation state.');
		}
	}

	// --- REGENERATE PAIRING CODES ---
	async function handleRegeneratePairing(laneId: string) {
		try {
			const res = await postApiAdminLanesByIdRegeneratePairing({
				path: { id: laneId }
			});
			if (res.data) {
				lanes = lanes.map((l) =>
					l.id === laneId
						? {
								...l,
								tabletPairingCode: res.data!.tabletPairingCode,
								screenPairingCode: res.data!.screenPairingCode
							}
						: l
				);
			}
		} catch (e) {
			console.error('Failed to regenerate pairing codes', e);
		}
	}

	// --- TRANSFER MODAL ---
	function openTransferModal(lane: LaneDto) {
		transferSourceLane = lane;
		transferError = null;
		const availableTarget = lanes.find((l) => l.id !== lane.id && !l.activeSession && l.isActive);
		transferTargetLaneId = availableTarget ? availableTarget.id : '';
		showTransferModal = true;
	}

	async function handleExecuteTransfer() {
		if (!transferSourceLane || !transferTargetLaneId) return;
		isTransferring = true;
		transferError = null;
		try {
			await postApiAdminLanesBySourceLaneIdTransferToByTargetLaneId({
				path: {
					sourceLaneId: transferSourceLane.id,
					targetLaneId: transferTargetLaneId
				}
			});
			showTransferModal = false;
			await loadLanes();
		} catch (err: any) {
			transferError = err?.message || 'Transfer failed. Target lane may no longer be available.';
		} finally {
			isTransferring = false;
		}
	}

	// --- CREATE LANE ---
	function openCreateLaneModal() {
		newLaneNumber = lanes.length > 0 ? Math.max(...lanes.map((l) => Number(l.laneNumber))) + 1 : 1;
		newLaneName = `Lane ${newLaneNumber < 10 ? '0' : ''}${newLaneNumber}`;
		newLaneMaxThrowers = 6;
		showCreateLaneModal = true;
	}

	async function handleCreateLane(e: SubmitEvent) {
		e.preventDefault();
		if (!venueState.selectedVenue) return;
		isCreatingLane = true;
		try {
			const res = await postApiAdminLanesVenueByVenueId({
				path: { venueId: venueState.selectedVenue.id },
				body: {
					laneNumber: Number(newLaneNumber),
					name: newLaneName.trim(),
					maxThrowers: Number(newLaneMaxThrowers)
				}
			});
			if (res.data) {
				showCreateLaneModal = false;
				await loadLanes();
			}
		} catch (e) {
			console.error(e);
		} finally {
			isCreatingLane = false;
		}
	}

	// --- EDIT LANE ---
	function openEditLaneModal(lane: LaneDto) {
		editingLane = lane;
		editLaneNumber = Number(lane.laneNumber);
		editLaneName = lane.name;
		editLaneMaxThrowers = Number(lane.maxThrowers);
	}

	async function handleUpdateLane(e: SubmitEvent) {
		e.preventDefault();
		if (!editingLane) return;
		isUpdatingLane = true;
		try {
			const res = await putApiAdminLanesById({
				path: { id: editingLane.id },
				body: {
					laneNumber: Number(editLaneNumber),
					name: editLaneName.trim(),
					maxThrowers: Number(editLaneMaxThrowers),
					status: editingLane.currentStatus
				}
			});
			if (res.data) {
				editingLane = null;
				await loadLanes();
			}
		} catch (e) {
			console.error(e);
		} finally {
			isUpdatingLane = false;
		}
	}

	// --- DELETE LANE ---
	async function handleDeleteLane() {
		if (!deletingLane) return;
		try {
			await deleteApiAdminLanesById({
				path: { id: deletingLane.id }
			});
			deletingLane = null;
			await loadLanes();
		} catch (e) {
			console.error(e);
		}
	}
</script>

<svelte:head>
	<title>Lanes Overview | VenueAxe Admin</title>
</svelte:head>

<div class="tab-header">
	<div>
		<h2 class="font-display">Lanes Overview & Live Telemetry</h2>
		<p class="tab-subtitle">Manage physical throwing lanes, hardware pairing PINs, and session states</p>
	</div>
	<div style="display: flex; gap: 0.75rem; align-items: center;">
		<button class="btn btn-secondary font-display" onclick={loadLanes}>
			🔄 Refresh
		</button>
		<button class="btn btn-primary font-display" onclick={openCreateLaneModal}>
			+ Add Lane
		</button>
	</div>
</div>

{#if isLoading}
	<div style="padding: 3rem; text-align: center; color: var(--text-secondary);">
		<p class="font-display">Loading throwing lanes...</p>
	</div>
{:else if lanes.length === 0}
	<div class="empty-lanes-state glass-panel">
		<span class="empty-icon">🏟️</span>
		<h3 class="font-display empty-title">No Target Lanes Created Yet</h3>
		<p class="empty-desc">
			This venue currently has no lanes configured. Add your first target throwing lane to generate hardware pairing PINs and launch matches.
		</p>
		<button class="btn btn-primary font-display" onclick={openCreateLaneModal}>
			+ Add Your First Lane
		</button>
	</div>
{:else}
	<div class="lanes-grid">
		{#each lanes as lane (lane.id)}
			<div
				class="lane-card glass-panel"
				class:card-active={lane.activeSession}
				style={!lane.isActive ? 'opacity: 0.75; border-color: rgba(100, 116, 139, 0.4);' : ''}
			>
				<div class="lane-header" style="display: flex; justify-content: space-between; align-items: center;">
					<span class="lane-name font-display">{lane.name}</span>

					<!-- Status & Activation Toggle Pill -->
					{#if lane.activeSession}
						<span class="badge badge-active font-display">
							🔥 Active Match
						</span>
					{:else if lane.isActive}
						<button
							type="button"
							class="badge badge-available badge-toggle-btn font-display"
							title="Lane is ACTIVE for bookings. Click to deactivate."
							onclick={() => handleToggleLaneActive(lane)}
						>
							● Available (Active)
						</button>
					{:else}
						<button
							type="button"
							class="badge badge-deactivated badge-toggle-btn font-display"
							title="Lane is DEACTIVATED (no bookings allowed). Click to activate."
							onclick={() => handleToggleLaneActive(lane)}
						>
							⊘ Deactivated (Click to Activate)
						</button>
					{/if}
				</div>

				<!-- Active Session Timer & Controls -->
				{#if lane.activeSession}
					<div class="session-box">
						<div class="session-title font-display">{lane.activeSession.sessionTitle}</div>
						<div class="timer-display font-display" style="color: var(--accent-amber); font-weight: 800;">
							⏱️ {lane.activeSession.minutesRemaining} MIN REMAINING
						</div>
						<div class="extend-actions-row" style="display: flex; gap: 0.5rem; margin-top: 0.6rem;">
							<button
								type="button"
								class="btn btn-secondary btn-xs font-display"
								title="Extend active match by +15 minutes"
								onclick={() => handleExtendSession(lane.id, 15)}
							>
								+15m
							</button>
							<button
								type="button"
								class="btn btn-secondary btn-xs font-display"
								title="Extend active match by +30 minutes"
								onclick={() => handleExtendSession(lane.id, 30)}
							>
								+30m
							</button>
							<button
								type="button"
								class="btn btn-secondary btn-xs font-display"
								title="Transfer active match to another lane"
								onclick={() => openTransferModal(lane)}
								style="color: var(--accent-cyan); border-color: var(--accent-cyan);"
							>
								↔️ Move
							</button>
						</div>
						{#if lane.activeSession.currentGame}
							<div class="game-info" style="margin-top: 0.5rem; font-size: 0.8rem; color: var(--text-secondary); display: flex; justify-content: space-between;">
								<span>Mode: {lane.activeSession.currentGame.gameName}</span>
								<span>Round: {lane.activeSession.currentGame.currentRound}/{lane.activeSession.currentGame.totalRounds}</span>
							</div>
						{/if}
					</div>
				{:else}
					<div class="empty-lane-box" style="padding: 1.25rem; text-align: center; background: rgba(15, 23, 42, 0.4); border-radius: var(--radius-md); border: 1px dashed var(--border-color);">
						<span class="text-secondary" style="display: block; font-size: 0.9rem;">
							{#if lane.isActive}
								Ready for Throwers
							{:else}
								<span style="color: #94a3b8; font-weight: 600;">⚠️ Lane Deactivated from Bookings</span>
							{/if}
						</span>
						<span class="capacity-tag font-display" style="color: var(--accent-amber); font-size: 0.8rem; font-weight: 700;">
							Max {lane.maxThrowers} Throwers
						</span>
					</div>
				{/if}

				<!-- Next Upcoming Booking Today (if any) -->
				{#if lane.nextBookingToday}
					{@const balDue = Math.max(0, ((lane.nextBookingToday as any).totalAmountCents ?? 0) - ((lane.nextBookingToday as any).paidAmountCents ?? 0))}
					<div
						class="next-booking-card"
						style="background: rgba(30, 41, 59, 0.7); border: 1px solid rgba(59, 130, 246, 0.4); border-left: 3px solid #3b82f6; border-radius: var(--radius-md); padding: 0.6rem 0.75rem;"
					>
						<div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.25rem;">
							<span style="font-size: 0.7rem; font-weight: 800; color: #60a5fa; text-transform: uppercase; letter-spacing: 0.05em; display: flex; align-items: center; gap: 0.35rem;">
								<span>📅</span> NEXT BOOKING TODAY
							</span>
							<span class="font-mono font-display" style="font-size: 0.75rem; font-weight: 700; color: #93c5fd; background: rgba(59, 130, 246, 0.2); padding: 0.15rem 0.45rem; border-radius: 4px;">
								{formatBookingTime(lane.nextBookingToday.startTime)}
							</span>
						</div>
						<div style="display: flex; justify-content: space-between; align-items: baseline; gap: 0.5rem;">
							<span style="font-size: 0.85rem; font-weight: 600; color: #f8fafc; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
								{lane.nextBookingToday.guestName}
							</span>
							<span style="font-size: 0.75rem; color: #94a3b8; white-space: nowrap;">
								{lane.nextBookingToday.partySize} {lane.nextBookingToday.partySize === 1 ? 'thrower' : 'throwers'} • #{lane.nextBookingToday.bookingReference}
							</span>
						</div>

						<!-- High-contrast Deposit Balance Alert (Pit 1) -->
						{#if balDue > 0}
							<div style="display: flex; justify-content: space-between; align-items: center; margin-top: 0.45rem; padding-top: 0.4rem; border-top: 1px dashed rgba(245, 158, 11, 0.4);">
								<span style="font-size: 0.75rem; font-weight: 800; color: var(--accent-amber); display: flex; align-items: center; gap: 0.25rem;">
									<span>🟡</span> DEPOSIT PAID — ${(balDue / 100).toFixed(2)} DUE
								</span>
								<button
									type="button"
									class="btn btn-xs font-display"
									style="background: var(--accent-amber); color: #000; font-weight: 800; padding: 0.15rem 0.5rem; border-radius: 4px;"
									onclick={() => handleCollectLaneBookingBalance(lane.nextBookingToday!.bookingId, balDue, lane.nextBookingToday!.guestName)}
								>
									Collect ${(balDue / 100).toFixed(2)}
								</button>
							</div>
						{/if}
					</div>
				{/if}

				<!-- Pairing Codes Section -->
				<div class="pair-row" style="display: flex; gap: 0.75rem; align-items: center; background: rgba(10, 15, 25, 0.6); padding: 0.5rem 0.75rem; border-radius: var(--radius-md);">
					<div style="flex: 1;">
						<small style="font-size: 0.7rem; color: var(--text-muted); display: block;">Tablet PIN</small>
						<strong class="font-mono font-display" style="color: var(--text-primary);">{lane.tabletPairingCode || '---'}</strong>
					</div>
					<div style="flex: 1;">
						<small style="font-size: 0.7rem; color: var(--text-muted); display: block;">TV PIN</small>
						<strong class="font-mono font-display" style="color: var(--text-primary);">{lane.screenPairingCode || '---'}</strong>
					</div>
					<button
						class="btn-clear"
						title="Regenerate Pairing PINs"
						onclick={() => handleRegeneratePairing(lane.id)}
					>
						🔄
					</button>
				</div>

				<!-- Action Buttons: Clean Start / Stop Session & Config -->
				<div class="lane-actions" style="display: flex; gap: 0.5rem; margin-top: auto;">
					{#if lane.activeSession}
						<button
							class="btn btn-stop-session btn-sm font-display"
							style="flex: 1;"
							onclick={() => handleStopSession(lane)}
						>
							⏹️ Stop Session
						</button>
					{:else}
						<button
							class="btn btn-primary btn-sm font-display"
							style="flex: 1;"
							disabled={!lane.isActive}
							title={!lane.isActive ? 'Activate lane before launching matches' : ''}
							onclick={() => handleOpenStartSession(lane)}
						>
							+ Start Session
						</button>
					{/if}

					<button class="btn btn-secondary btn-sm" onclick={() => openEditLaneModal(lane)} title="Edit Lane Settings">
						✏️
					</button>
					<button class="btn btn-secondary btn-sm btn-delete" onclick={() => (deletingLane = lane)} title="Delete Lane">
						🗑️
					</button>
				</div>
			</div>
		{/each}
	</div>
{/if}

<!-- ACTIVE BOOKING DETECTED PROMPT MODAL -->
{#if activeBookingPromptLane && activeBookingPromptLane.currentBooking}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (activeBookingPromptLane = null)}
		onkeydown={(e) => {
			if (e.key === 'Escape') activeBookingPromptLane = null;
		}}
	>
		<div
			class="modal-card glass-panel"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
			style="max-width: 520px;"
		>
			<div class="modal-header-row">
				<div>
					<div style="display: inline-flex; align-items: center; gap: 0.4rem; padding: 0.2rem 0.6rem; border-radius: 9999px; background: rgba(245, 158, 11, 0.2); border: 1px solid rgba(245, 158, 11, 0.4); color: #fbbf24; font-size: 0.75rem; font-weight: 700; margin-bottom: 0.4rem;">
						<span>🎯 ACTIVE RESERVATION DETECTED</span>
					</div>
					<h3 class="modal-title font-display">Start Session on {activeBookingPromptLane.name}</h3>
					<p class="editor-hint" style="margin-bottom: 0;">
						Current time falls within a scheduled reservation for this lane.
					</p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (activeBookingPromptLane = null)}>✕</button>
			</div>

			<!-- Active Booking Details Card -->
			<div style="margin-top: 1.25rem; padding: 1rem; border-radius: var(--radius-md); background: rgba(255, 255, 255, 0.04); border: 1px solid rgba(255, 255, 255, 0.12);">
				<div style="display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 0.5rem;">
					<h4 style="font-size: 1.1rem; font-weight: 700; color: #fff; margin: 0;">
						{activeBookingPromptLane.currentBooking.guestName}
					</h4>
					<span style="font-size: 0.8rem; font-family: monospace; color: var(--accent-amber);">
						#{activeBookingPromptLane.currentBooking.bookingReference}
					</span>
				</div>
				<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; font-size: 0.88rem; color: var(--text-secondary);">
					<div>
						<span style="color: var(--text-muted);">Scheduled Window:</span><br />
						<strong style="color: #fff;">{formatBookingTime(activeBookingPromptLane.currentBooking.startTime)} – {formatBookingTime(activeBookingPromptLane.currentBooking.endTime)}</strong>
					</div>
					<div>
						<span style="color: var(--text-muted);">Party Size:</span><br />
						<strong style="color: #fff;">{activeBookingPromptLane.currentBooking.partySize} Throwers</strong>
					</div>
				</div>
				{#if activeBookingPromptLane.currentBooking.notes}
					<div style="margin-top: 0.6rem; font-size: 0.8rem; color: var(--text-muted); font-style: italic; border-top: 1px solid rgba(255, 255, 255, 0.08); padding-top: 0.5rem;">
						Note: "{activeBookingPromptLane.currentBooking.notes}"
					</div>
				{/if}
			</div>

			<!-- Upcoming Booking Later Today Notice -->
			{#if activeBookingPromptLane.nextBookingToday}
				<div style="margin-top: 1rem; padding: 0.75rem 1rem; border-radius: var(--radius-sm); background: rgba(59, 130, 246, 0.12); border: 1px solid rgba(59, 130, 246, 0.35); color: #bfdbfe; font-size: 0.85rem;">
					<strong style="color: #93c5fd;">📅 Another Reservation Later Today:</strong><br />
					{activeBookingPromptLane.nextBookingToday.guestName} ({activeBookingPromptLane.nextBookingToday.partySize} guests) at {formatBookingTime(activeBookingPromptLane.nextBookingToday.startTime)} – {formatBookingTime(activeBookingPromptLane.nextBookingToday.endTime)}
				</div>
			{/if}

			<div style="display: flex; flex-direction: column; gap: 0.75rem; margin-top: 1.5rem;">
				<button
					type="button"
					class="btn btn-primary font-display"
					style="width: 100%; padding: 0.85rem 1rem; font-size: 1rem;"
					onclick={() => startSessionWithBooking(activeBookingPromptLane!, activeBookingPromptLane!.currentBooking!)}
				>
					✅ Start with this Reservation ({activeBookingPromptLane.currentBooking.partySize} Throwers)
				</button>
				<button
					type="button"
					class="btn btn-secondary font-display"
					style="width: 100%; padding: 0.75rem 1rem;"
					onclick={() => startWalkInSession(activeBookingPromptLane!)}
				>
					➕ Start New / Walk-in Session
				</button>
				<button
					type="button"
					class="btn btn-clear"
					style="color: var(--text-secondary); align-self: center;"
					onclick={() => (activeBookingPromptLane = null)}
				>
					Cancel
				</button>
			</div>
		</div>
	</div>
{/if}

<!-- SESSION LAUNCHER MODAL -->
{#if selectedLaneForSession}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (selectedLaneForSession = null)}
		onkeydown={(e) => {
			if (e.key === 'Escape') selectedLaneForSession = null;
		}}
	>
		<div
			class="modal-card glass-panel"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<div class="modal-header-row">
				<div>
					<h3 class="modal-title font-display">Launch Match Session</h3>
					<p class="editor-hint" style="margin-bottom: 0;">Start live scoring on {selectedLaneForSession.name}</p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (selectedLaneForSession = null)}>✕</button>
			</div>

			<!-- Upcoming Booking Alert if exists -->
			{#if selectedLaneForSession.nextBookingToday}
				<div style="margin-top: 0.75rem; padding: 0.75rem 1rem; border-radius: var(--radius-sm); background: rgba(59, 130, 246, 0.12); border: 1px solid rgba(59, 130, 246, 0.35); color: #bfdbfe; font-size: 0.85rem;">
					<strong style="color: #93c5fd;">📅 Upcoming Reservation Later Today:</strong><br />
					{selectedLaneForSession.nextBookingToday.guestName} ({selectedLaneForSession.nextBookingToday.partySize} guests) at {formatBookingTime(selectedLaneForSession.nextBookingToday.startTime)} – {formatBookingTime(selectedLaneForSession.nextBookingToday.endTime)}
				</div>
			{/if}

			{#if sessionBookingId}
				<div style="margin-top: 0.5rem; display: inline-flex; align-items: center; gap: 0.35rem; padding: 0.25rem 0.65rem; border-radius: 4px; background: rgba(16, 185, 129, 0.15); border: 1px solid rgba(16, 185, 129, 0.35); color: #34d399; font-size: 0.82rem; font-weight: 600;">
					<span>🎫 Using reservation settings</span>
				</div>
			{/if}

			<form onsubmit={(e) => { e.preventDefault(); handleStartSession(); }} style="margin-top: 1rem;">
				<div class="form-group">
					<label class="form-label" for="sess-title">Session Title</label>
					<input id="sess-title" type="text" class="form-input" bind:value={sessionTitle} required />
				</div>

				<div class="form-group" style="margin-top: 0.75rem;">
					<label class="form-label" for="sess-players">Player Names (comma separated)</label>
					<input id="sess-players" type="text" class="form-input" bind:value={playerNames} placeholder="Sarah, Marcus" required />
				</div>

				<div class="form-row-2" style="margin-top: 0.75rem;">
					<div class="form-group">
						<label class="form-label" for="sess-game">Game Mode</label>
						<select id="sess-game" class="form-input" bind:value={gameType}>
							<option value="watl-standard">WATL Standard (10 Throws)</option>
							<option value="countdown">Countdown (301)</option>
						</select>
					</div>
					<div class="form-group">
						<label class="form-label" for="sess-duration">Duration (Minutes)</label>
						<input id="sess-duration" type="number" min="15" max="240" step="15" class="form-input" bind:value={sessionDurationMinutes} required />
					</div>
				</div>

				<div class="modal-actions" style="margin-top: 1.5rem;">
					<button type="button" class="btn btn-secondary" onclick={() => (selectedLaneForSession = null)}>
						Cancel
					</button>
					<button type="submit" class="btn btn-primary font-display" disabled={isStartingSession}>
						{isStartingSession ? 'Launching...' : '🚀 Launch Match'}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

<!-- DEACTIVATION WARNING MODAL -->
{#if deactivatingLane}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (deactivatingLane = null)}
		onkeydown={(e) => {
			if (e.key === 'Escape') deactivatingLane = null;
		}}
	>
		<div
			class="modal-card glass-panel modal-danger"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			style="max-width: 550px;"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<div class="modal-header-row">
				<div>
					<h3 class="modal-title font-display" style="color: var(--accent-crimson);">⚠️ Upcoming Reservations Warning</h3>
					<p class="editor-hint" style="margin-bottom: 0;">Lane {deactivatingLane.name} has scheduled bookings</p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (deactivatingLane = null)}>✕</button>
			</div>

			<div class="alert alert-warning" style="margin-top: 1rem;">
				<strong>Notice:</strong> This lane has <strong>{upcomingBookingsForDeactivation.length}</strong> upcoming reservation(s) scheduled. Deactivating this lane will prevent all new bookings from selecting it, but will NOT automatically cancel or move existing reservations.
			</div>

			<div style="margin-top: 1rem; max-height: 180px; overflow-y: auto; background: rgba(0,0,0,0.3); border-radius: var(--radius-md); padding: 0.75rem;">
				{#each upcomingBookingsForDeactivation as b}
					<div style="display: flex; justify-content: space-between; font-size: 0.85rem; padding: 0.35rem 0; border-bottom: 1px solid rgba(255,255,255,0.06);">
						<span class="font-display"><strong>{b.bookingReference}</strong> ({b.guestFirstName} {b.guestLastName})</span>
						<span style="color: var(--accent-amber);">{new Date(b.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} • {b.partySize}p</span>
					</div>
				{/each}
			</div>

			<p style="margin-top: 1rem; font-size: 0.88rem; color: var(--text-secondary);">
				Are you sure you want to deactivate <strong>{deactivatingLane.name}</strong> now?
			</p>

			<div class="modal-actions" style="margin-top: 1.5rem;">
				<button type="button" class="btn btn-secondary" onclick={() => (deactivatingLane = null)}>
					Cancel
				</button>
				<button
					type="button"
					class="btn btn-danger font-display"
					onclick={() => executeToggleActive(deactivatingLane!.id, false)}
				>
					Deactivate Lane Anyway
				</button>
			</div>
		</div>
	</div>
{/if}

<!-- LANE TRANSFER MODAL -->
{#if showTransferModal && transferSourceLane}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (showTransferModal = false)}
		onkeydown={(e) => {
			if (e.key === 'Escape') showTransferModal = false;
		}}
	>
		<div
			class="modal-card glass-panel"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			style="max-width: 500px;"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<div class="modal-header-row">
				<div>
					<h3 class="modal-title font-display">Transfer Match Session</h3>
					<p class="editor-hint" style="margin-bottom: 0;">Relocate active game to a different physical lane</p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (showTransferModal = false)}>✕</button>
			</div>

			{#if transferError}
				<div class="alert-error" style="margin-top: 1rem;">
					⚠️ {transferError}
				</div>
			{/if}

			<div style="margin-top: 1rem;">
				<p style="font-size: 0.9rem; color: var(--text-secondary);">
					Current Lane: <strong style="color: #fff;">{transferSourceLane.name}</strong>
				</p>
				<div class="form-group" style="margin-top: 1rem;">
					<label class="form-label" for="target-lane-select">Destination Lane</label>
					<select id="target-lane-select" class="form-input" bind:value={transferTargetLaneId}>
						{#each lanes.filter((l) => l.id !== transferSourceLane?.id && !l.activeSession && l.isActive) as target}
							<option value={target.id}>{target.name} (Ready, Max {target.maxThrowers})</option>
						{/each}
					</select>
				</div>
			</div>

			<div class="modal-actions" style="margin-top: 1.5rem;">
				<button type="button" class="btn btn-secondary" onclick={() => (showTransferModal = false)}>
					Cancel
				</button>
				<button
					type="button"
					class="btn btn-primary font-display"
					disabled={isTransferring || !transferTargetLaneId}
					onclick={handleExecuteTransfer}
				>
					{isTransferring ? 'Moving Match...' : '↔️ Move Match'}
				</button>
			</div>
		</div>
	</div>
{/if}

<!-- CREATE LANE MODAL -->
{#if showCreateLaneModal}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (showCreateLaneModal = false)}
		onkeydown={(e) => {
			if (e.key === 'Escape') showCreateLaneModal = false;
		}}
	>
		<div
			class="modal-card glass-panel"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<div class="modal-header-row">
				<div>
					<h3 class="modal-title font-display">Add Target Lane</h3>
					<p class="editor-hint" style="margin-bottom: 0;">Configure a new throwing lane for this facility</p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (showCreateLaneModal = false)}>✕</button>
			</div>

			<form onsubmit={handleCreateLane} style="margin-top: 1rem;">
				<div class="form-row-2">
					<div class="form-group">
						<label class="form-label" for="nl-num">Lane Number</label>
						<input id="nl-num" type="number" min="1" max="99" class="form-input" bind:value={newLaneNumber} required />
					</div>
					<div class="form-group">
						<label class="form-label" for="nl-cap">Max Throwers</label>
						<input id="nl-cap" type="number" min="1" max="20" class="form-input" bind:value={newLaneMaxThrowers} required />
					</div>
				</div>
				<div class="form-group" style="margin-top: 0.75rem;">
					<label class="form-label" for="nl-name">Display Name</label>
					<input id="nl-name" type="text" class="form-input" bind:value={newLaneName} required />
				</div>

				<div class="modal-actions" style="margin-top: 1.5rem;">
					<button type="button" class="btn btn-secondary" onclick={() => (showCreateLaneModal = false)}>Cancel</button>
					<button type="submit" class="btn btn-primary font-display" disabled={isCreatingLane}>
						{isCreatingLane ? 'Adding...' : '+ Add Lane'}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

<!-- EDIT LANE MODAL -->
{#if editingLane}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (editingLane = null)}
		onkeydown={(e) => {
			if (e.key === 'Escape') editingLane = null;
		}}
	>
		<div
			class="modal-card glass-panel"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<div class="modal-header-row">
				<div>
					<h3 class="modal-title font-display">Edit Lane Details</h3>
					<p class="editor-hint" style="margin-bottom: 0;">Update configuration for {editingLane.name}</p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (editingLane = null)}>✕</button>
			</div>

			<form onsubmit={handleUpdateLane} style="margin-top: 1rem;">
				<div class="form-row-2">
					<div class="form-group">
						<label class="form-label" for="el-num">Lane Number</label>
						<input id="el-num" type="number" min="1" max="99" class="form-input" bind:value={editLaneNumber} required />
					</div>
					<div class="form-group">
						<label class="form-label" for="el-cap">Max Throwers</label>
						<input id="el-cap" type="number" min="1" max="20" class="form-input" bind:value={editLaneMaxThrowers} required />
					</div>
				</div>
				<div class="form-group" style="margin-top: 0.75rem;">
					<label class="form-label" for="el-name">Display Name</label>
					<input id="el-name" type="text" class="form-input" bind:value={editLaneName} required />
				</div>

				<div class="modal-actions" style="margin-top: 1.5rem;">
					<button type="button" class="btn btn-secondary" onclick={() => (editingLane = null)}>Cancel</button>
					<button type="submit" class="btn btn-primary font-display" disabled={isUpdatingLane}>
						{isUpdatingLane ? 'Saving...' : 'Save Changes'}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

<!-- DELETE LANE MODAL -->
{#if deletingLane}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (deletingLane = null)}
		onkeydown={(e) => {
			if (e.key === 'Escape') deletingLane = null;
		}}
	>
		<div
			class="modal-card glass-panel modal-danger"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<h3 class="modal-title font-display" style="color: var(--accent-crimson);">Delete Lane</h3>
			<p style="color: var(--text-secondary); margin-top: 0.5rem; line-height: 1.5;">
				Are you sure you want to permanently remove <strong>{deletingLane.name}</strong>? This action cannot be undone.
			</p>
			<div class="modal-actions" style="margin-top: 1.5rem;">
				<button type="button" class="btn btn-secondary" onclick={() => (deletingLane = null)}>Cancel</button>
				<button type="button" class="btn btn-danger font-display" onclick={handleDeleteLane}>
					Delete Lane
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	.lanes-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
		gap: 1.5rem;
		align-items: stretch;
	}

	.lane-card {
		padding: 1.5rem;
		display: flex;
		flex-direction: column;
		gap: 1rem;
		background: rgba(15, 22, 36, 0.75);
		backdrop-filter: blur(16px);
		border: 1px solid rgba(255, 255, 255, 0.08);
		border-radius: var(--radius-lg);
		box-shadow: 0 4px 24px rgba(0, 0, 0, 0.35);
		transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
		position: relative;
		overflow: hidden;
	}

	.lane-card:hover {
		border-color: rgba(245, 158, 11, 0.35);
		box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5), 0 0 20px rgba(245, 158, 11, 0.08);
		transform: translateY(-2px);
	}

	.card-active {
		border-color: rgba(245, 158, 11, 0.6) !important;
		background: linear-gradient(180deg, rgba(30, 25, 15, 0.85) 0%, rgba(15, 22, 36, 0.9) 100%) !important;
		box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5), 0 0 24px rgba(245, 158, 11, 0.15) !important;
	}

	.lane-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding-bottom: 0.25rem;
	}

	.lane-name {
		font-size: 1.35rem;
		font-weight: 900;
		letter-spacing: -0.02em;
		color: #ffffff;
	}

	.session-box {
		background: rgba(10, 14, 23, 0.8);
		border: 1px solid rgba(245, 158, 11, 0.3);
		padding: 1rem 1.15rem;
		border-radius: var(--radius-md);
		display: flex;
		flex-direction: column;
		gap: 0.35rem;
	}

	.session-title {
		font-weight: 800;
		font-size: 1rem;
		color: #ffffff;
	}

	.timer-display {
		color: var(--accent-amber);
		font-weight: 900;
		font-size: 1.15rem;
		letter-spacing: 0.03em;
		text-shadow: 0 0 12px rgba(245, 158, 11, 0.4);
	}

	.extend-actions-row {
		display: flex;
		gap: 0.5rem;
		margin-top: 0.5rem;
	}

	.empty-lane-box {
		background: rgba(10, 14, 24, 0.5);
		border: 1px dashed rgba(255, 255, 255, 0.12);
		padding: 1.4rem;
		border-radius: var(--radius-md);
		text-align: center;
		display: flex;
		flex-direction: column;
		gap: 0.4rem;
		justify-content: center;
		min-height: 105px;
	}

	.capacity-tag {
		font-size: 0.82rem;
		color: var(--accent-amber);
		font-weight: 800;
		letter-spacing: 0.02em;
	}

	.next-booking-card {
		background: rgba(15, 23, 42, 0.85);
		border: 1px solid rgba(59, 130, 246, 0.4);
		border-left: 4px solid #3b82f6;
		border-radius: var(--radius-md);
		padding: 0.75rem 0.85rem;
		display: flex;
		flex-direction: column;
		gap: 0.3rem;
	}

	.pair-row {
		display: flex;
		align-items: center;
		gap: 0.85rem;
		background: rgba(10, 14, 23, 0.7);
		border: 1px solid rgba(255, 255, 255, 0.06);
		padding: 0.6rem 0.85rem;
		border-radius: var(--radius-md);
	}

	.lane-actions {
		display: flex;
		gap: 0.5rem;
		margin-top: auto;
		padding-top: 0.5rem;
	}

	.empty-lanes-state {
		text-align: center;
		padding: 4rem 2rem;
		border-radius: var(--radius-xl);
		display: flex;
		flex-direction: column;
		align-items: center;
		max-width: 600px;
		margin: 2rem auto;
		background: rgba(17, 21, 31, 0.85);
		border: 1px dashed var(--border-color);
	}

	.empty-icon {
		font-size: 3.5rem;
		margin-bottom: 1rem;
		display: block;
	}

	.empty-title {
		font-size: 1.5rem;
		font-weight: 800;
		color: var(--text-primary);
		margin-bottom: 0.5rem;
	}

	.empty-desc {
		color: var(--text-secondary);
		font-size: 0.95rem;
		line-height: 1.6;
		margin-bottom: 1.75rem;
	}
</style>

