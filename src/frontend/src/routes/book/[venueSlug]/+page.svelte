<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import {
		getApiPublicVenuesByVenueSlugBookingPage,
		postApiPublicVenuesByVenueSlugAvailability,
		postApiPublicVenuesByVenueSlugBook
	} from '$lib/api/client';
	import type {
		PublicVenueBookingPageDto,
		TimeSlotDto,
		BookingDto
	} from '$lib/api/generated/types.gen';

	let venueSlug = $derived(page.params.venueSlug ?? 'downtown');

	let bookingPage = $state<PublicVenueBookingPageDto | null>(null);
	let packages = $state<any[]>([]);
	let selectedPackageId = $state<string>('');
	let partySize = $state(4);
	let selectedDate = $state(new Date().toISOString().split('T')[0]);
	let selectedDuration = $state(60);

	let availableSlots = $state<TimeSlotDto[]>([]);
	let selectedSlot = $state<TimeSlotDto | null>(null);
	let isLoadingSlots = $state(false);

	// Guest Form
	let firstName = $state('');
	let lastName = $state('');
	let email = $state('');
	let phone = $state('');
	let notes = $state('');
	let isBooking = $state(false);
	let confirmedBooking = $state<BookingDto | null>(null);

	onMount(async () => {
		try {
			const res = await getApiPublicVenuesByVenueSlugBookingPage({
				path: { venueSlug }
			});
			if (res.data) {
				bookingPage = res.data;
				if (bookingPage.bookingConfig?.packagesJson) {
					try {
						packages = JSON.parse(bookingPage.bookingConfig.packagesJson);
						if (packages.length > 0) selectedPackageId = packages[0].id;
					} catch (e) {}
				}
				await fetchAvailability();
			}
		} catch (e) {
			console.error(e);
		}
	});

	async function fetchAvailability() {
		if (!bookingPage) return;
		isLoadingSlots = true;
		selectedSlot = null;

		try {
			const res = await postApiPublicVenuesByVenueSlugAvailability({
				path: { venueSlug },
				body: {
					date: selectedDate as any,
					partySize,
					durationMinutes: selectedDuration
				}
			});
			if (res.data) availableSlots = res.data;
		} catch (e) {
			console.error(e);
		} finally {
			isLoadingSlots = false;
		}
	}

	async function handleCompleteBooking(e: SubmitEvent) {
		e.preventDefault();
		if (!selectedSlot) return;

		isBooking = true;
		try {
			const res = await postApiPublicVenuesByVenueSlugBook({
				path: { venueSlug },
				body: {
					guestFirstName: firstName,
					guestLastName: lastName,
					guestEmail: email,
					guestPhone: phone,
					partySize,
					startTime: selectedSlot.startTime,
					durationMinutes: selectedDuration,
					selectedPackageId,
					customIntakeResponsesJson: null,
					notes
				}
			});
			if (res.data) {
				confirmedBooking = res.data;
			}
		} catch (e) {
			alert('Booking failed. Please try again.');
		} finally {
			isBooking = false;
		}
	}
</script>

<div class="booking-page-container">
	{#if confirmedBooking}
		<!-- CONFIRMATION SCREEN -->
		<div class="confirm-card glass-panel">
			<span class="confirm-icon">🎉</span>
			<h1 class="confirm-title font-display">BOOKING CONFIRMED!</h1>
			<p class="confirm-ref font-display">Reference: <span class="text-amber">{confirmedBooking.bookingReference}</span></p>

			<div class="confirm-details">
				<div class="detail-row">
					<span>Guest Name:</span>
					<strong>{confirmedBooking.guestFirstName} {confirmedBooking.guestLastName}</strong>
				</div>
				<div class="detail-row">
					<span>Date & Time:</span>
					<strong>{new Date(confirmedBooking.startTime).toLocaleString()}</strong>
				</div>
				<div class="detail-row">
					<span>Party Size:</span>
					<strong>{confirmedBooking.partySize} Throwers</strong>
				</div>
				<div class="detail-row">
					<span>Assigned Lanes:</span>
					<strong>Lanes {confirmedBooking.assignedLaneNumbers.join(', ')}</strong>
				</div>
				<div class="detail-row">
					<span>Total Paid:</span>
					<strong>${(Number(confirmedBooking.totalAmountCents) / 100).toFixed(2)}</strong>
				</div>
			</div>

			<!-- Prominent Digital Waiver Prompt -->
			<div class="waiver-cta-box">
				<h3 class="font-display waiver-cta-title">✍️ MANDATORY SAFETY WAIVERS</h3>
				<p class="waiver-cta-text">
					All throwers in your party must sign a digital safety waiver before entering the throwing bays.
				</p>
				<a href="/sign/{venueSlug}?ref={confirmedBooking.bookingReference}" class="btn btn-primary btn-block">
					Sign Digital Waiver Now &rarr;
				</a>
			</div>
		</div>
	{:else if bookingPage}
		<div class="booking-wizard">
			<!-- Header -->
			<div class="wizard-header">
				<h1 class="venue-title font-display">{bookingPage.venueName}</h1>
				<p class="venue-subtitle">Reserve your competitive axe throwing experience</p>
			</div>

			<div class="wizard-grid">
				<!-- Left: Configuration Form -->
				<div class="wizard-steps glass-panel">
					<!-- Step 1: Package & Party -->
					<div class="step-section">
						<span class="step-num font-display">1</span>
						<h2 class="step-title font-display">Select Package & Party Size</h2>

						<div class="packages-list">
							{#each packages as pkg (pkg.id)}
								<!-- svelte-ignore a11y_click_events_have_key_events -->
								<!-- svelte-ignore a11y_no_static_element_interactions -->
								<div
									class="pkg-card"
									class:selected={selectedPackageId === pkg.id}
									onclick={() => (selectedPackageId = pkg.id)}
								>
									<div class="pkg-header">
										<h4 class="pkg-name font-display">{pkg.name}</h4>
										<span class="pkg-price font-display">${pkg.pricePerPersonCents / 100} / person</span>
									</div>
									<p class="pkg-desc">{pkg.description}</p>
								</div>
							{/each}
						</div>

						<div class="party-controls">
							<label class="form-label" for="party-count">Throwers in Party</label>
							<div class="counter-box">
								<button type="button" class="btn-count" onclick={() => { if (partySize > 2) { partySize--; fetchAvailability(); } }}>-</button>
								<span class="count-val font-display">{partySize}</span>
								<button type="button" class="btn-count" onclick={() => { if (partySize < 24) { partySize++; fetchAvailability(); } }}>+</button>
							</div>
						</div>
					</div>

					<!-- Step 2: Date & Slot Availability -->
					<div class="step-section">
						<span class="step-num font-display">2</span>
						<h2 class="step-title font-display">Choose Date & Time</h2>

						<div class="form-group" style="margin-bottom: 1.25rem;">
							<label class="form-label" for="book-date">Select Date</label>
							<input
								id="book-date"
								type="date"
								class="form-input"
								bind:value={selectedDate}
								onchange={fetchAvailability}
							/>
						</div>

						<div class="slots-grid">
							{#if isLoadingSlots}
								<p class="text-secondary">Checking lane availability...</p>
							{:else}
								{#each availableSlots as slot}
									<button
										type="button"
										class="slot-btn"
										class:disabled={!slot.isAvailable}
										class:selected={selectedSlot === slot}
										disabled={!slot.isAvailable}
										onclick={() => (selectedSlot = slot)}
									>
										<span class="slot-time font-display">
											{new Date(slot.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
										</span>
										<span class="slot-avail">
											{slot.isAvailable ? `${slot.availableLanesCount} bays open` : 'Sold Out'}
										</span>
									</button>
								{/each}
							{/if}
						</div>
					</div>

					<!-- Step 3: Contact & Checkout -->
					{#if selectedSlot}
						<form onsubmit={handleCompleteBooking} class="step-section">
							<span class="step-num font-display">3</span>
							<h2 class="step-title font-display">Guest Contact Details</h2>

							<div class="form-grid">
								<div class="form-group">
									<label class="form-label" for="first-name">First Name</label>
									<input id="first-name" type="text" class="form-input" bind:value={firstName} required />
								</div>
								<div class="form-group">
									<label class="form-label" for="last-name">Last Name</label>
									<input id="last-name" type="text" class="form-input" bind:value={lastName} required />
								</div>
							</div>

							<div class="form-grid" style="margin-top: 1rem;">
								<div class="form-group">
									<label class="form-label" for="book-email">Email Address</label>
									<input id="book-email" type="email" class="form-input" bind:value={email} required />
								</div>
								<div class="form-group">
									<label class="form-label" for="book-phone">Mobile Phone</label>
									<input id="book-phone" type="tel" class="form-input" bind:value={phone} required />
								</div>
							</div>

							<button type="submit" class="btn btn-primary btn-block" style="margin-top: 1.5rem;" disabled={isBooking}>
								{isBooking ? 'Securing Lanes...' : `Complete Reservation • $${(Number(selectedSlot.priceCents) / 100).toFixed(2)}`}
							</button>
						</form>
					{/if}
				</div>

				<!-- Right: Order Summary -->
				<div class="order-summary glass-panel">
					<h3 class="summary-title font-display">Booking Summary</h3>

					<div class="summary-row">
						<span>Venue:</span>
						<strong>{bookingPage.venueName}</strong>
					</div>

					<div class="summary-row">
						<span>Party Size:</span>
						<strong>{partySize} Throwers</strong>
					</div>

					<div class="summary-row">
						<span>Duration:</span>
						<strong>{selectedDuration} Minutes</strong>
					</div>

					{#if selectedSlot}
						<div class="summary-row">
							<span>Selected Slot:</span>
							<strong>{new Date(selectedSlot.startTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })}</strong>
						</div>

						<div class="total-box">
							<span class="total-label font-display">Total Due</span>
							<span class="total-amount font-display">${(Number(selectedSlot.priceCents) / 100).toFixed(2)}</span>
						</div>
					{:else}
						<p class="hint-text">Select a time slot to see total.</p>
					{/if}

					<div class="safety-footwear-note">
						⚠️ Closed-toe shoes are mandatory for all throwers.
					</div>
				</div>
			</div>
		</div>
	{:else}
		<div class="loading-state">
			<p>Loading venue booking experience...</p>
		</div>
	{/if}
</div>

<style>
	.booking-page-container {
		max-width: 1200px;
		margin: 0 auto;
		padding: 2.5rem 1.5rem 5rem;
	}

	.wizard-header {
		text-align: center;
		margin-bottom: 2.5rem;
	}

	.venue-title {
		font-size: 2.6rem;
		font-weight: 900;
	}

	.venue-subtitle {
		color: var(--text-secondary);
		margin-top: 0.25rem;
	}

	.wizard-grid {
		display: grid;
		grid-template-columns: 1fr 360px;
		gap: 2rem;
		align-items: start;
	}

	.wizard-steps {
		padding: 2rem;
		display: flex;
		flex-direction: column;
		gap: 2rem;
	}

	.step-section {
		position: relative;
		padding-left: 2.5rem;
		border-bottom: 1px solid var(--border-color);
		padding-bottom: 2rem;
	}

	.step-section:last-child {
		border-bottom: none;
		padding-bottom: 0;
	}

	.step-num {
		position: absolute;
		left: 0;
		top: 0;
		width: 28px;
		height: 28px;
		border-radius: 50%;
		background: var(--accent-amber);
		color: #000;
		display: flex;
		align-items: center;
		justify-content: center;
		font-weight: 900;
		font-size: 0.9rem;
	}

	.step-title {
		font-size: 1.25rem;
		margin-bottom: 1.25rem;
	}

	.packages-list {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
		margin-bottom: 1.5rem;
	}

	.pkg-card {
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		border-radius: var(--radius-md);
		padding: 1rem 1.25rem;
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.pkg-card.selected {
		border-color: var(--accent-amber);
		background: rgba(245, 158, 11, 0.1);
	}

	.pkg-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 0.35rem;
	}

	.pkg-name {
		font-weight: 700;
		font-size: 1rem;
	}

	.pkg-price {
		color: var(--accent-amber);
		font-weight: 800;
	}

	.pkg-desc {
		color: var(--text-secondary);
		font-size: 0.85rem;
	}

	.counter-box {
		display: inline-flex;
		align-items: center;
		gap: 1.5rem;
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		padding: 0.5rem 1rem;
		border-radius: var(--radius-md);
	}

	.btn-count {
		width: 32px;
		height: 32px;
		border-radius: 50%;
		border: 1px solid var(--border-color);
		background: var(--bg-surface-elevated);
		color: var(--text-primary);
		font-size: 1.2rem;
		font-weight: 800;
		cursor: pointer;
	}

	.count-val {
		font-size: 1.4rem;
		font-weight: 900;
		color: var(--accent-amber);
	}

	.slots-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(130px, 1fr));
		gap: 0.75rem;
	}

	.slot-btn {
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		color: var(--text-primary);
		padding: 0.75rem 0.5rem;
		border-radius: var(--radius-md);
		cursor: pointer;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.25rem;
		transition: all 0.15s ease;
	}

	.slot-btn.selected {
		background: var(--accent-amber);
		color: #000;
		border-color: var(--accent-amber);
		font-weight: 800;
	}

	.slot-btn.disabled {
		opacity: 0.35;
		cursor: not-allowed;
	}

	.slot-time {
		font-size: 1.1rem;
		font-weight: 700;
	}

	.slot-avail {
		font-size: 0.75rem;
		color: var(--text-muted);
	}

	.slot-btn.selected .slot-avail {
		color: #000;
	}

	.form-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1rem;
	}

	.btn-block {
		width: 100%;
	}

	.order-summary {
		padding: 2rem;
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
	}

	.summary-title {
		font-size: 1.3rem;
		border-bottom: 1px solid var(--border-color);
		padding-bottom: 0.75rem;
	}

	.summary-row {
		display: flex;
		justify-content: space-between;
		font-size: 0.95rem;
	}

	.total-box {
		border-top: 1px solid var(--border-color);
		padding-top: 1rem;
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.total-label {
		font-size: 1.2rem;
		font-weight: 800;
	}

	.total-amount {
		font-size: 1.8rem;
		font-weight: 900;
		color: var(--accent-amber);
	}

	.safety-footwear-note {
		background: rgba(245, 158, 11, 0.1);
		border: 1px solid var(--accent-amber);
		color: #fbbf24;
		padding: 0.75rem;
		border-radius: var(--radius-md);
		font-size: 0.8rem;
		margin-top: 1rem;
	}

	/* Confirmation Card */
	.confirm-card {
		max-width: 600px;
		margin: 3rem auto;
		padding: 3rem;
		text-align: center;
	}

	.confirm-icon { font-size: 3.5rem; margin-bottom: 1rem; display: block; }
	.confirm-title { font-size: 2.2rem; font-weight: 900; }
	.confirm-ref { font-size: 1.2rem; margin-bottom: 2rem; }

	.confirm-details {
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		border-radius: var(--radius-md);
		padding: 1.25rem;
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
		text-align: left;
		margin-bottom: 2rem;
	}

	.detail-row {
		display: flex;
		justify-content: space-between;
		font-size: 0.95rem;
	}

	.waiver-cta-box {
		background: rgba(6, 182, 212, 0.15);
		border: 1px solid var(--accent-cyan);
		border-radius: var(--radius-lg);
		padding: 1.5rem;
		text-align: left;
	}

	.waiver-cta-title { color: var(--accent-cyan); font-size: 1.1rem; margin-bottom: 0.5rem; }
	.waiver-cta-text { font-size: 0.9rem; color: var(--text-secondary); margin-bottom: 1.25rem; }

	@media (max-width: 900px) {
		.wizard-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
