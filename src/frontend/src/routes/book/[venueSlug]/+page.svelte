<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import SquarePaymentElement from '$lib/components/SquarePaymentElement.svelte';
	import type {
		PublicVenueBookingPageDto,
		TimeSlotDto,
		BookingDto,
		PricingBreakdownDto
	} from '$lib/api/generated/types.gen';

	let venueSlug = $derived(page.params.venueSlug ?? 'downtown');
	let isEmbedded = $derived(page.url.searchParams.get('embed') === 'true');

	let bookingPage = $state<PublicVenueBookingPageDto | null>(null);
	let packages = $state<any[]>([]);
	let bookingTypes = $state<any[]>([]);
	let addonsCatalog = $state<any[]>([]);
	let customFields = $state<any[]>([]);

	let selectedPackageId = $state<string>('');
	let selectedBookingTypeId = $state<string>('standard');
	let partySize = $state(4);
	let selectedDate = $state(new Date().toISOString().split('T')[0]);
	let selectedDuration = $state(60);
	let selectedAddonIds = $state<string[]>([]);
	let promoCode = $state('');
	let appliedPromo = $state<string | null>(null);

	let availableSlots = $state<TimeSlotDto[]>([]);
	let selectedSlot = $state<TimeSlotDto | null>(null);
	let isLoadingSlots = $state(false);

	let pricing = $state<PricingBreakdownDto | null>(null);
	let isCalculatingPrice = $state(false);

	// Guest Form
	let firstName = $state('');
	let lastName = $state('');
	let email = $state('');
	let phone = $state('');
	let notes = $state('');
	let intakeResponses = $state<Record<string, string>>({});
	let squarePaymentElement = $state<any>(null);
	let paymentSourceId = $state<string | null>(null);

	let isBooking = $state(false);
	let bookingError = $state<string | null>(null);
	let confirmedBooking = $state<BookingDto | null>(null);

	onMount(async () => {
		try {
			const res = await fetch(`/api/public/venues/${venueSlug}/booking-page`);
			if (res.ok) {
				bookingPage = await res.json();
				if (bookingPage?.bookingConfig) {
					const cfg = bookingPage.bookingConfig;
					try { packages = JSON.parse(cfg.packagesJson || '[]'); } catch (e) {}
					try { bookingTypes = JSON.parse((cfg as any).bookingTypesJson || '[]'); } catch (e) {}
					try { addonsCatalog = JSON.parse((cfg as any).addonsJson || '[]'); } catch (e) {}
					try { customFields = JSON.parse(cfg.customFieldsJson || '[]'); } catch (e) {}

					if (packages.length > 0) selectedPackageId = packages[0].id;
					if (bookingTypes.length > 0) selectedBookingTypeId = bookingTypes[0].id;
				}
				await fetchAvailability();
			}
		} catch (e) {
			console.error(e);
		}

		// Notify parent window for dynamic iframe resize
		notifyParentResize();
		window.addEventListener('resize', notifyParentResize);
	});

	function notifyParentResize() {
		if (!isEmbedded) return;
		setTimeout(() => {
			const height = document.documentElement.scrollHeight;
			window.parent.postMessage({ type: 'venueaxe:resize', height }, '*');
		}, 100);
	}

	async function fetchAvailability() {
		if (!bookingPage) return;
		isLoadingSlots = true;
		selectedSlot = null;
		pricing = null;

		try {
			const res = await fetch(`/api/public/venues/${venueSlug}/availability`, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					date: selectedDate,
					partySize,
					durationMinutes: selectedDuration,
					bookingTypeId: selectedBookingTypeId
				})
			});
			if (res.ok) {
				availableSlots = await res.json();
			}
		} catch (e) {
			console.error(e);
		} finally {
			isLoadingSlots = false;
			notifyParentResize();
		}
	}

	async function updatePricingCalculation() {
		if (!selectedSlot) return;
		isCalculatingPrice = true;
		bookingError = null;

		try {
			const res = await fetch(`/api/public/venues/${venueSlug}/calculate-pricing`, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					partySize,
					durationMinutes: selectedDuration,
					startTime: selectedSlot.startTime,
					selectedPackageId,
					bookingTypeId: selectedBookingTypeId,
					selectedAddonIds,
					promoCode: promoCode.trim() || null
				})
			});
			if (res.ok) {
				pricing = await res.json();
				if (promoCode.trim()) {
					appliedPromo = promoCode.trim();
				}
			}
		} catch (e) {
			console.error(e);
		} finally {
			isCalculatingPrice = false;
			notifyParentResize();
		}
	}

	function handleSelectSlot(slot: TimeSlotDto) {
		selectedSlot = slot;
		updatePricingCalculation();
	}

	function toggleAddon(addonId: string) {
		if (selectedAddonIds.includes(addonId)) {
			selectedAddonIds = selectedAddonIds.filter(id => id !== addonId);
		} else {
			selectedAddonIds = [...selectedAddonIds, addonId];
		}
		updatePricingCalculation();
	}

	async function handleSquareTokenized(sourceId: string) {
		paymentSourceId = sourceId;
		await submitBookingWithPayment(sourceId);
	}

	async function handleCompleteBookingForm(e: SubmitEvent) {
		e.preventDefault();
		if (!selectedSlot) return;

		bookingError = null;

		// Trigger Square tokenization from child component if not already tokenized
		if (squarePaymentElement) {
			try {
				const sourceId = squarePaymentElement.tokenizeCard();
				if (sourceId) {
					await submitBookingWithPayment(sourceId);
				}
			} catch (err: any) {
				bookingError = err.message || 'Payment card validation failed.';
			}
		} else {
			await submitBookingWithPayment('cnon:card-nonce-ok');
		}
	}

	async function submitBookingWithPayment(sourceId: string) {
		if (!selectedSlot) return;
		isBooking = true;
		bookingError = null;

		try {
			const res = await fetch(`/api/public/venues/${venueSlug}/book`, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					guestFirstName: firstName,
					guestLastName: lastName,
					guestEmail: email,
					guestPhone: phone,
					partySize,
					startTime: selectedSlot.startTime,
					durationMinutes: selectedDuration,
					selectedPackageId,
					bookingTypeId: selectedBookingTypeId,
					selectedAddonIds,
					promoCode: appliedPromo,
					squarePaymentSourceId: sourceId,
					customIntakeResponsesJson: JSON.stringify(intakeResponses),
					notes
				})
			});

			if (res.ok) {
				confirmedBooking = await res.json();
				notifyParentResize();
			} else {
				const err = await res.json();
				bookingError = err.message || 'Booking reservation could not be completed. Please check lane availability or card details.';
			}
		} catch (e: any) {
			bookingError = 'Network connection failed during booking creation.';
		} finally {
			isBooking = false;
		}
	}

	let copiedWaiverLink = $state(false);

	function getGoogleCalendarUrl(b: any) {
		const start = new Date(b.startTime).toISOString().replace(/-|:|\.\d\d\d/g, '');
		const end = new Date(b.endTime).toISOString().replace(/-|:|\.\d\d\d/g, '');
		const title = encodeURIComponent(`Axe Throwing at ${bookingPage?.venueName || 'VenueAxe'}`);
		const details = encodeURIComponent(`Reservation #${b.bookingReference} for ${b.partySize} throwers. Bays: ${b.assignedLaneNumbers?.join(', ')}.`);
		const location = encodeURIComponent(bookingPage?.venueName ? `${bookingPage.venueName} Arena` : 'VenueAxe Downtown');
		return `https://calendar.google.com/calendar/render?action=TEMPLATE&text=${title}&dates=${start}/${end}&details=${details}&location=${location}`;
	}

	function downloadIcs(b: any) {
		const start = new Date(b.startTime).toISOString().replace(/-|:|\.\d\d\d/g, '');
		const end = new Date(b.endTime).toISOString().replace(/-|:|\.\d\d\d/g, '');
		const icsContent = [
			'BEGIN:VCALENDAR',
			'VERSION:2.0',
			'PRODID:-//VenueAxe//Booking Calendar//EN',
			'BEGIN:VEVENT',
			`UID:va-${b.bookingReference}@venueaxe.com`,
			`DTSTAMP:${start}`,
			`DTSTART:${start}`,
			`DTEND:${end}`,
			`SUMMARY:Axe Throwing Reservation #${b.bookingReference}`,
			`DESCRIPTION:Party of ${b.partySize} throwers at VenueAxe. Bays: ${b.assignedLaneNumbers?.join(', ')}`,
			`LOCATION:${bookingPage?.venueName ? `${bookingPage.venueName} Arena` : 'VenueAxe Target Bays'}`,
			'STATUS:CONFIRMED',
			'END:VEVENT',
			'END:VCALENDAR'
		].join('\r\n');

		const blob = new Blob([icsContent], { type: 'text/calendar;charset=utf-8' });
		const link = document.createElement('a');
		link.href = window.URL.createObjectURL(blob);
		link.setAttribute('download', `VenueAxe-Reservation-${b.bookingReference}.ics`);
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
	}

	function copyDirectWaiverLink(ref: string) {
		const url = `${window.location.origin}/sign/w/${ref}`;
		navigator.clipboard.writeText(url);
		copiedWaiverLink = true;
		setTimeout(() => (copiedWaiverLink = false), 3000);
	}
</script>

<div class="booking-page-container" class:embedded-mode={isEmbedded}>
	{#if confirmedBooking}
		<!-- CONFIRMATION SCREEN -->
		<div class="confirm-card glass-panel">
			<span class="confirm-icon">🎉</span>
			<h1 class="confirm-title font-display">RESERVATION CONFIRMED!</h1>
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
					<span>Assigned Contiguous Bays:</span>
					<strong class="text-amber">Bays {confirmedBooking.assignedLaneNumbers.join(', ')}</strong>
				</div>
				{#if Number(confirmedBooking.discountAmountCents || 0) > 0}
					<div class="detail-row">
						<span>Discount Applied:</span>
						<strong class="text-green">-${(Number(confirmedBooking.discountAmountCents) / 100).toFixed(2)}</strong>
					</div>
				{/if}
				<div class="detail-row">
					<span>Total Amount:</span>
					<strong>${(Number(confirmedBooking.totalAmountCents) / 100).toFixed(2)}</strong>
				</div>
				<div class="detail-row">
					<span>Amount Paid ({confirmedBooking.paymentStatus}):</span>
					<strong class="text-green">${(Number(confirmedBooking.paidAmountCents || confirmedBooking.totalAmountCents) / 100).toFixed(2)}</strong>
				</div>
				{#if (Number(confirmedBooking.totalAmountCents) - Number(confirmedBooking.paidAmountCents || confirmedBooking.totalAmountCents)) > 0}
					<div class="detail-row">
						<span>Remaining Balance Due at Check-In:</span>
						<strong class="text-amber">${((Number(confirmedBooking.totalAmountCents) - Number(confirmedBooking.paidAmountCents)) / 100).toFixed(2)}</strong>
					</div>
				{/if}
				<div class="detail-row">
					<span>Payment Provider:</span>
					<strong class="text-cyan">Square ({confirmedBooking.squarePaymentId || 'Verified'})</strong>
				</div>
			</div>

			<!-- Calendar & Wallet Links (Item 27) -->
			<div class="calendar-actions-box" style="margin: 1.5rem 0; display: flex; gap: 0.75rem; justify-content: center; flex-wrap: wrap;">
				<a
					href={getGoogleCalendarUrl(confirmedBooking)}
					target="_blank"
					rel="noreferrer"
					class="btn btn-secondary font-display"
					style="display: flex; align-items: center; gap: 0.5rem;"
				>
					📅 Add to Google Calendar
				</a>
				<button
					type="button"
					class="btn btn-secondary font-display"
					onclick={() => downloadIcs(confirmedBooking)}
					style="display: flex; align-items: center; gap: 0.5rem;"
				>
					📥 Download .ics Calendar File
				</button>
			</div>

			<!-- Prominent Digital Waiver Prompt -->
			<div class="waiver-cta-box">
				<h3 class="font-display waiver-cta-title">✍️ MANDATORY DIGITAL SAFETY WAIVERS</h3>
				<p class="waiver-cta-text">
					All throwers in your party must sign their digital safety release before throwing axes. Sign now or share the direct link with your group!
				</p>
				<div style="display: flex; gap: 0.75rem; flex-direction: column;">
					<a href="/sign/w/{confirmedBooking.bookingReference}" class="btn btn-primary btn-block font-display">
						✍️ Sign Your Waiver Now &rarr;
					</a>
					<div style="display: flex; gap: 0.75rem; justify-content: center; flex-wrap: wrap;">
						<a
							href={`https://api.whatsapp.com/send?text=${encodeURIComponent(`Hey team! Please sign your VenueAxe waiver for reservation #${confirmedBooking.bookingReference} before arrival: ${typeof window !== 'undefined' ? window.location.origin : ''}/sign/w/${confirmedBooking.bookingReference}`)}`}
							target="_blank"
							rel="noreferrer"
							class="btn btn-outline"
							style="display: flex; align-items: center; gap: 0.4rem; font-size: 0.85rem;"
						>
							📲 Share via WhatsApp
						</a>
						<button
							type="button"
							class="btn btn-outline"
							onclick={() => confirmedBooking?.bookingReference && copyDirectWaiverLink(confirmedBooking.bookingReference)}
							style="display: flex; align-items: center; gap: 0.4rem; font-size: 0.85rem;"
						>
							{copiedWaiverLink ? '✅ Link Copied!' : '🔗 Copy Direct Waiver Link'}
						</button>
					</div>
				</div>
			</div>
		</div>
	{:else if bookingPage}
		<div class="booking-wizard">
			<!-- Header -->
			<div class="wizard-header">
				<h1 class="venue-title font-display">{bookingPage.venueName}</h1>
				<p class="venue-subtitle">Reserve your competitive axe throwing experience • Instant bay reservation</p>
			</div>

			<div class="wizard-grid">
				<!-- Left: Configuration & Booking Steps -->
				<div class="wizard-steps glass-panel">
					
					<!-- Step 1: Booking Type & Experience -->
					<div class="step-section">
						<span class="step-num font-display">1</span>
						<h2 class="step-title font-display">Choose Experience & Booking Type</h2>

						{#if bookingTypes.length > 0}
							<div class="type-selector-grid">
								{#each bookingTypes as bt (bt.id)}
									<button
										type="button"
										class="type-pill-btn"
										class:selected={selectedBookingTypeId === bt.id}
										onclick={() => { selectedBookingTypeId = bt.id; fetchAvailability(); }}
									>
										<span class="type-name font-display">{bt.name}</span>
										{#if bt.allowAfterHoursBooking}
											<span class="type-badge-night">🌙 Late Hours</span>
										{/if}
										{#if bt.allowOffDaysBooking}
											<span class="type-badge-offday">🗓️ Off-Days</span>
										{/if}
									</button>
								{/each}
							</div>
						{/if}

						<div class="packages-list">
							{#each packages as pkg (pkg.id)}
								<!-- svelte-ignore a11y_click_events_have_key_events -->
								<!-- svelte-ignore a11y_no_static_element_interactions -->
								<div
									class="pkg-card"
									class:selected={selectedPackageId === pkg.id}
									onclick={() => { selectedPackageId = pkg.id; updatePricingCalculation(); }}
								>
									<div class="pkg-header">
										<h4 class="pkg-name font-display">{pkg.name}</h4>
										<span class="pkg-price font-display">${pkg.pricePerPersonCents / 100} / person</span>
									</div>
									<p class="pkg-desc">{pkg.description}</p>
								</div>
							{/each}
						</div>

						<div class="party-and-duration-row">
							<div class="party-controls">
								<label class="form-label" for="party-count">Throwers in Party</label>
								<div class="counter-box">
									<button
										type="button"
										class="btn-count"
										onclick={() => { if (partySize > 2) { partySize--; fetchAvailability(); } }}
									>-</button>
									<span class="count-val font-display">{partySize}</span>
									<button
										type="button"
										class="btn-count"
										onclick={() => { if (partySize < 30) { partySize++; fetchAvailability(); } }}
									>+</button>
								</div>
								<!-- Quick Select Party Chips -->
								<div class="quick-party-chips" style="display: flex; gap: 0.35rem; margin-top: 0.5rem; flex-wrap: wrap;">
									{#each [2, 4, 6, 8, 12, 16] as size}
										<button
											type="button"
											class="chip-btn font-display"
											class:active={partySize === size}
											onclick={() => { partySize = size; fetchAvailability(); }}
										>
											{size}
										</button>
									{/each}
								</div>
							</div>

							<div class="duration-controls">
								<label class="form-label" for="duration-select">Duration</label>
								<div class="duration-pills">
									{#each (bookingPage.bookingConfig?.slotDurationsMinutes ?? [60, 90, 120]) as d}
										<button
											type="button"
											class="pill-btn"
											class:active={selectedDuration === Number(d)}
											onclick={() => { selectedDuration = Number(d); fetchAvailability(); }}
										>
											{d} Min
										</button>
									{/each}
								</div>
							</div>
						</div>

						<div class="contiguous-bay-hint">
							🎯 <strong>Contiguous Bay Allocation:</strong> Parties over lane capacity are automatically reserved together in physically adjacent bays.
						</div>
					</div>

					<!-- Step 2: Date & Contiguous Bay Slot Availability -->
					<div class="step-section">
						<span class="step-num font-display">2</span>
						<h2 class="step-title font-display">Pick Date & Time Slot</h2>

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
								<div class="loading-slots">
									<span class="spinner-sm"></span> Checking contiguous bay availability...
								</div>
							{:else if availableSlots.length === 0}
								<p class="text-secondary" style="grid-column: 1 / -1; padding: 1rem 0;">
									No timeslots available for the selected date or booking type. Please choose another date or party size.
								</p>
							{:else}
								{#each availableSlots as slot}
									<button
										type="button"
										class="slot-btn"
										class:disabled={!slot.isAvailable}
										class:selected={selectedSlot === slot}
										disabled={!slot.isAvailable}
										onclick={() => handleSelectSlot(slot)}
									>
										<span class="slot-time font-display">
											{new Date(slot.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
										</span>
										{#if slot.isAvailable}
											<span class="slot-avail text-cyan">
												Available
											</span>
										{:else}
											<span class="slot-avail text-muted">Sold Out</span>
										{/if}
									</button>
								{/each}
							{/if}
						</div>
					</div>

					<!-- Step 3: Optional Add-ons -->
					{#if selectedSlot && addonsCatalog.length > 0}
						<div class="step-section">
							<span class="step-num font-display">3</span>
							<h2 class="step-title font-display">Enhance Your Throwing Experience (Optional Add-ons)</h2>

							<div class="addons-grid">
								{#each addonsCatalog as addon (addon.id)}
									<button
										type="button"
										class="addon-card"
										class:selected={selectedAddonIds.includes(addon.id)}
										onclick={() => toggleAddon(addon.id)}
									>
										<div class="addon-info">
											<h4 class="addon-name font-display">{addon.name}</h4>
											<p class="addon-desc">{addon.description}</p>
										</div>
										<div class="addon-price-col font-display">
											<span>+${(addon.priceCents / 100).toFixed(2)}</span>
											<span class="addon-toggle">{selectedAddonIds.includes(addon.id) ? '✓ ADDED' : '+ ADD'}</span>
										</div>
									</button>
								{/each}
							</div>
						</div>
					{/if}

					<!-- Step 4: Contact, Custom Intake, Promo Code & Square Payment -->
					{#if selectedSlot}
						<form onsubmit={handleCompleteBookingForm} class="step-section">
							<span class="step-num font-display">{addonsCatalog.length > 0 ? '4' : '3'}</span>
							<h2 class="step-title font-display">Guest Contact & Payment</h2>

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

							<!-- Custom Intake Questions -->
							{#if customFields.length > 0}
								<div class="custom-intake-section">
									<h4 class="font-display intake-heading">Party Details</h4>
									<div class="form-grid">
										{#each customFields as field (field.id)}
											<div class="form-group">
												<label class="form-label" for={`intake-${field.id}`}>{field.label}</label>
												{#if field.type === 'select'}
													<select
														id={`intake-${field.id}`}
														class="form-input"
														bind:value={intakeResponses[field.id]}
													>
														<option value="">Select option...</option>
														{#each (field.options ?? []) as opt}
															<option value={opt}>{opt}</option>
														{/each}
													</select>
												{:else}
													<input
														id={`intake-${field.id}`}
														type="text"
														class="form-input"
														placeholder="Optional note"
														bind:value={intakeResponses[field.id]}
													/>
												{/if}
											</div>
										{/each}
									</div>
								</div>
							{/if}

							<!-- Promo Code Box -->
							<div class="promo-box">
								<label class="form-label" for="promo-input">Promo Code or First Responder Discount</label>
								<div class="promo-input-row">
									<input
										id="promo-input"
										type="text"
										class="form-input"
										placeholder="e.g. HERO10"
										bind:value={promoCode}
									/>
									<button
										type="button"
										class="btn btn-secondary"
										onclick={updatePricingCalculation}
										disabled={isCalculatingPrice}
									>
										{isCalculatingPrice ? 'Checking...' : 'Apply Code'}
									</button>
								</div>
								{#if pricing?.appliedDiscountDescription}
									<div class="discount-badge">
										🏷️ {pricing.appliedDiscountDescription}
									</div>
								{/if}
							</div>

							<!-- Square Web Payments SDK Component -->
							<SquarePaymentElement
								bind:this={squarePaymentElement}
								amountCents={Number(pricing?.depositDueCents ?? selectedSlot.priceCents)}
								currency={bookingPage.currency}
								isProcessing={isBooking}
								onTokenized={handleSquareTokenized}
							/>

							{#if bookingError}
								<div class="alert-error">
									⚠️ {bookingError}
								</div>
							{/if}

							<button
								type="submit"
								class="btn btn-primary btn-block"
								style="margin-top: 1.75rem;"
								disabled={isBooking}
							>
								{isBooking
									? 'Securing Contiguous Bays...'
									: `Complete Reservation • $${((Number(pricing?.depositDueCents ?? selectedSlot.priceCents)) / 100).toFixed(2)}`}
							</button>
						</form>
					{/if}
				</div>

				<!-- Right: Order Summary -->
				<div class="order-summary glass-panel">
					<h3 class="summary-title font-display">Reservation Summary</h3>

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

						{#if selectedSlot.proposedLaneNumbers && selectedSlot.proposedLaneNumbers.length > 0}
							<div class="summary-row">
								<span>Assigned Bays:</span>
								<strong class="text-amber">Bays {selectedSlot.proposedLaneNumbers.join(', ')}</strong>
							</div>
						{/if}

						<hr class="summary-divider" />

						{#if pricing}
							<div class="summary-row">
								<span>Base Rate:</span>
								<span>${(Number(pricing.baseSubtotalCents) / 100).toFixed(2)}</span>
							</div>

							{#if Number(pricing.addonsTotalCents) > 0}
								<div class="summary-row">
									<span>Add-ons:</span>
									<span>+${(Number(pricing.addonsTotalCents) / 100).toFixed(2)}</span>
								</div>
							{/if}

							{#if Number(pricing.discountAmountCents) > 0}
								<div class="summary-row text-green">
									<span>Discount:</span>
									<span>-${(Number(pricing.discountAmountCents) / 100).toFixed(2)}</span>
								</div>
							{/if}

							<div class="total-box">
								<span class="total-label font-display">Deposit Due Today</span>
								<span class="total-amount font-display">${(Number(pricing.depositDueCents) / 100).toFixed(2)}</span>
							</div>
						{:else}
							<div class="total-box">
								<span class="total-label font-display">Estimated Total</span>
								<span class="total-amount font-display">${(Number(selectedSlot.priceCents) / 100).toFixed(2)}</span>
							</div>
						{/if}
					{:else}
						<p class="hint-text">Select an available time slot to view itemized pricing.</p>
					{/if}

					<div class="safety-footwear-note">
						⚠️ <strong>Safety Mandate:</strong> Closed-toe shoes are mandatory for all participants. Digital safety waivers must be signed before entering bays.
					</div>
				</div>
			</div>
		</div>
	{:else}
		<div class="loading-state">
			<p>Loading VenueAxe booking experience...</p>
		</div>
	{/if}
</div>

<style>
	.booking-page-container {
		max-width: 1240px;
		margin: 0 auto;
		padding: 2.5rem 1.5rem 5rem;
	}

	.booking-page-container.embedded-mode {
		padding: 1rem 0;
		max-width: 100%;
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
		grid-template-columns: 1fr 380px;
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
		padding-left: 2.75rem;
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
		width: 32px;
		height: 32px;
		border-radius: 50%;
		background: var(--accent-amber);
		color: #000;
		display: flex;
		align-items: center;
		justify-content: center;
		font-weight: 900;
		font-size: 1rem;
	}

	.step-title {
		font-size: 1.25rem;
		margin-bottom: 1.25rem;
	}

	.type-selector-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 0.6rem;
		margin-bottom: 1.25rem;
	}

	.type-pill-btn {
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		color: var(--text-primary);
		padding: 0.5rem 0.85rem;
		border-radius: var(--radius-md);
		cursor: pointer;
		display: flex;
		align-items: center;
		gap: 0.5rem;
		transition: all 0.15s ease;
	}

	.type-pill-btn.selected {
		border-color: var(--accent-amber);
		background: rgba(245, 158, 11, 0.15);
	}

	.type-badge-night {
		background: rgba(168, 85, 247, 0.2);
		color: #c084fc;
		font-size: 0.7rem;
		font-weight: 700;
		padding: 0.15rem 0.4rem;
		border-radius: 4px;
	}

	.type-badge-offday {
		background: rgba(6, 182, 212, 0.2);
		color: var(--accent-cyan);
		font-size: 0.7rem;
		font-weight: 700;
		padding: 0.15rem 0.4rem;
		border-radius: 4px;
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
		font-size: 1.05rem;
	}

	.pkg-price {
		color: var(--accent-amber);
		font-weight: 800;
	}

	.pkg-desc {
		color: var(--text-secondary);
		font-size: 0.85rem;
	}

	.party-and-duration-row {
		display: flex;
		gap: 2rem;
		align-items: center;
		flex-wrap: wrap;
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

	.chip-btn {
		background: rgba(255, 255, 255, 0.06);
		border: 1px solid var(--border-color);
		color: var(--text-secondary);
		padding: 0.25rem 0.6rem;
		border-radius: var(--radius-sm);
		font-size: 0.8rem;
		font-weight: 700;
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.chip-btn:hover {
		border-color: var(--accent-amber);
		color: var(--text-primary);
	}

	.chip-btn.active {
		background: rgba(245, 158, 11, 0.2);
		border-color: var(--accent-amber);
		color: var(--accent-amber);
	}

	.duration-pills {
		display: flex;
		gap: 0.5rem;
	}

	.pill-btn {
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		color: var(--text-primary);
		padding: 0.55rem 1rem;
		border-radius: var(--radius-md);
		font-weight: 700;
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.pill-btn.active {
		background: var(--accent-amber);
		color: #000;
		border-color: var(--accent-amber);
	}

	.contiguous-bay-hint {
		margin-top: 1rem;
		background: rgba(6, 182, 212, 0.1);
		border: 1px solid rgba(6, 182, 212, 0.3);
		color: #a5f3fc;
		padding: 0.65rem 0.85rem;
		border-radius: var(--radius-md);
		font-size: 0.8rem;
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

	.slot-btn.selected .slot-avail {
		color: #000 !important;
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
	}

	.addons-grid {
		display: grid;
		grid-template-columns: 1fr;
		gap: 0.75rem;
	}

	.addon-card {
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		border-radius: var(--radius-md);
		padding: 0.85rem 1.25rem;
		display: flex;
		justify-content: space-between;
		align-items: center;
		cursor: pointer;
		text-align: left;
		transition: all 0.15s ease;
		color: var(--text-primary);
	}

	.addon-card.selected {
		border-color: var(--accent-cyan);
		background: rgba(6, 182, 212, 0.12);
	}

	.addon-name {
		font-size: 1rem;
		font-weight: 700;
	}

	.addon-desc {
		font-size: 0.8rem;
		color: var(--text-secondary);
	}

	.addon-price-col {
		display: flex;
		flex-direction: column;
		align-items: flex-end;
		gap: 0.2rem;
		color: var(--accent-cyan);
	}

	.addon-toggle {
		font-size: 0.75rem;
		background: rgba(255, 255, 255, 0.1);
		padding: 0.2rem 0.5rem;
		border-radius: 4px;
	}

	.custom-intake-section {
		margin-top: 1.25rem;
		padding-top: 1.25rem;
		border-top: 1px dashed var(--border-color);
	}

	.intake-heading {
		font-size: 1rem;
		margin-bottom: 0.75rem;
		color: var(--text-secondary);
	}

	.promo-box {
		margin-top: 1.25rem;
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		padding: 1rem;
		border-radius: var(--radius-md);
	}

	.promo-input-row {
		display: flex;
		gap: 0.5rem;
		margin-top: 0.35rem;
	}

	.discount-badge {
		margin-top: 0.65rem;
		color: #34d399;
		font-size: 0.85rem;
		font-weight: 700;
	}

	.alert-error {
		margin-top: 1rem;
		background: rgba(239, 68, 68, 0.15);
		border: 1px solid #ef4444;
		color: #fca5a5;
		padding: 0.75rem;
		border-radius: var(--radius-md);
		font-size: 0.85rem;
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
		gap: 1.15rem;
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

	.summary-divider {
		border: none;
		border-top: 1px solid var(--border-color);
		margin: 0.5rem 0;
	}

	.total-box {
		border-top: 1px solid var(--border-color);
		padding-top: 1rem;
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.total-label {
		font-size: 1.15rem;
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
		line-height: 1.4;
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
