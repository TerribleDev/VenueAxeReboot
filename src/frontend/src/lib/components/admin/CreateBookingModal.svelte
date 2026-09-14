<script lang="ts">
	import { venueState } from '$lib/stores/venueState.svelte';
	import {
		postApiAdminBookings,
		getApiAdminLanesVenueByVenueIdAvailableForSlot
	} from '$lib/api/client';
	import { localSlotToUtcIso, getTodayDateString } from '$lib/utils/dateTime';
	import type { BookingDto, LaneSlotOptionDto } from '$lib/api/generated/types.gen';

	interface Props {
		isOpen: boolean;
		prefillLaneNumber?: number | null;
		prefillDate?: string;
		prefillStartTime?: string;
		onClose: () => void;
		onSuccess: (booking: BookingDto) => void;
	}

	let {
		isOpen = false,
		prefillLaneNumber = null,
		prefillDate = '',
		prefillStartTime = '',
		onClose,
		onSuccess
	}: Props = $props();

	let firstName = $state('');
	let lastName = $state('');
	let email = $state('');
	let phone = $state('');
	let partySize = $state(2);
	let bookingDate = $state(new Date().toISOString().split('T')[0]);
	let startTime = $state('17:00');
	let durationMinutes = $state(60);
	let laneMode = $state<'auto' | 'specific'>('auto');
	let specificLaneNumber = $state<number | null>(null);
	let paymentMethod = $state('Cash');
	let paymentStatus = $state('Pending');
	let notes = $state('');
	let autoCheckIn = $state(false);

	let isSubmitting = $state(false);
	let errorMessage = $state<string | null>(null);

	let allLanesForSlot = $state<LaneSlotOptionDto[]>([]);
	let isLoadingLanes = $state(false);
	let wasOpen = $state(false);

	// Derived: available lanes
	const availableLanes = $derived(allLanesForSlot.filter((l) => l.isAvailable));

	// Synchronize initial state ONLY when modal transitions from closed to open
	$effect(() => {
		if (isOpen && !wasOpen) {
			wasOpen = true;
			errorMessage = null;
			firstName = '';
			lastName = '';
			email = '';
			phone = '';
			partySize = 2;
			bookingDate = prefillDate || getTodayDateString(venueState.selectedVenue?.timezone);
			startTime = prefillStartTime || '17:00';
			durationMinutes = 60;
			paymentMethod = 'Cash';
			paymentStatus = 'Pending';
			notes = '';
			autoCheckIn = false;

			if (prefillLaneNumber !== null && prefillLaneNumber !== undefined) {
				laneMode = 'specific';
				specificLaneNumber = Number(prefillLaneNumber);
			} else {
				laneMode = 'auto';
				specificLaneNumber = null;
			}

			loadAvailableLanes();
		} else if (!isOpen && wasOpen) {
			wasOpen = false;
		}
	});

	// Reactively refresh available lanes whenever date, time, duration, or venue changes while modal is open
	$effect(() => {
		if (isOpen && wasOpen) {
			const _d = bookingDate;
			const _t = startTime;
			const _dur = durationMinutes;
			const _vId = venueState.selectedVenue?.id;
			if (_d && _t && _vId) {
				loadAvailableLanes();
			}
		}
	});

	async function loadAvailableLanes() {
		if (!venueState.selectedVenue || !isOpen) return;
		isLoadingLanes = true;
		try {
			const startUtcIso = localSlotToUtcIso(bookingDate, startTime, venueState.selectedVenue?.timezone);
			const res = await getApiAdminLanesVenueByVenueIdAvailableForSlot({
				path: { venueId: venueState.selectedVenue.id },
				query: {
					startTime: startUtcIso,
					durationMinutes: Number(durationMinutes)
				}
			});

			allLanesForSlot = res.data || [];

			// If specificLaneNumber is selected, check if it exists in allLanesForSlot
			if (specificLaneNumber !== null) {
				const currentOpt = allLanesForSlot.find((l) => l.laneNumber === specificLaneNumber);
				if (!currentOpt || !currentOpt.isAvailable) {
					// If the requested prefilled lane is not available, default to first available lane
					const firstAvail = allLanesForSlot.find((l) => l.isAvailable);
					if (firstAvail && !prefillLaneNumber) {
						specificLaneNumber = Number(firstAvail.laneNumber);
					}
				}
			} else if (laneMode === 'specific') {
				const firstAvail = allLanesForSlot.find((l) => l.isAvailable);
				if (firstAvail) {
					specificLaneNumber = Number(firstAvail.laneNumber);
				}
			}
		} catch (e) {
			console.error('Failed to load available lanes for timeslot', e);
			allLanesForSlot = [];
		} finally {
			isLoadingLanes = false;
		}
	}

	async function handleSubmit(e: SubmitEvent) {
		e.preventDefault();
		if (!venueState.selectedVenue) return;

		isSubmitting = true;
		errorMessage = null;

		try {
			const startUtcIso = localSlotToUtcIso(bookingDate, startTime, venueState.selectedVenue?.timezone);
			const specificLanes =
				laneMode === 'specific' && specificLaneNumber !== null
					? [Number(specificLaneNumber)]
					: undefined;

			const res = await postApiAdminBookings({
				body: {
					venueId: venueState.selectedVenue.id,
					guestFirstName: firstName.trim(),
					guestLastName: lastName.trim(),
					guestEmail: email.trim() || undefined,
					guestPhone: phone.trim() || undefined,
					partySize: Number(partySize),
					startTime: startUtcIso,
					durationMinutes: Number(durationMinutes),
					specificLaneNumbers: specificLanes,
					paymentMethod,
					paymentStatus,
					notes: notes.trim() || undefined,
					autoCheckIn
				}
			});

			if (res.data) {
				onSuccess(res.data);
			} else {
				errorMessage = 'Could not reserve lane. Target lane may be occupied or unavailable.';
			}
		} catch (err: any) {
			errorMessage = err?.message || 'Failed to create reservation. Please verify details and try again.';
		} finally {
			isSubmitting = false;
		}
	}
</script>

{#if isOpen}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={onClose}
		onkeydown={(e) => {
			if (e.key === 'Escape') onClose();
		}}
	>
		<div
			class="modal-card glass-panel"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			style="max-width: 620px;"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<div class="modal-header-row" style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 1.25rem;">
				<div>
					<h3 class="modal-title font-display" style="font-size: 1.25rem; margin: 0; color: #fff;">
						+ Create Reservation
					</h3>
					<p class="editor-hint" style="margin: 0.25rem 0 0 0; font-size: 0.82rem; color: var(--text-secondary);">
						Reserve throwing lanes for a group or walk-in guest.
					</p>
				</div>
				<button type="button" class="btn-clear" onclick={onClose} aria-label="Close modal">✕</button>
			</div>

			{#if errorMessage}
				<div class="alert-error" style="margin-bottom: 1rem; padding: 0.65rem 0.85rem; font-size: 0.85rem; border-radius: var(--radius-sm);">
					⚠️ {errorMessage}
				</div>
			{/if}

			<form onsubmit={handleSubmit}>
				<div class="form-row-2">
					<div class="form-group">
						<label class="form-label" for="booking-first-name">First Name *</label>
						<input
							id="booking-first-name"
							type="text"
							class="form-input"
							bind:value={firstName}
							required
							placeholder="Jane"
						/>
					</div>
					<div class="form-group">
						<label class="form-label" for="booking-last-name">Last Name *</label>
						<input
							id="booking-last-name"
							type="text"
							class="form-input"
							bind:value={lastName}
							required
							placeholder="Doe"
						/>
					</div>
				</div>

				<div class="form-row-2" style="margin-top: 0.75rem;">
					<div class="form-group">
						<label class="form-label" for="booking-email">Email (Optional)</label>
						<input
							id="booking-email"
							type="email"
							class="form-input"
							bind:value={email}
							placeholder="guest@example.com"
						/>
					</div>
					<div class="form-group">
						<label class="form-label" for="booking-phone">Phone (Optional)</label>
						<input
							id="booking-phone"
							type="tel"
							class="form-input"
							bind:value={phone}
							placeholder="555-0199"
						/>
					</div>
				</div>

				<div class="form-row-3" style="margin-top: 0.75rem;">
					<div class="form-group">
						<label class="form-label" for="booking-party-size">Party Size *</label>
						<input
							id="booking-party-size"
							type="number"
							min="1"
							max="50"
							class="form-input"
							bind:value={partySize}
							required
						/>
					</div>
					<div class="form-group">
						<label class="form-label" for="booking-date">Date *</label>
						<input
							id="booking-date"
							type="date"
							class="form-input"
							bind:value={bookingDate}
							onchange={loadAvailableLanes}
							required
						/>
					</div>
					<div class="form-group">
						<label class="form-label" for="booking-time">Start Time *</label>
						<input
							id="booking-time"
							type="time"
							class="form-input"
							bind:value={startTime}
							onchange={loadAvailableLanes}
							required
						/>
					</div>
				</div>

				<div class="form-row-2" style="margin-top: 0.75rem;">
					<div class="form-group">
						<label class="form-label" for="booking-duration">Duration</label>
						<select
							id="booking-duration"
							class="form-input"
							bind:value={durationMinutes}
							onchange={loadAvailableLanes}
						>
							<option value={30}>30 Minutes</option>
							<option value={60}>60 Minutes (Standard)</option>
							<option value={90}>90 Minutes</option>
							<option value={120}>120 Minutes (2 Hours)</option>
						</select>
					</div>
					<div class="form-group">
						<label class="form-label" for="booking-lane-mode">Target Lane Assignment</label>
						<select
							id="booking-lane-mode"
							class="form-input"
							bind:value={laneMode}
							onchange={() => {
								if (laneMode === 'specific' && specificLaneNumber === null && availableLanes.length > 0) {
									specificLaneNumber = Number(availableLanes[0].laneNumber);
								}
							}}
						>
							<option value="auto">Auto-Allocate Contiguous Lanes</option>
							<option value="specific">Assign Specific Lane</option>
						</select>
					</div>
				</div>

				<!-- Dynamic Lane Selector (When Specific Lane is Chosen) -->
				{#if laneMode === 'specific'}
					<div
						class="form-group"
						style="margin-top: 0.75rem; padding: 0.85rem; background: rgba(10, 15, 25, 0.65); border-radius: var(--radius-md); border: 1px solid var(--border-color);"
					>
						<label class="form-label font-display" for="booking-specific-lane" style="color: var(--accent-amber); font-size: 0.85rem;">
							🎯 Select Target Lane for {startTime} ({bookingDate}):
						</label>
						{#if isLoadingLanes}
							<p style="font-size: 0.82rem; color: var(--text-secondary); margin-top: 0.35rem;">
								Checking lane availability for this timeslot...
							</p>
						{:else if allLanesForSlot.length === 0}
							<div class="alert-error" style="margin-top: 0.5rem; font-size: 0.85rem; padding: 0.5rem 0.75rem;">
								⚠️ No lanes found for this venue.
							</div>
						{:else if availableLanes.length === 0}
							<div class="alert-error" style="margin-top: 0.5rem; font-size: 0.85rem; padding: 0.5rem 0.75rem;">
								⚠️ All lanes are reserved or deactivated for this timeslot. Choose another time or use auto-allocate.
							</div>
						{:else}
							<select
								id="booking-specific-lane"
								class="form-input"
								style="margin-top: 0.35rem; font-weight: 600;"
								bind:value={specificLaneNumber}
								required
							>
								{#each allLanesForSlot as l (l.laneId)}
									<option value={l.laneNumber} disabled={!l.isAvailable}>
										{l.name} {#if l.isAvailable}(Available){:else}- [{l.conflictReason || 'Reserved'}]{/if}
									</option>
								{/each}
							</select>
						{/if}
					</div>
				{/if}

				<div class="form-row-2" style="margin-top: 0.75rem;">
					<div class="form-group">
						<label class="form-label" for="booking-payment-method">Payment Method</label>
						<select id="booking-payment-method" class="form-input" bind:value={paymentMethod}>
							<option value="Cash">💵 Cash at Counter</option>
							<option value="PosTerminal">💳 Card / POS Terminal</option>
							<option value="Comp">🎁 Comp / VIP / House Guest</option>
							<option value="SquareCard">📱 Square Card / Digital</option>
							<option value="Unpaid">⏳ Unpaid / Pay Later</option>
						</select>
					</div>
					<div class="form-group">
						<label class="form-label" for="booking-payment-status">Payment Status</label>
						<select id="booking-payment-status" class="form-input" bind:value={paymentStatus}>
							<option value="Pending">Payment Pending</option>
							<option value="PaidInFull">Paid In Full</option>
							<option value="DepositPaid">Deposit Paid</option>
						</select>
					</div>
				</div>

				<div class="form-group" style="margin-top: 0.75rem;">
					<label class="form-label" for="booking-notes">Internal Notes (Optional)</label>
					<input
						id="booking-notes"
						type="text"
						class="form-input"
						bind:value={notes}
						placeholder="Party notes, birthday celebration, etc."
					/>
				</div>

				<div class="checkbox-row" style="margin-top: 1rem;">
					<label class="checkbox-label" style="display: flex; align-items: center; gap: 0.5rem; cursor: pointer;">
						<input type="checkbox" bind:checked={autoCheckIn} />
						<span style="font-size: 0.88rem;"><strong>Immediate Check-In:</strong> Check party in immediately upon booking</span>
					</label>
				</div>

				<div class="modal-actions" style="margin-top: 1.5rem; display: flex; justify-content: flex-end; gap: 0.75rem;">
					<button type="button" class="btn btn-secondary" onclick={onClose}>
						Cancel
					</button>
					<button type="submit" class="btn btn-primary font-display" disabled={isSubmitting}>
						{isSubmitting ? 'Reserving...' : '+ Create Reservation'}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
