<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { venueState } from '$lib/stores/venueState.svelte';
	import {
		getApiAdminBookingsVenueByVenueIdScheduleMatrix,
		getApiAdminLanesVenueByVenueIdAvailableForSlot,
		putApiAdminBookingsByIdReassignLane,
		putApiAdminBookingsByIdStatus
	} from '$lib/api/client';
	import CreateBookingModal from '$lib/components/admin/CreateBookingModal.svelte';
	import {
		getTodayDateString,
		getTimeInVenueTz,
		formatTimeInTz
	} from '$lib/utils/dateTime';
	import { createAdminHubConnection } from '$lib/services/signalr';
	import type * as signalR from '@microsoft/signalr';
	import type {
		LaneScheduleMatrixDto,
		ScheduleBookingBlockDto,
		LaneSlotOptionDto
	} from '$lib/api/generated/types.gen';

	let scheduleDate = $state<string>(getTodayDateString(venueState.selectedVenue?.timezone));
	let scheduleMatrix = $state<LaneScheduleMatrixDto | null>(null);
	let isLoadingMatrix = $state(false);

	let selectedBookingDetail = $state<ScheduleBookingBlockDto | null>(null);
	let showCreateModal = $state(false);
	let createModalPrefillLane = $state<number | null>(null);
	let createModalPrefillDate = $state<string>('');
	let createModalPrefillTime = $state<string>('');
	let bookingSuccessNotification = $state<string | null>(null);

	let availableLanesForSlot = $state<LaneSlotOptionDto[]>([]);
	let isLoadingAvailableLanes = $state(false);
	let isReassigningLane = $state(false);
	let reassignmentSuccessMessage = $state<string | null>(null);
	let reassignmentErrorMessage = $state<string | null>(null);

	// Timeline scrolling & "Now" line
	let matrixScrollContainer: HTMLDivElement | null = $state(null);
	let now = $state(new Date());
	let refreshInterval: any = null;

	// 24 hours timeline (0:00 to 23:00)
	const timelineHours = Array.from({ length: 24 }, (_, i) => i);
	const hourWidthPx = 100;
	const totalTimelineWidthPx = 24 * hourWidthPx;

	// Calculate current time position on the timeline in venue's timezone
	const venueNowTime = $derived.by(() => {
		return getTimeInVenueTz(now, venueState.selectedVenue?.timezone);
	});
	const nowMinutes = $derived(venueNowTime.hours * 60 + venueNowTime.minutes);
	const nowLeftPct = $derived((nowMinutes / (24 * 60)) * 100);
	const nowLeftPx = $derived((nowMinutes / (24 * 60)) * totalTimelineWidthPx);

	const isToday = $derived(scheduleDate === getTodayDateString(venueState.selectedVenue?.timezone));

	async function loadScheduleMatrix() {
		if (!venueState.selectedVenue) return;
		isLoadingMatrix = true;
		try {
			const res = await getApiAdminBookingsVenueByVenueIdScheduleMatrix({
				path: { venueId: venueState.selectedVenue.id },
				query: { date: scheduleDate }
			});
			scheduleMatrix = res.data || null;
		} catch (e) {
			console.error('Failed to load schedule matrix', e);
		} finally {
			isLoadingMatrix = false;
		}
	}

	function scrollToNow() {
		if (!matrixScrollContainer) return;
		const containerWidth = matrixScrollContainer.clientWidth;
		// 140px is lane name column offset
		const targetLeft = nowLeftPx + 140 - containerWidth / 2;
		matrixScrollContainer.scrollTo({
			left: Math.max(0, targetLeft),
			behavior: 'smooth'
		});
	}

	function changeScheduleDay(delta: number) {
		const [y, m, d] = scheduleDate.split('-').map(Number);
		const dateObj = new Date(y, m - 1, d);
		dateObj.setDate(dateObj.getDate() + delta);
		const ny = dateObj.getFullYear();
		const nm = String(dateObj.getMonth() + 1).padStart(2, '0');
		const nd = String(dateObj.getDate()).padStart(2, '0');
		scheduleDate = `${ny}-${nm}-${nd}`;
		loadScheduleMatrix();
	}

	function formatHourLabel(h: number): string {
		const period = h >= 12 ? 'PM' : 'AM';
		const display = h % 12 === 0 ? 12 : h % 12;
		return `${display} ${period}`;
	}

	function isSlotInFuture(dateStr: string, hour: number): boolean {
		const todayStr = getTodayDateString(venueState.selectedVenue?.timezone);
		if (dateStr > todayStr) return true;
		if (dateStr < todayStr) return false;
		return hour > venueNowTime.hours || (hour === venueNowTime.hours && venueNowTime.minutes < 45);
	}

	function calculateBlockStyle(startTimeIso: string, endTimeIso: string) {
		const start = new Date(startTimeIso);
		const end = new Date(endTimeIso);
		const timeInZone = getTimeInVenueTz(start, venueState.selectedVenue?.timezone);

		const startMinutes = timeInZone.hours * 60 + timeInZone.minutes;
		let durationMinutes = (end.getTime() - start.getTime()) / (1000 * 60);
		if (durationMinutes <= 0) durationMinutes = 60;

		const leftPct = (startMinutes / (24 * 60)) * 100;
		const widthPct = (durationMinutes / (24 * 60)) * 100;

		return { leftPct, widthPct };
	}

	async function inspectBooking(block: ScheduleBookingBlockDto) {
		selectedBookingDetail = block;
		reassignmentSuccessMessage = null;
		reassignmentErrorMessage = null;
		await loadSlotAvailableLanes(block);
	}

	async function loadSlotAvailableLanes(block: ScheduleBookingBlockDto) {
		if (!venueState.selectedVenue) return;
		isLoadingAvailableLanes = true;
		try {
			const start = new Date(block.startTime);
			const end = new Date(block.endTime);
			const duration = Math.max(30, Math.round((end.getTime() - start.getTime()) / (1000 * 60)));

			const res = await getApiAdminLanesVenueByVenueIdAvailableForSlot({
				path: { venueId: venueState.selectedVenue.id },
				query: {
					startTime: block.startTime,
					durationMinutes: duration
				}
			});
			availableLanesForSlot = res.data || [];
		} catch (e) {
			console.error('Failed to load available lanes for slot', e);
		} finally {
			isLoadingAvailableLanes = false;
		}
	}

	async function handleReassignLane(targetLaneId: string) {
		if (!selectedBookingDetail || !targetLaneId) return;
		isReassigningLane = true;
		reassignmentSuccessMessage = null;
		reassignmentErrorMessage = null;

		try {
			const res = await putApiAdminBookingsByIdReassignLane({
				path: { id: selectedBookingDetail.bookingId },
				body: { targetLaneId }
			});

			if (res.data) {
				reassignmentSuccessMessage = `Successfully reassigned to Lane ${res.data.assignedLaneNumbers.join(', ')}!`;
				// Update in-memory details
				selectedBookingDetail = {
					...selectedBookingDetail,
					laneNumbers: res.data.assignedLaneNumbers,
					laneIds: [targetLaneId]
				};
				await loadScheduleMatrix();
				await loadSlotAvailableLanes(selectedBookingDetail);
			} else {
				reassignmentErrorMessage = 'Could not reassign lane.';
			}
		} catch (err: any) {
			reassignmentErrorMessage = err?.message || 'Conflict occurred: lane may already be reserved.';
		} finally {
			isReassigningLane = false;
		}
	}

	async function handleUpdateBookingStatus(bookingId: string, status: number) {
		try {
			await putApiAdminBookingsByIdStatus({
				path: { id: bookingId },
				query: { status }
			});
			selectedBookingDetail = null;
			await loadScheduleMatrix();
		} catch (e) {
			console.error(e);
		}
	}

	$effect(() => {
		if (venueState.selectedVenue && scheduleDate) {
			loadScheduleMatrix();
		}
	});

	let hubConnection: signalR.HubConnection | null = null;

	onMount(async () => {
		loadScheduleMatrix();

		try {
			hubConnection = createAdminHubConnection();
			await hubConnection.start();
			await hubConnection.invoke('JoinAdminGroup');
			hubConnection.on('OnLaneStateChanged', () => {
				loadScheduleMatrix();
			});
		} catch (e) {
			console.warn('SignalR schedule connection warning:', e);
		}

		// Auto-refresh every 60 seconds (1 minute)
		refreshInterval = setInterval(() => {
			now = new Date();
			loadScheduleMatrix();
		}, 60000);

		// Center on "Now" on initial load
		setTimeout(() => {
			if (isToday) {
				scrollToNow();
			}
		}, 300);
	});

	onDestroy(() => {
		if (refreshInterval) {
			clearInterval(refreshInterval);
		}
		if (hubConnection) {
			hubConnection.stop();
			hubConnection = null;
		}
	});
</script>

<svelte:head>
	<title>Lane Schedule Matrix | VenueAxe Admin</title>
</svelte:head>

<!-- Clean Controls Bar (Verbose Headings Removed) -->
<div class="schedule-controls-bar glass-panel" style="display: flex; justify-content: space-between; align-items: center; padding: 0.85rem 1.25rem; margin-bottom: 1.25rem; flex-wrap: wrap; gap: 1rem;">
	<div style="display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap;">
		<button class="btn btn-secondary btn-sm font-display" onclick={() => changeScheduleDay(-1)}>
			&larr; Prev Day
		</button>
		<input
			type="date"
			class="form-input form-input-sm font-display"
			style="padding: 0.35rem 0.6rem; font-size: 0.85rem;"
			bind:value={scheduleDate}
			onchange={loadScheduleMatrix}
		/>
		<button class="btn btn-secondary btn-sm font-display" onclick={() => changeScheduleDay(1)}>
			Next Day &rarr;
		</button>
		<button
			class="btn btn-secondary btn-sm font-display"
			class:btn-today-active={isToday}
			onclick={() => {
				scheduleDate = getTodayDateString(venueState.selectedVenue?.timezone);
				loadScheduleMatrix();
			}}
		>
			Today
		</button>
		<button
			class="btn btn-primary btn-sm font-display"
			title="Scroll timeline to current time"
			onclick={scrollToNow}
		>
			📍 Now
		</button>
	</div>

	<div style="display: flex; align-items: center; gap: 0.75rem;">
		<span style="font-size: 0.8rem; color: var(--text-secondary); font-family: monospace;">
			Auto-refreshes every 1m • Last: {formatTimeInTz(now, venueState.selectedVenue?.timezone)}
		</span>
		<button class="btn btn-secondary btn-sm font-display" onclick={loadScheduleMatrix}>
			🔄 Refresh
		</button>
	</div>
</div>

{#if bookingSuccessNotification}
	<div class="alert-success" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; padding: 0.75rem 1.25rem; border-radius: var(--radius-md);">
		<span style="font-weight: 600;">✓ {bookingSuccessNotification}</span>
		<button type="button" class="btn-clear" onclick={() => (bookingSuccessNotification = null)}>✕</button>
	</div>
{/if}

<!-- Scrollable Timeline Matrix Container -->
{#if isLoadingMatrix && !scheduleMatrix}
	<div style="padding: 3rem; text-align: center; color: var(--text-secondary);">
		<p class="font-display">Loading timeline matrix...</p>
	</div>
{:else if scheduleMatrix}
	<div
		class="matrix-scroll-wrapper glass-panel"
		bind:this={matrixScrollContainer}
		style="overflow-x: auto; position: relative; border-radius: var(--radius-lg); padding: 1rem; background: rgba(15, 23, 42, 0.85); border: 1px solid var(--border-color);"
	>
		<div class="matrix-canvas" style="min-width: {totalTimelineWidthPx + 140}px; position: relative;">
			<!-- Red "Now" Line Indicator -->
			{#if isToday}
				<div
					class="matrix-now-indicator"
					style="position: absolute; top: 0; bottom: 0; left: calc(140px + {nowLeftPx}px); width: 2px; background: #ef4444; z-index: 25; pointer-events: none; box-shadow: 0 0 10px rgba(239, 68, 68, 0.9);"
				>
					<div
						class="matrix-now-badge font-mono"
						style="position: sticky; top: 0; transform: translateX(-50%); background: #ef4444; color: #fff; font-size: 0.68rem; font-weight: 800; padding: 0.15rem 0.4rem; border-radius: 4px; white-space: nowrap; box-shadow: 0 2px 4px rgba(0,0,0,0.5);"
					>
						NOW {formatTimeInTz(now, venueState.selectedVenue?.timezone)}
					</div>
				</div>
			{/if}

			<!-- Matrix Header (Lane Column + 24 Hour Columns) -->
			<div class="matrix-header-row" style="display: grid; grid-template-columns: 140px 1fr; border-bottom: 2px solid var(--border-color); padding-bottom: 0.5rem; margin-bottom: 0.5rem;">
				<div class="matrix-lane-col-header font-display" style="font-size: 0.75rem; color: var(--text-muted); letter-spacing: 0.08em; display: flex; align-items: center;">
					THROWING LANE
				</div>
				<div
					class="matrix-timeline-header"
					style="display: grid; grid-template-columns: repeat(24, {hourWidthPx}px); text-align: center;"
				>
					{#each timelineHours as h}
						<div class="time-col-header font-display" style="font-size: 0.72rem; color: var(--text-secondary); border-left: 1px solid rgba(255, 255, 255, 0.08); padding: 0.2rem 0;">
							{formatHourLabel(h)}
						</div>
					{/each}
				</div>
			</div>

			<!-- Matrix Lanes Rows -->
			<div class="matrix-body" style="display: flex; flex-direction: column; gap: 0.5rem;">
				{#each scheduleMatrix.lanes as lane (lane.id)}
					<div
						class="matrix-lane-row"
						class:lane-row-deactivated={!lane.isActive}
						style="display: grid; grid-template-columns: 140px 1fr; min-height: 54px; align-items: stretch; border-bottom: 1px solid rgba(255, 255, 255, 0.05); padding: 0.25rem 0;"
					>
						<div class="matrix-lane-cell" style="display: flex; flex-direction: column; justify-content: center; padding-right: 0.75rem;">
							<span class="matrix-lane-name font-display" style="font-size: 0.92rem; font-weight: 700; color: var(--text-primary);">
								{lane.name}
							</span>
							<span class="matrix-lane-cap" style="font-size: 0.7rem; color: var(--text-muted);">
								{#if !lane.isActive}
									<strong style="color: #f87171;">⊘ Deactivated</strong>
								{:else}
									Cap: {lane.maxThrowers} Throwers
								{/if}
							</span>
						</div>

						<!-- Timeline Track for this lane -->
						<div
							class="matrix-track"
							class:track-deactivated={!lane.isActive}
							style="position: relative; display: grid; grid-template-columns: repeat(24, {hourWidthPx}px); background: rgba(10, 15, 25, 0.65); border-radius: var(--radius-sm); overflow: hidden;"
						>
							{#if !lane.isActive}
								<div class="deactivated-stripe-overlay font-display">
									<span>⊘ LANE DEACTIVATED</span>
								</div>
							{/if}
							{#each timelineHours as h}
								{@const inFuture = isSlotInFuture(scheduleDate, h)}
								{@const canBook = lane.isActive && inFuture}
								<button
									type="button"
									class="track-hour-slot-btn"
									class:slot-disabled={!canBook}
									disabled={!canBook}
									title={!lane.isActive
										? `${lane.name} is deactivated`
										: !inFuture
										? `Past Slot: ${formatHourLabel(h)} on ${lane.name} (Cannot create bookings in the past)`
										: `Empty Slot: ${formatHourLabel(h)} on ${lane.name}. Click to reserve.`}
									onclick={() => {
										if (!canBook) return;
										createModalPrefillLane = Number(lane.laneNumber);
										createModalPrefillDate = scheduleDate;
										createModalPrefillTime = `${String(h).padStart(2, '0')}:00`;
										showCreateModal = true;
									}}
								></button>
							{/each}

							<!-- Bookings allocated to this lane -->
							{#each scheduleMatrix.bookings as b (b.bookingId)}
								{#if (b.laneNumbers || []).includes(lane.laneNumber)}
									{@const { leftPct, widthPct } = calculateBlockStyle(b.startTime, b.endTime)}
									<button
										type="button"
										class="booking-matrix-card"
										style="left: {leftPct}%; width: {widthPct}%; position: absolute; top: 4px; bottom: 4px; background: linear-gradient(135deg, rgba(245, 158, 11, 0.9), rgba(217, 119, 6, 0.95)); border: 1px solid rgba(251, 191, 36, 0.8); border-radius: var(--radius-sm); padding: 0.2rem 0.5rem; color: #fff; text-align: left; cursor: pointer; display: flex; flex-direction: column; justify-content: center; overflow: hidden; z-index: 10; box-shadow: 0 2px 4px rgba(0,0,0,0.4);"
										class:multi-lane={(b.laneNumbers || []).length > 1}
										onclick={() => inspectBooking(b)}
									>
										<span class="booking-matrix-title font-display" style="font-size: 0.82rem; font-weight: 700; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
											{b.guestName}
										</span>
										<span class="booking-matrix-meta font-mono" style="font-size: 0.68rem; opacity: 0.9; white-space: nowrap;">
											{b.partySize}p • {b.bookingReference}
										</span>
										{#if (b.laneNumbers || []).length > 1}
											<span class="contiguous-badge font-display" style="font-size: 0.62rem; background: rgba(0,0,0,0.4); padding: 0.1rem 0.3rem; border-radius: 3px; margin-top: 0.15rem; width: fit-content;">
												Lanes {b.laneNumbers.join('-')}
											</span>
										{/if}
									</button>
								{/if}
							{/each}
						</div>
					</div>
				{/each}
			</div>
		</div>
	</div>
{:else}
	<p class="text-secondary" style="padding: 2rem; text-align: center;">No schedule data available.</p>
{/if}

<!-- BOOKING INSPECTION & LANE REASSIGNMENT MODAL -->
{#if selectedBookingDetail}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (selectedBookingDetail = null)}
		onkeydown={(e) => {
			if (e.key === 'Escape') selectedBookingDetail = null;
		}}
	>
		<div
			class="modal-card glass-panel"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			style="max-width: 550px;"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<div class="modal-header-row">
				<div>
					<h3 class="modal-title font-display">Reservation Details</h3>
					<p class="editor-hint" style="margin-bottom: 0;">Ref: <strong style="color: var(--accent-amber);">{selectedBookingDetail.bookingReference}</strong></p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (selectedBookingDetail = null)}>✕</button>
			</div>

			{#if reassignmentSuccessMessage}
				<div class="alert-success" style="margin-top: 0.75rem;">
					✓ {reassignmentSuccessMessage}
				</div>
			{/if}

			{#if reassignmentErrorMessage}
				<div class="alert-error" style="margin-top: 0.75rem;">
					⚠️ {reassignmentErrorMessage}
				</div>
			{/if}

			<div style="margin-top: 1rem; display: flex; flex-direction: column; gap: 0.75rem;">
				<div class="form-row-2">
					<div>
						<span class="form-label" style="display: block;">Guest Name</span>
						<strong class="font-display" style="font-size: 1.05rem;">{selectedBookingDetail.guestName}</strong>
					</div>
					<div>
						<span class="form-label" style="display: block;">Party Size</span>
						<strong class="font-display" style="font-size: 1.05rem;">{selectedBookingDetail.partySize} Throwers</strong>
					</div>
				</div>

				<div class="form-row-2">
					<div>
						<span class="form-label" style="display: block;">Start Time</span>
						<span class="font-mono">{formatTimeInTz(selectedBookingDetail.startTime, venueState.selectedVenue?.timezone)}</span>
					</div>
					<div>
						<span class="form-label" style="display: block;">End Time</span>
						<span class="font-mono">{formatTimeInTz(selectedBookingDetail.endTime, venueState.selectedVenue?.timezone)}</span>
					</div>
				</div>

				<div class="form-row-2">
					<div>
						<span class="form-label" style="display: block;">Payment Status</span>
						<span class="badge badge-available font-display">{selectedBookingDetail.paymentStatus}</span>
					</div>
					<div>
						<span class="form-label" style="display: block;">Signed Waivers</span>
						<span class="font-display" style="color: var(--accent-amber); font-weight: 700;">
							{selectedBookingDetail.signedWaiverCount} / {selectedBookingDetail.partySize} Signed
						</span>
					</div>
				</div>

				<!-- ASSIGNED LANES SELECTOR DROPDOWN (DELIV-4.2) -->
				<div class="form-group" style="margin-top: 0.5rem; padding: 1rem; background: rgba(10, 15, 25, 0.7); border-radius: var(--radius-md); border: 1px solid var(--border-color);">
					<label class="form-label font-display" for="reassign-lane-select" style="color: var(--accent-amber); font-size: 0.85rem;">
						🎯 Assigned Lane (Change to Reassign):
					</label>
					{#if isLoadingAvailableLanes}
						<p style="font-size: 0.82rem; color: var(--text-secondary); margin-top: 0.25rem;">Checking lane availability for this timeslot...</p>
					{:else}
						<select
							id="reassign-lane-select"
							class="form-input"
							style="margin-top: 0.35rem; font-weight: 700;"
							disabled={isReassigningLane}
							value={selectedBookingDetail.laneIds && selectedBookingDetail.laneIds.length > 0 ? selectedBookingDetail.laneIds[0] : ''}
							onchange={(e) => handleReassignLane(e.currentTarget.value)}
						>
							{#each availableLanesForSlot as opt (opt.laneId)}
								{@const isCurrent = (selectedBookingDetail.laneNumbers || []).includes(opt.laneNumber)}
								<option value={opt.laneId} disabled={!opt.isAvailable && !isCurrent}>
									{opt.name} {#if isCurrent}(Current Assignment){:else if opt.isAvailable}(Available){:else}- [{opt.conflictReason}]{/if}
								</option>
							{/each}
						</select>
						<p style="font-size: 0.75rem; color: var(--text-muted); margin-top: 0.35rem;">
							Selecting another available lane instantly saves the new assignment to the database.
						</p>
					{/if}
				</div>
			</div>

			<div class="modal-actions" style="margin-top: 1.5rem;">
				{#if selectedBookingDetail.status !== 2}
					<button
						type="button"
						class="btn btn-primary font-display"
						onclick={() => handleUpdateBookingStatus(selectedBookingDetail!.bookingId, 2)}
					>
						Check In Party
					</button>
				{/if}
				{#if selectedBookingDetail.status !== 4}
					<button
						type="button"
						class="btn btn-secondary btn-delete font-display"
						onclick={() => handleUpdateBookingStatus(selectedBookingDetail!.bookingId, 4)}
					>
						Cancel Reservation
					</button>
				{/if}
				<button
					type="button"
					class="btn btn-secondary"
					onclick={() => (selectedBookingDetail = null)}
				>
					Close
				</button>
			</div>
		</div>
	</div>
{/if}

<!-- CREATE RESERVATION MODAL (IN-PLACE FOR LANE MATRIX) -->
<CreateBookingModal
	isOpen={showCreateModal}
	prefillLaneNumber={createModalPrefillLane}
	prefillDate={createModalPrefillDate}
	prefillStartTime={createModalPrefillTime}
	onClose={() => {
		showCreateModal = false;
	}}
	onSuccess={async (newBooking) => {
		showCreateModal = false;
		bookingSuccessNotification = `Reservation created successfully for ${newBooking.guestFirstName} ${newBooking.guestLastName}! Ref: ${newBooking.bookingReference}`;
		setTimeout(() => {
			bookingSuccessNotification = null;
		}, 6000);
		await loadScheduleMatrix();
	}}
/>

<style>
	.track-hour-slot-btn {
		border: none;
		background: transparent;
		border-left: 1px solid rgba(255, 255, 255, 0.05);
		height: 100%;
		padding: 0;
		cursor: pointer;
		transition: background 0.15s ease;
	}

	.track-hour-slot-btn:hover:not(:disabled) {
		background: rgba(245, 158, 11, 0.18);
	}

	.track-hour-slot-btn.slot-disabled {
		cursor: not-allowed;
		background: rgba(0, 0, 0, 0.28);
		opacity: 0.35;
	}

	.track-hour-slot-btn.slot-disabled:hover {
		background: rgba(0, 0, 0, 0.28);
	}

	.matrix-lane-row.lane-row-deactivated {
		background: rgba(15, 23, 42, 0.4);
		opacity: 0.78;
	}

	.matrix-track.track-deactivated {
		background: repeating-linear-gradient(
			-45deg,
			rgba(15, 23, 42, 0.92),
			rgba(15, 23, 42, 0.92) 12px,
			rgba(30, 41, 59, 0.65) 12px,
			rgba(30, 41, 59, 0.65) 24px
		) !important;
	}

	.deactivated-stripe-overlay {
		position: absolute;
		inset: 0;
		background: repeating-linear-gradient(
			-45deg,
			rgba(15, 23, 42, 0.85),
			rgba(15, 23, 42, 0.85) 14px,
			rgba(51, 65, 85, 0.42) 14px,
			rgba(51, 65, 85, 0.42) 28px
		);
		display: flex;
		align-items: center;
		justify-content: center;
		pointer-events: none;
		z-index: 12;
	}

	.deactivated-stripe-overlay span {
		background: rgba(15, 23, 42, 0.92);
		border: 1px dashed rgba(239, 68, 68, 0.6);
		padding: 0.25rem 1.25rem;
		border-radius: 4px;
		font-size: 0.75rem;
		font-weight: 800;
		letter-spacing: 0.12em;
		color: #f87171;
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.6);
	}
</style>
