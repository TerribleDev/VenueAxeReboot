<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import WaiverCanvas from '$lib/components/WaiverCanvas.svelte';
	import {
		getApiWaiversTemplateByVenueSlug,
		postApiWaiversSign
	} from '$lib/api/client';
	import type { WaiverTemplateDto, WaiverDto, BookingDto } from '$lib/api/generated/types.gen';

	let bookingReference = $derived(page.params.bookingReference ?? '');
	let booking = $state<BookingDto | null>(null);
	let isLoadingBooking = $state(true);
	let bookingError = $state<string | null>(null);

	let template = $state<WaiverTemplateDto | null>(null);
	let firstName = $state('');
	let lastName = $state('');
	let email = $state('');
	let phone = $state('');
	let dob = $state('');
	let isGuardian = $state(false);
	let minorList = $state<string[]>(['']);
	let signaturePng = $state('');
	let emailMarketingOptIn = $state(true);
	let isSubmitting = $state(false);
	let signedWaiver = $state<WaiverDto | null>(null);
	let submitError = $state<string | null>(null);
	let termsAccepted = $state(false);
	let canvasKey = $state(0);

	function addMinor() {
		minorList = [...minorList, ''];
	}

	function removeMinor(index: number) {
		if (minorList.length > 1) {
			minorList = minorList.filter((_, i) => i !== index);
		} else {
			minorList = [''];
		}
	}

	function resetForNextSigner() {
		signedWaiver = null;
		firstName = '';
		lastName = '';
		email = '';
		phone = '';
		dob = '';
		isGuardian = false;
		minorList = [''];
		signaturePng = '';
		submitError = null;
		termsAccepted = false;
		canvasKey++;
	}

	onMount(async () => {
		try {
			// 1. Fetch booking by reference
			const res = await fetch(`/api/public/bookings/${bookingReference}`);
			if (res.ok) {
				booking = await res.json();
			} else {
				bookingError = 'Reservation reference not found. You can still sign a general waiver.';
			}
		} catch (e: any) {
			bookingError = e.message;
		} finally {
			isLoadingBooking = false;
		}

		// 2. Fetch template
		try {
			const resTmplByBooking = await fetch(`/api/waivers/template/by-booking/${bookingReference}`);
			const bookingSlug = (booking as any)?.venueSlug;
			if (resTmplByBooking.ok) {
				template = await resTmplByBooking.json();
			} else if (bookingSlug) {
				const resTmpl = await getApiWaiversTemplateByVenueSlug({
					path: { venueSlug: bookingSlug }
				});
				if (resTmpl.data) {
					template = resTmpl.data;
				}
			} else {
				const resTmpl = await getApiWaiversTemplateByVenueSlug({
					path: { venueSlug: 'downtown' }
				});
				if (resTmpl.data) {
					template = resTmpl.data;
				}
			}
		} catch (e) {
			console.error('Failed to load waiver template', e);
		}
	});

	function handleSignatureDrawn(dataUrl: string) {
		signaturePng = dataUrl;
		submitError = null;
	}

	let copiedLink = $state(false);

	function copyPartyLink() {
		navigator.clipboard.writeText(window.location.href);
		copiedLink = true;
		setTimeout(() => (copiedLink = false), 2500);
	}

	const calculatedAge = $derived.by(() => {
		if (!dob) return null;
		const birthDate = new Date(dob);
		if (isNaN(birthDate.getTime())) return null;
		const today = new Date();
		let age = today.getFullYear() - birthDate.getFullYear();
		const m = today.getMonth() - birthDate.getMonth();
		if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
			age--;
		}
		return age;
	});

	const isMinorSigner = $derived(calculatedAge !== null && calculatedAge < 18);

	function validateIntake(): string | null {
		if (!firstName.trim()) return 'Please enter your legal first name.';
		if (!lastName.trim()) return 'Please enter your legal last name.';
		if (!email.trim() || !email.includes('@')) return 'Please enter a valid email address.';
		if (!dob) return 'Please enter your date of birth.';
		const birthDate = new Date(dob);
		if (isNaN(birthDate.getTime()) || birthDate > new Date()) return 'Please enter a valid past date of birth.';
		if (isMinorSigner && !isGuardian) {
			return 'Participants under 18 cannot sign independently. A parent or legal guardian must sign on their behalf.';
		}
		if (isGuardian && calculatedAge !== null && calculatedAge < 18) {
			return 'A parent or legal guardian must be at least 18 years of age.';
		}
		const validMinors = minorList.map((n) => n.trim()).filter((n) => n.length > 0);
		if (isGuardian && validMinors.length === 0) return 'Please enter at least one minor participant name covered by your signature.';
		if (!signaturePng) return 'Please sign with your finger or mouse in the signature box below.';
		if (!termsAccepted) return 'You must check the box agreeing to the liability release terms.';
		return null;
	}

	async function handleSubmitWaiver(e: Event) {
		e.preventDefault();
		const validationError = validateIntake();
		if (validationError) {
			submitError = validationError;
			return;
		}

		if (!template) {
			submitError = 'Waiver template unavailable. Please try again.';
			return;
		}

		isSubmitting = true;
		submitError = null;

		try {
			const validMinors = minorList.map((n) => n.trim()).filter((n) => n.length > 0);
			const cleanedMinors = isGuardian && validMinors.length > 0
				? JSON.stringify(validMinors.map((name) => ({ name })))
				: null;

			const res = await postApiWaiversSign({
				body: {
					templateId: template.id,
					bookingId: booking?.id ?? null,
					bookingReference: bookingReference || null,
					signerFirstName: firstName.trim(),
					signerLastName: lastName.trim(),
					signerEmail: email.trim(),
					signerPhone: phone.trim() || '',
					emailMarketingOptIn,
					dateOfBirth: dob as any,
					isGuardianSigning: isGuardian,
					minorsCoveredJson: cleanedMinors,
					signatureImagePngBase64: signaturePng,
					signatureVectorSvg: null,
					userAgent: typeof navigator !== 'undefined' ? navigator.userAgent : 'Browser'
				} as any
			});

			if (res.data) {
				signedWaiver = res.data;
				if (booking) {
					booking.signedWaiverCount = (Number(booking.signedWaiverCount) || 0) + 1;
				}
			} else {
				const err = res.error as any;
				if (err?.errors) {
					const messages = Object.values(err.errors).flat().join(' ');
					submitError = messages || err.title || 'Validation error submitting waiver.';
				} else {
					submitError = err?.detail ?? err?.title ?? err?.message ?? 'Submission failed. Please verify your details.';
				}
			}
		} catch (err: any) {
			submitError = err?.message ?? 'An unexpected error occurred while saving waiver.';
		} finally {
			isSubmitting = false;
		}
	}
</script>

<svelte:head>
	<title>Sign Waiver for Party #{bookingReference} | VenueAxe</title>
</svelte:head>

<div class="waiver-viewport">
	<header class="waiver-header glass-panel">
		<div class="header-content">
			<a href="/" class="brand-logo font-display">
				<span class="axe-icon">🪓</span> VENUE<span class="text-amber">AXE</span>
			</a>
			<div class="header-badges">
				<span class="badge badge-amber font-display">DIRECT PARTY PASS</span>
				<span class="badge badge-cyan font-display">REF #{bookingReference}</span>
			</div>
		</div>
	</header>

	<main class="waiver-container">
		{#if signedWaiver}
			<!-- SUCCESS SCREEN -->
			<div class="success-card glass-panel">
				<div class="success-icon">✅</div>
				<h1 class="font-display success-title">WAIVER SIGNED & VERIFIED!</h1>
				<p class="success-subtitle">
					Thank you, <strong>{signedWaiver.signerFirstName} {signedWaiver.signerLastName}</strong>. Your liability waiver has been attached to reservation #{bookingReference}.
				</p>

				<div class="hash-box">
					<span class="hash-label">IMMUTABLE WAIVER AUDIT ID:</span>
					<code class="hash-code">{signedWaiver.id}</code>
					<span class="timestamp">STAMPED: {new Date(signedWaiver.signedAtUtc).toLocaleString()}</span>
				</div>

				<div class="success-actions">
					<a href="/book/downtown" class="btn btn-outline font-display">Back to Venue</a>
					<button class="btn btn-primary font-display" onclick={resetForNextSigner}>
						✍️ Sign Another Thrower
					</button>
				</div>
			</div>
		{:else}
			<div class="waiver-layout">
				<!-- Booking Party Summary & Waiver Checklist (Pit 2) -->
				{#if booking}
					{@const totalParty = Number(booking.partySize ?? 1)}
					{@const signedCount = Number(booking.signedWaiverCount ?? 0)}
					{@const remaining = Math.max(0, totalParty - signedCount)}
					{@const pct = Math.min(100, Math.round((signedCount / (totalParty || 1)) * 100))}

					<div class="booking-banner glass-panel">
						<div class="party-info">
							<span class="info-label">Group Reservation:</span>
							<h3 class="font-display party-name">{booking.guestFirstName} {booking.guestLastName}'s Party</h3>
							<p class="party-meta">
								📅 {new Date(booking.startTime).toLocaleDateString()} at {new Date(booking.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
								&bull; 👥 {totalParty} Throwers &bull; Target Bays: {booking.assignedLaneNumbers?.join(', ') || 'Auto'}
							</p>

							<div class="party-share-row">
								<button class="btn btn-sm btn-secondary font-display" onclick={copyPartyLink}>
									{copiedLink ? '✓ Link Copied!' : '🔗 Copy Shareable Waiver Link'}
								</button>
								<span class="share-hint">Send link to your group so throwers can sign before arriving</span>
							</div>
						</div>

						<div class="waiver-counter-box">
							<div class="waiver-counter">
								<span class="waiver-num font-display" class:complete={remaining === 0}>
									{signedCount} / {totalParty}
								</span>
								<span class="waiver-label">Waivers Signed</span>
							</div>

							<div class="waiver-progress-track">
								<div class="waiver-progress-fill" style="width: {pct}%;"></div>
							</div>

							{#if remaining > 0}
								<span class="waiver-status-badge badge-warning">
									⚠️ {remaining} more {remaining === 1 ? 'waiver' : 'waivers'} needed
								</span>
							{:else}
								<span class="waiver-status-badge badge-success">
									✓ Party 100% Cleared
								</span>
							{/if}
						</div>
					</div>
				{/if}

				<!-- Agreement Legal Text -->
				<section class="legal-card glass-panel">
					<h2 class="font-display section-title">{template?.title ?? 'Participant Release & Liability Waiver'}</h2>
					<p class="section-version">Version {template?.versionNumber ?? 1} &bull; Required for lane entry</p>

					<div class="legal-scrollbox">
						{#if template}
							<div class="markdown-body">
								{@html (template.bodyTextMarkdown ?? '').replace(/\n\n/g, '<p>').replace(/\n/g, '<br/>')}
							</div>
						{:else}
							<p class="loading-text">Loading agreement text...</p>
						{/if}
					</div>
				</section>

				<!-- Signer Intake Form -->
				<section class="form-card glass-panel">
					<h3 class="font-display form-title">Thrower Information</h3>
					<p class="form-subtitle">All fields marked with an asterisk (*) are legally required.</p>

					{#if submitError}
						<div class="alert alert-error">{submitError}</div>
					{/if}

					<form onsubmit={handleSubmitWaiver} class="waiver-form">
						<div class="form-row-2">
							<div class="form-group">
								<label class="form-label" for="w-first">Legal First Name *</label>
								<input id="w-first" type="text" class="form-input" bind:value={firstName} required placeholder="Marcus" />
							</div>
							<div class="form-group">
								<label class="form-label" for="w-last">Legal Last Name *</label>
								<input id="w-last" type="text" class="form-input" bind:value={lastName} required placeholder="Vance" />
							</div>
						</div>

						<div class="form-row-2">
							<div class="form-group">
								<label class="form-label" for="w-email">Email Address *</label>
								<input id="w-email" type="email" class="form-input" bind:value={email} required placeholder="marcus@example.com" />
							</div>
							<div class="form-group">
								<label class="form-label" for="w-phone">Phone Number</label>
								<input id="w-phone" type="tel" class="form-input" bind:value={phone} placeholder="555-0199" />
							</div>
						</div>

						<div class="form-group">
							<label class="form-label" for="w-dob">Date of Birth *</label>
							<input id="w-dob" type="date" class="form-input" bind:value={dob} required />
						</div>

						<!-- Minor Guardian Checkbox -->
						<div class="checkbox-group">
							<label class="checkbox-label" for="w-guardian">
								<input id="w-guardian" type="checkbox" bind:checked={isGuardian} />
								<span>I am signing as parent or legal guardian for minor participant(s) under 18</span>
							</label>
						</div>

						{#if isGuardian}
							<div class="form-group minor-box" style="display: flex; flex-direction: column; gap: 0.5rem;">
								<label class="form-label" for="w-minor-0">Minor Participants Covered by Signature *</label>
								<div style="display: flex; flex-direction: column; gap: 0.5rem;">
									{#each minorList as _, idx}
										<div class="minor-row" style="display: flex; gap: 0.5rem; align-items: center;">
											<input
												id={`w-minor-${idx}`}
												type="text"
												class="form-input"
												bind:value={minorList[idx]}
												placeholder={`Minor #${idx + 1} Full Name & Age`}
												style="flex: 1;"
											/>
											{#if minorList.length > 1}
												<button
													type="button"
													class="btn-remove-minor"
													onclick={() => removeMinor(idx)}
													title="Remove minor"
													style="background: rgba(239, 68, 68, 0.15); border: 1px solid rgba(239, 68, 68, 0.4); color: #f87171; border-radius: 6px; padding: 0.55rem 0.85rem; cursor: pointer; font-size: 0.9rem;"
												>
													✕
												</button>
											{/if}
										</div>
									{/each}
								</div>
								<button
									type="button"
									class="btn-add-minor"
									onclick={addMinor}
									style="margin-top: 0.35rem; align-self: flex-start; background: rgba(59, 130, 246, 0.15); border: 1px dashed rgba(59, 130, 246, 0.5); color: #60a5fa; border-radius: 6px; padding: 0.45rem 0.9rem; font-size: 0.85rem; cursor: pointer; display: inline-flex; align-items: center; gap: 0.35rem;"
								>
									+ Add Minor
								</button>
							</div>
						{/if}

						<!-- Vector Signature Pad -->
						<div class="signature-section">
							<div class="sig-header">
								<label class="form-label" for="sig-pad">Drawn Signature *</label>
								<span class="sig-hint">Sign with finger or stylus inside box</span>
							</div>
							{#key canvasKey}
								<WaiverCanvas onchange={(png) => (signaturePng = png)} />
							{/key}
						</div>

						<div class="agreement-acknowledgement">
							<label class="checkbox-label" for="w-ack" style="cursor: pointer; display: flex; align-items: center; gap: 0.75rem; width: 100%;">
								<input id="w-ack" type="checkbox" bind:checked={termsAccepted} style="width: 1.3rem; height: 1.3rem; accent-color: var(--accent-amber); cursor: pointer; flex-shrink: 0;" />
								<span style="user-select: none; font-size: 0.95rem; line-height: 1.4;">I acknowledge that I have read, understood, and agree to the terms of the liability release.</span>
							</label>
						</div>

						<div class="agreement-acknowledgement" style="margin-top: 0.75rem;">
							<label class="checkbox-label" for="w-marketing-optin" style="cursor: pointer; display: flex; align-items: center; gap: 0.75rem; width: 100%;">
								<input id="w-marketing-optin" type="checkbox" bind:checked={emailMarketingOptIn} style="width: 1.3rem; height: 1.3rem; accent-color: var(--accent-amber); cursor: pointer; flex-shrink: 0;" />
								<span style="user-select: none; font-size: 0.9rem; line-height: 1.4; color: var(--text-secondary);">Keep me updated on league news, tournaments, and exclusive promotional discounts via email.</span>
							</label>
						</div>

						<button type="submit" class="btn btn-primary btn-submit font-display" disabled={isSubmitting}>
							{isSubmitting ? 'Verifying & Submitting...' : 'Complete & Sign Waiver →'}
						</button>
					</form>
				</section>
			</div>
		{/if}
	</main>
</div>

<style>
	.waiver-viewport {
		min-height: 100vh;
		background: radial-gradient(circle at top right, rgba(245, 158, 11, 0.08), transparent 40%),
			radial-gradient(circle at bottom left, rgba(6, 182, 212, 0.08), transparent 40%),
			var(--bg-primary);
		color: var(--text-primary);
		padding-bottom: 4rem;
	}

	.waiver-header {
		padding: 1rem 2rem;
		border-bottom: 1px solid var(--border-color);
		background: rgba(15, 17, 23, 0.85);
		backdrop-filter: blur(12px);
		position: sticky;
		top: 0;
		z-index: 50;
	}

	.header-content {
		max-width: 1200px;
		margin: 0 auto;
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.brand-logo {
		font-size: 1.5rem;
		font-weight: 900;
		color: #fff;
		text-decoration: none;
		letter-spacing: 0.05em;
	}

	.header-badges {
		display: flex;
		gap: 0.5rem;
	}

	.badge {
		padding: 0.35rem 0.75rem;
		border-radius: 9999px;
		font-size: 0.75rem;
		font-weight: 700;
		letter-spacing: 0.08em;
	}

	.badge-amber {
		background: rgba(245, 158, 11, 0.15);
		color: var(--accent-amber);
		border: 1px solid rgba(245, 158, 11, 0.3);
	}

	.badge-cyan {
		background: rgba(6, 182, 212, 0.15);
		color: var(--accent-cyan);
		border: 1px solid rgba(6, 182, 212, 0.3);
	}

	.waiver-container {
		max-width: 1100px;
		margin: 2rem auto;
		padding: 0 1.5rem;
	}

	.booking-banner {
		padding: 1.5rem 2rem;
		border-radius: 12px;
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 2rem;
		background: linear-gradient(135deg, rgba(30, 34, 48, 0.9), rgba(15, 17, 23, 0.9));
		border: 1px solid rgba(245, 158, 11, 0.25);
	}

	.info-label {
		font-size: 0.8rem;
		text-transform: uppercase;
		letter-spacing: 0.08em;
		color: var(--accent-amber);
		font-weight: 700;
	}

	.party-name {
		font-size: 1.6rem;
		margin: 0.25rem 0;
	}

	.party-meta {
		color: var(--text-secondary);
		font-size: 0.9rem;
		margin: 0;
	}

	.party-share-row {
		display: flex;
		align-items: center;
		gap: 1rem;
		margin-top: 0.75rem;
	}

	.share-hint {
		font-size: 0.8rem;
		color: var(--text-secondary);
	}

	.waiver-counter-box {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.5rem;
		min-width: 220px;
	}

	.waiver-counter {
		text-align: center;
		padding: 0.75rem 1.5rem;
		background: rgba(0, 0, 0, 0.4);
		border-radius: 8px;
		border: 1px solid var(--border-color);
		width: 100%;
	}

	.waiver-num {
		font-size: 2.2rem;
		font-weight: 900;
		color: var(--accent-amber);
		display: block;
		line-height: 1;
	}

	.waiver-num.complete {
		color: #10b981;
	}

	.waiver-label {
		font-size: 0.75rem;
		color: var(--text-secondary);
		text-transform: uppercase;
		letter-spacing: 0.05em;
		margin-top: 0.25rem;
		display: block;
	}

	.waiver-progress-track {
		width: 100%;
		height: 8px;
		background: rgba(255, 255, 255, 0.1);
		border-radius: 9999px;
		overflow: hidden;
	}

	.waiver-progress-fill {
		height: 100%;
		background: linear-gradient(90deg, var(--accent-amber), #10b981);
		transition: width 0.3s ease;
	}

	.waiver-status-badge {
		font-size: 0.75rem;
		font-weight: 800;
		padding: 0.25rem 0.6rem;
		border-radius: 9999px;
	}

	.badge-warning {
		background: rgba(245, 158, 11, 0.2);
		color: var(--accent-amber);
		border: 1px solid rgba(245, 158, 11, 0.4);
	}

	.badge-success {
		background: rgba(16, 185, 129, 0.2);
		color: #10b981;
		border: 1px solid rgba(16, 185, 129, 0.4);
	}

	.waiver-layout {
		display: flex;
		flex-direction: column;
		gap: 2rem;
	}

	.legal-card, .form-card {
		padding: 2rem;
		border-radius: 12px;
	}

	.section-title, .form-title {
		font-size: 1.5rem;
		margin-top: 0;
		margin-bottom: 0.25rem;
	}

	.section-version, .form-subtitle {
		color: var(--text-secondary);
		font-size: 0.85rem;
		margin-bottom: 1.25rem;
	}

	.legal-scrollbox {
		max-height: 250px;
		overflow-y: auto;
		background: rgba(0, 0, 0, 0.25);
		border: 1px solid var(--border-color);
		padding: 1.25rem;
		border-radius: 8px;
		font-size: 0.9rem;
		line-height: 1.6;
		color: #cbd5e1;
	}

	.form-row-2 {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1.25rem;
		margin-bottom: 1.25rem;
	}

	@media (max-width: 640px) {
		.form-row-2 {
			grid-template-columns: 1fr;
		}
	}

	.form-group {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		margin-bottom: 1.25rem;
	}

	.form-label {
		font-size: 0.85rem;
		font-weight: 600;
		color: var(--text-secondary);
	}

	.form-input {
		background: rgba(255, 255, 255, 0.05);
		border: 1px solid var(--border-color);
		padding: 0.85rem 1rem;
		border-radius: 8px;
		color: #fff;
		font-size: 0.95rem;
		transition: border-color 0.2s;
	}

	.form-input:focus {
		outline: none;
		border-color: var(--accent-amber);
	}

	.checkbox-group {
		margin-bottom: 1.25rem;
	}

	.checkbox-label {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		font-size: 0.9rem;
		cursor: pointer;
	}

	.signature-section {
		margin: 1.5rem 0;
	}

	.sig-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 0.5rem;
	}

	.sig-hint {
		font-size: 0.8rem;
		color: var(--accent-cyan);
	}

	.agreement-acknowledgement {
		margin: 1.5rem 0;
		padding: 1rem;
		background: rgba(245, 158, 11, 0.05);
		border: 1px solid rgba(245, 158, 11, 0.2);
		border-radius: 8px;
	}

	.btn-submit {
		width: 100%;
		padding: 1rem;
		font-size: 1.1rem;
		font-weight: 700;
	}

	.success-card {
		padding: 3rem;
		text-align: center;
		border-radius: 16px;
		max-width: 650px;
		margin: 3rem auto;
	}

	.success-icon {
		font-size: 4rem;
		margin-bottom: 1rem;
	}

	.success-title {
		font-size: 2rem;
		color: #22c55e;
		margin: 0 0 0.5rem;
	}

	.hash-box {
		background: rgba(0, 0, 0, 0.4);
		padding: 1rem;
		border-radius: 8px;
		border: 1px solid var(--border-color);
		margin: 1.5rem 0;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.hash-label {
		font-size: 0.75rem;
		color: var(--text-secondary);
		letter-spacing: 0.08em;
	}

	.hash-code {
		font-family: monospace;
		color: var(--accent-cyan);
		font-size: 0.85rem;
		word-break: break-all;
	}

	.timestamp {
		font-size: 0.75rem;
		color: var(--text-secondary);
	}

	.success-actions {
		display: flex;
		gap: 1rem;
		justify-content: center;
		margin-top: 2rem;
	}
</style>
