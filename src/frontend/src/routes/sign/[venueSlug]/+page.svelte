<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import WaiverCanvas from '$lib/components/WaiverCanvas.svelte';
	import {
		getApiWaiversTemplateByVenueSlug,
		postApiWaiversSign
	} from '$lib/api/client';
	import type { WaiverTemplateDto, WaiverDto } from '$lib/api/generated/types.gen';

	let venueSlug = $derived(page.params.venueSlug ?? 'downtown');

	let template = $state<WaiverTemplateDto | null>(null);
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

	function handleSignatureChange(png: string) {
		signaturePng = png;
	}

	async function handleSubmitWaiver(e: SubmitEvent) {
		e.preventDefault();
		if (!template) return;
		if (!signaturePng) {
			alert('Please sign the waiver using your finger or stylus.');
			return;
		}

		isSubmitting = true;
		try {
			const res = await postApiWaiversSign({
				body: {
					templateId: template.id,
					bookingId: undefined,
					signerFirstName: firstName,
					signerLastName: lastName,
					signerEmail: email,
					signerPhone: phone,
					dateOfBirth: dob as any,
					isGuardianSigning: isGuardian,
					minorsCoveredJson: isGuardian ? JSON.stringify(minorNames.split(',').map((n) => ({ name: n.trim() }))) : null,
					signatureImagePngBase64: signaturePng,
					signatureVectorSvg: null,
					userAgent: navigator.userAgent
				}
			});
			if (res.data) {
				signedWaiver = res.data;
			}
		} catch (e) {
			alert('Failed to submit waiver. Please check your information.');
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="waiver-page-container">
	{#if signedWaiver}
		<!-- SUCCESS CONFIRMATION -->
		<div class="waiver-success glass-panel">
			<span class="success-icon">✅</span>
			<h1 class="success-title font-display">WAIVER VERIFIED & RECORDED</h1>
			<p class="success-sub font-display">
				Thank you, <span class="text-amber">{signedWaiver.signerFirstName} {signedWaiver.signerLastName}</span>!
			</p>
			<p class="success-text">
				Your digital release has been cryptographically timestamped and linked to your session. You are cleared to throw!
			</p>
			<div class="sig-review-box">
				<span class="sig-label">Recorded Digital Signature:</span>
				<img src={signedWaiver.signatureImagePngBase64} alt="Signed" class="sig-img" />
			</div>
			<button class="btn btn-secondary" onclick={() => { signedWaiver = null; firstName = ''; lastName = ''; email = ''; phone = ''; signaturePng = ''; }}>
				+ Sign Another Guest Waiver
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
					<WaiverCanvas onchange={handleSignatureChange} />
				</div>

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
	.waiver-page-container {
		max-width: 800px;
		margin: 0 auto;
		padding: 2rem 1.5rem 5rem;
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
		margin-bottom: 2rem;
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
	.success-text { color: var(--text-secondary); max-width: 500px; line-height: 1.5; margin-bottom: 2rem; }

	.sig-review-box {
		background: #0f141c;
		border: 1px solid var(--border-color);
		padding: 1rem 2rem;
		border-radius: var(--radius-md);
		margin-bottom: 2rem;
	}

	.sig-label { font-size: 0.75rem; color: var(--text-muted); display: block; margin-bottom: 0.5rem; text-transform: uppercase; }
	.sig-img { height: 60px; filter: invert(1); }

	@media (max-width: 600px) {
		.form-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
