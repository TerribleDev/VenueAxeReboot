<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { page } from '$app/state';
	import WaiverCanvas from '$lib/components/WaiverCanvas.svelte';
	import {
		getApiWaiversTemplateByVenueSlug,
		postApiWaiversSign
	} from '$lib/api/client';
	import type { WaiverTemplateDto, WaiverDto } from '$lib/api/generated/types.gen';

	let venueSlug = $derived(page.params.venueSlug ?? 'downtown');
	let isKiosk = $derived(page.url.searchParams.get('kiosk') === 'true');

	let template = $state<WaiverTemplateDto | null>(null);
	let bookingReference = $state(page.url.searchParams.get('ref') ?? '');
	let firstName = $state('');
	let lastName = $state('');
	let email = $state('');
	let phone = $state('');
	let dob = $state('');
	let isGuardian = $state(false);
	let minorNames = $state('');
	let signaturePng = $state('');
	let isSubmitting = $state(false);
	let signedWaiver = $state<WaiverDto | null>(null);

	let submitError = $state<string | null>(null);
	let termsAccepted = $state(false);

	// Kiosk Auto-Reset
	let countdown = $state(10);
	let countdownInterval = $state<any>(null);
	let canvasKey = $state(0);

	onMount(async () => {
		try {
			const res = await getApiWaiversTemplateByVenueSlug({
				path: { venueSlug }
			});
			if (res.data) {
				template = res.data;
			}
		} catch (e) {
			console.error(e);
		}
	});

	onDestroy(() => {
		if (countdownInterval) {
			clearInterval(countdownInterval);
			countdownInterval = null;
		}
	});

	function handleSignatureChange(png: string) {
		signaturePng = png;
		submitError = null;
	}

	function resetForNextSigner() {
		if (countdownInterval) {
			clearInterval(countdownInterval);
			countdownInterval = null;
		}
		signedWaiver = null;
		firstName = '';
		lastName = '';
		email = '';
		phone = '';
		dob = '';
		isGuardian = false;
		minorNames = '';
		signaturePng = '';
		bookingReference = '';
		submitError = null;
		termsAccepted = false;
		countdown = 10;
		canvasKey++;
	}

	function startKioskAutoReset() {
		countdown = 10;
		if (countdownInterval) clearInterval(countdownInterval);
		countdownInterval = setInterval(() => {
			if (countdown > 1) {
				countdown--;
			} else {
				resetForNextSigner();
			}
		}, 1000);
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
		if (isGuardian && !minorNames.trim()) return 'Please enter the minor participant names covered by your signature.';
		if (!signaturePng) return 'Please sign using your finger or mouse in the signature box below.';
		if (!termsAccepted) return 'You must check the box agreeing to the liability release terms.';
		return null;
	}

	async function handleSubmitWaiver(e: SubmitEvent) {
		e.preventDefault();
		if (!template) return;
		
		const validationError = validateIntake();
		if (validationError) {
			submitError = validationError;
			return;
		}

		isSubmitting = true;
		submitError = null;
		try {
			const cleanedMinors = isGuardian && minorNames.trim()
				? JSON.stringify(
						minorNames
							.split(',')
							.map((n) => ({ name: n.trim() }))
							.filter((m) => m.name.length > 0)
				  )
				: null;

			const res = await postApiWaiversSign({
				body: {
					templateId: template.id,
					bookingId: null,
					bookingReference: bookingReference.trim() || null,
					signerFirstName: firstName.trim(),
					signerLastName: lastName.trim(),
					signerEmail: email.trim(),
					signerPhone: phone.trim() || '',
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
				if (isKiosk) {
					startKioskAutoReset();
				}
			} else {
				const err = res.error as any;
				if (err?.errors) {
					const messages = Object.values(err.errors).flat().join(' ');
					submitError = messages || err.title || 'Validation error submitting waiver.';
				} else {
					submitError = err?.detail ?? err?.title ?? err?.message ?? 'Failed to submit waiver. Please check your information.';
				}
			}
		} catch (e: any) {
			submitError = e?.message ?? 'Failed to submit waiver. Please check your information.';
		} finally {
			isSubmitting = false;
		}
	}
</script>

{#if isKiosk}
	<div class="kiosk-top-bar">
		<div class="kiosk-indicator">
			<span class="kiosk-pulse-beacon"></span>
			<span class="kiosk-badge font-display">🔒 RECEPTION KIOSK MODE</span>
		</div>
		<span class="kiosk-venue font-display">{venueSlug.toUpperCase()} ARENA CHECK-IN</span>
		<button class="kiosk-quick-reset" onclick={resetForNextSigner} title="Reset Form">
			🔄 Clear Screen
		</button>
	</div>
{/if}

<div class="waiver-page-container" class:kiosk-mode-container={isKiosk}>
	{#if signedWaiver}
		<!-- SUCCESS CONFIRMATION -->
		<div class="waiver-success glass-panel">
			<span class="success-icon">✅</span>
			<h1 class="success-title font-display">WAIVER VERIFIED & RECORDED</h1>
			<p class="success-sub font-display">
				Thank you, <span class="text-amber">{signedWaiver.signerFirstName} {signedWaiver.signerLastName}</span>!
			</p>
			<p class="success-text">
				Your digital release has been cryptographically timestamped and linked to your reservation. You are cleared to throw!
			</p>

			{#if isKiosk}
				<div class="kiosk-countdown-box">
					<div class="kiosk-countdown-ring font-display">{countdown}</div>
					<div class="kiosk-countdown-info">
						<span class="kiosk-countdown-title">Auto-resetting for the next thrower in {countdown}s</span>
						<div class="kiosk-progress-track">
							<div class="kiosk-progress-bar" style="width: {countdown * 10}%;"></div>
						</div>
					</div>
				</div>
			{/if}

			<div class="sig-review-box">
				<span class="sig-label">Recorded Digital Signature:</span>
				<img src={signedWaiver.signatureImagePngBase64} alt="Signed" class="sig-img" />
			</div>

			<button class="btn btn-primary btn-kiosk-next font-display" onclick={resetForNextSigner}>
				⚡ Sign Next Guest Waiver Now
			</button>
		</div>
	{:else if template}
		<div class="waiver-card glass-panel">
			<div class="waiver-header">
				<span class="axe-badge">🪓</span>
				<h1 class="waiver-title font-display">Participant Safety Waiver</h1>
				<p class="waiver-version font-display">Agreement Version {template.versionNumber}</p>
			</div>

			<!-- Legal Disclaimer Markdown/Text Box -->
			<div class="legal-scrollbox">
				<pre class="legal-text">{template.bodyTextMarkdown}</pre>
			</div>

			<form onsubmit={handleSubmitWaiver} class="waiver-form">
				<!-- Booking Reference (Optional) -->
				<div class="form-group booking-ref-group">
					<label class="form-label" for="booking-ref">
						Booking Reference <span class="label-hint">(Optional — links to your group reservation)</span>
					</label>
					<input
						id="booking-ref"
						type="text"
						class="form-input text-uppercase"
						bind:value={bookingReference}
						placeholder="e.g. VA-84920"
					/>
				</div>

				<div class="form-grid">
					<div class="form-group">
						<label class="form-label" for="first-name">Legal First Name</label>
						<input id="first-name" type="text" class="form-input" bind:value={firstName} required placeholder="John" />
					</div>
					<div class="form-group">
						<label class="form-label" for="last-name">Legal Last Name</label>
						<input id="last-name" type="text" class="form-input" bind:value={lastName} required placeholder="Doe" />
					</div>
				</div>

				<div class="form-grid">
					<div class="form-group">
						<label class="form-label" for="email">Email Address</label>
						<input id="email" type="email" class="form-input" bind:value={email} required placeholder="john@example.com" />
					</div>
					<div class="form-group">
						<label class="form-label" for="phone">Phone Number</label>
						<input id="phone" type="tel" class="form-input" bind:value={phone} required placeholder="(555) 000-0000" />
					</div>
				</div>

				<div class="form-group">
					<label class="form-label" for="dob">Date of Birth</label>
					<input id="dob" type="date" class="form-input" bind:value={dob} required />
				</div>

				<!-- Minor / Guardian Policy -->
				<div class="guardian-box">
					<label class="guardian-checkbox">
						<input type="checkbox" bind:checked={isGuardian} />
						<span>I am signing as a parent or legal guardian for minor participant(s)</span>
					</label>

					{#if isGuardian}
						<div class="form-group" style="margin-top: 0.75rem;">
							<label class="form-label" for="minor-names">Minor Full Names (comma-separated)</label>
							<input id="minor-names" type="text" class="form-input" bind:value={minorNames} placeholder="Timmy Doe, Emma Doe" />
						</div>
					{/if}
				</div>

				<!-- Signature Canvas -->
				<div class="signature-section">
					{#key canvasKey}
						<WaiverCanvas onchange={handleSignatureChange} />
					{/key}
				</div>

				<div class="terms-ack-box">
					<label class="terms-ack-label" for="kiosk-ack">
						<input id="kiosk-ack" type="checkbox" bind:checked={termsAccepted} />
						<span>I acknowledge that I have read, understood, and agree to the terms of the safety release and liability waiver.</span>
					</label>
				</div>

				{#if submitError}
					<div class="alert-error-box font-display">
						⚠️ {submitError}
					</div>
				{/if}

				<button type="submit" class="btn btn-primary btn-block" disabled={isSubmitting}>
					{isSubmitting ? 'Recording Signature...' : '✍️ Submit Legal Waiver'}
				</button>
			</form>
		</div>
	{:else}
		<div class="loading-state">
			<p>Loading legal safety waiver...</p>
		</div>
	{/if}
</div>

<style>
	/* Kiosk Header */
	.kiosk-top-bar {
		position: sticky;
		top: 0;
		z-index: 100;
		background: rgba(10, 14, 20, 0.95);
		backdrop-filter: blur(12px);
		border-bottom: 2px solid var(--accent-amber);
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0.75rem 2rem;
		box-shadow: 0 4px 20px rgba(0, 0, 0, 0.5);
	}

	.kiosk-indicator {
		display: flex;
		align-items: center;
		gap: 0.6rem;
	}

	.kiosk-pulse-beacon {
		width: 10px;
		height: 10px;
		border-radius: 50%;
		background: #10b981;
		box-shadow: 0 0 12px #10b981;
		animation: pulse-ring 1.8s infinite;
	}

	@keyframes pulse-ring {
		0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
		70% { transform: scale(1); box-shadow: 0 0 0 8px rgba(16, 185, 129, 0); }
		100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
	}

	.kiosk-badge {
		font-size: 0.85rem;
		font-weight: 800;
		letter-spacing: 0.08em;
		color: #e2e8f0;
	}

	.kiosk-venue {
		font-size: 0.8rem;
		font-weight: 700;
		color: var(--accent-amber);
		letter-spacing: 0.1em;
	}

	.kiosk-quick-reset {
		background: rgba(255, 255, 255, 0.05);
		border: 1px solid var(--border-color);
		color: #94a3b8;
		padding: 0.35rem 0.75rem;
		border-radius: var(--radius-sm);
		font-size: 0.75rem;
		font-weight: 600;
		cursor: pointer;
		transition: all 0.2s;
	}

	.kiosk-quick-reset:hover {
		background: rgba(239, 68, 68, 0.15);
		color: #ef4444;
		border-color: rgba(239, 68, 68, 0.3);
	}

	.waiver-page-container {
		max-width: 800px;
		margin: 0 auto;
		padding: 2rem 1.5rem 5rem;
	}

	.kiosk-mode-container {
		padding-top: 1.5rem;
	}

	.waiver-card {
		padding: 2.5rem;
	}

	.waiver-header {
		text-align: center;
		margin-bottom: 2rem;
	}

	.axe-badge { font-size: 2.5rem; display: block; margin-bottom: 0.5rem; }
	.waiver-title { font-size: 2rem; font-weight: 800; }
	.waiver-version { color: var(--accent-amber); font-size: 0.85rem; letter-spacing: 0.05em; }

	.legal-scrollbox {
		background: #0b0e14;
		border: 1px solid var(--border-color);
		padding: 1.5rem;
		border-radius: var(--radius-md);
		max-height: 240px;
		overflow-y: auto;
		margin-bottom: 1.5rem;
	}

	.legal-text {
		color: var(--text-secondary);
		font-family: var(--font-body);
		font-size: 0.9rem;
		white-space: pre-wrap;
		line-height: 1.6;
	}

	.waiver-form {
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
	}

	.booking-ref-group {
		background: rgba(245, 158, 11, 0.04);
		border: 1px dashed rgba(245, 158, 11, 0.3);
		padding: 1rem;
		border-radius: var(--radius-md);
	}

	.label-hint {
		font-size: 0.75rem;
		color: var(--text-muted);
		font-weight: normal;
	}

	.text-uppercase {
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.form-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1rem;
	}

	.guardian-box {
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		padding: 1rem;
		border-radius: var(--radius-md);
	}

	.guardian-checkbox {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		cursor: pointer;
		font-size: 0.9rem;
	}

	.guardian-checkbox input {
		width: 18px;
		height: 18px;
		accent-color: var(--accent-amber);
	}

	.signature-section {
		margin: 1rem 0;
	}

	.btn-block {
		width: 100%;
		padding: 1rem;
		font-size: 1.1rem;
	}

	/* Success State */
	.waiver-success {
		padding: 3.5rem 2rem;
		text-align: center;
		display: flex;
		flex-direction: column;
		align-items: center;
	}

	.success-icon { font-size: 4rem; margin-bottom: 1rem; }
	.success-title { font-size: 2rem; font-weight: 900; }
	.success-sub { font-size: 1.25rem; margin: 0.5rem 0 1rem; }
	.success-text { color: var(--text-secondary); max-width: 500px; line-height: 1.5; margin-bottom: 1.5rem; }

	.kiosk-countdown-box {
		background: rgba(16, 185, 129, 0.08);
		border: 1px solid rgba(16, 185, 129, 0.3);
		border-radius: var(--radius-lg);
		padding: 1.25rem 2rem;
		display: flex;
		align-items: center;
		gap: 1.5rem;
		margin-bottom: 1.5rem;
		width: 100%;
		max-width: 480px;
	}

	.kiosk-countdown-ring {
		width: 52px;
		height: 52px;
		border-radius: 50%;
		background: #10b981;
		color: #0b0e14;
		font-size: 1.75rem;
		font-weight: 900;
		display: flex;
		align-items: center;
		justify-content: center;
		flex-shrink: 0;
		box-shadow: 0 0 16px rgba(16, 185, 129, 0.4);
	}

	.kiosk-countdown-info {
		flex: 1;
		text-align: left;
	}

	.kiosk-countdown-title {
		font-size: 0.9rem;
		font-weight: 600;
		color: #e2e8f0;
		display: block;
		margin-bottom: 0.5rem;
	}

	.kiosk-progress-track {
		height: 6px;
		background: rgba(255, 255, 255, 0.1);
		border-radius: 3px;
		overflow: hidden;
	}

	.kiosk-progress-bar {
		height: 100%;
		background: #10b981;
		border-radius: 3px;
		transition: width 1s linear;
	}

	.sig-review-box {
		background: #0f141c;
		border: 1px solid var(--border-color);
		padding: 1rem 2rem;
		border-radius: var(--radius-md);
		margin-bottom: 1.5rem;
	}

	.sig-label { font-size: 0.75rem; color: var(--text-muted); display: block; margin-bottom: 0.5rem; text-transform: uppercase; }
	.sig-img { height: 60px; }

	.btn-kiosk-next {
		font-size: 1.15rem;
		padding: 1rem 2.5rem;
		box-shadow: 0 4px 20px rgba(245, 158, 11, 0.3);
		letter-spacing: 0.05em;
	}

	.terms-ack-box {
		margin: 1.25rem 0 0.5rem;
		padding: 0.75rem 1rem;
		background: rgba(255, 255, 255, 0.03);
		border: 1px solid var(--border-color);
		border-radius: var(--radius-sm);
	}

	.terms-ack-label {
		display: flex;
		align-items: flex-start;
		gap: 0.75rem;
		cursor: pointer;
		font-size: 0.85rem;
		color: var(--text-secondary);
		line-height: 1.4;
	}

	.terms-ack-label input {
		margin-top: 0.2rem;
	}

	.alert-error-box {
		background: rgba(239, 68, 68, 0.2);
		border: 1px solid var(--accent-crimson, #ef4444);
		color: #fca5a5;
		padding: 0.75rem 1rem;
		border-radius: var(--radius-sm);
		font-size: 0.9rem;
		font-weight: 700;
		margin: 1rem 0;
	}

	@media (max-width: 600px) {
		.form-grid {
			grid-template-columns: 1fr;
		}
		.kiosk-top-bar {
			padding: 0.6rem 1rem;
		}
		.kiosk-venue {
			display: none;
		}
	}
</style>
