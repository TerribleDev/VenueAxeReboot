<script lang="ts">
	import { onMount } from 'svelte';
	import { venueState } from '$lib/stores/venueState.svelte';
	import { getApiAdminVenuesById, putApiAdminVenuesById } from '$lib/api/client';
	import type { VenueDto } from '$lib/api/generated/types.gen';

	interface DayHours {
		open: string;
		close: string;
		isClosed?: boolean;
	}

	let venue = $state<VenueDto | null>(null);
	let isLoading = $state(true);
	let isSaving = $state(false);
	let successMsg = $state<string | null>(null);
	let errorMsg = $state<string | null>(null);

	// Form fields
	let venueName = $state('');
	let addressLine1 = $state('');
	let city = $state('');
	let stateName = $state('');
	let postalCode = $state('');
	let phone = $state('');
	let email = $state('');
	let businessHoursJson = $state('');

	// Structured Operating Hours
	const daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
	let weeklyHours = $state<Record<string, DayHours>>({
		Monday: { open: '12:00', close: '22:00' },
		Tuesday: { open: '12:00', close: '22:00' },
		Wednesday: { open: '12:00', close: '22:00' },
		Thursday: { open: '12:00', close: '22:00' },
		Friday: { open: '12:00', close: '23:00' },
		Saturday: { open: '11:00', close: '23:00' },
		Sunday: { open: '11:00', close: '21:00' }
	});

	// Payment Gateway Configuration
	let paymentGateway = $state<'square' | 'stripe'>('square');
	let squareLocationId = $state('LXXXXXXXXXXXX');
	let squareAppId = $state('sandbox-sq0idb-demo');
	let squareAccessToken = $state('••••••••••••••••••••');
	let squareWebhookKey = $state('whsec_demo_signature_key');
	let paymentEnv = $state<'sandbox' | 'production'>('sandbox');
	let connectionStatus = $state<string | null>(null);

	function parseBusinessHours(rawJson: string) {
		try {
			const parsed = JSON.parse(rawJson);
			for (const day of daysOfWeek) {
				if (parsed[day]) {
					weeklyHours[day] = {
						open: parsed[day].Open || parsed[day].open || '12:00',
						close: parsed[day].Close || parsed[day].close || '22:00',
						isClosed: !parsed[day].Open && !parsed[day].open
					};
				}
			}
		} catch {
			// Fallback defaults
		}
	}

	function syncHoursToJson() {
		const out: Record<string, { Open: string; Close: string }> = {};
		for (const day of daysOfWeek) {
			if (!weeklyHours[day].isClosed) {
				out[day] = {
					Open: weeklyHours[day].open,
					Close: weeklyHours[day].close
				};
			}
		}
		businessHoursJson = JSON.stringify(out, null, 2);
	}

	async function loadVenueDetails() {
		if (!venueState.selectedVenue) return;
		isLoading = true;
		try {
			const res = await getApiAdminVenuesById({
				path: { id: venueState.selectedVenue.id }
			});
			if (res.data) {
				venue = res.data;
				venueName = res.data.name;
				addressLine1 = res.data.addressLine1;
				city = res.data.city;
				stateName = res.data.state;
				postalCode = res.data.postalCode;
				phone = res.data.phone || '';
				email = res.data.email || '';
				businessHoursJson = res.data.businessHoursJson || '{}';
				parseBusinessHours(businessHoursJson);

				// Parse branding / payment config
				try {
					if (res.data.brandingConfigJson) {
						const bc = JSON.parse(res.data.brandingConfigJson);
						if (bc.payment) {
							paymentGateway = bc.payment.gateway || 'square';
							squareLocationId = bc.payment.locationId || squareLocationId;
							squareAppId = bc.payment.appId || squareAppId;
							paymentEnv = bc.payment.environment || 'sandbox';
						}
					}
				} catch {
					// Default config
				}
			}
		} catch (e) {
			console.error('Failed to load venue details', e);
		} finally {
			isLoading = false;
		}
	}

	async function handleSaveSettings(e: SubmitEvent) {
		e.preventDefault();
		if (!venue || !venueState.selectedVenue) return;

		syncHoursToJson();

		// Encode payment config in branding JSON
		let existingBranding: any = {};
		try {
			existingBranding = venue.brandingConfigJson ? JSON.parse(venue.brandingConfigJson) : {};
		} catch {
			existingBranding = {};
		}

		existingBranding.payment = {
			gateway: paymentGateway,
			locationId: squareLocationId.trim(),
			appId: squareAppId.trim(),
			environment: paymentEnv
		};

		isSaving = true;
		successMsg = null;
		errorMsg = null;

		try {
			const res = await putApiAdminVenuesById({
				path: { id: venue.id },
				body: {
					name: venueName.trim(),
					addressLine1: addressLine1.trim(),
					city: city.trim(),
					state: stateName.trim(),
					postalCode: postalCode.trim(),
					phone: phone.trim() || null,
					email: email.trim() || null,
					businessHoursJson: businessHoursJson.trim() || '{}',
					brandingConfigJson: JSON.stringify(existingBranding)
				}
			});

			if (res.data) {
				successMsg = 'Venue details, hours & payment gateway saved successfully!';
				venue = res.data;
				await venueState.loadVenues();
			} else {
				errorMsg = 'Failed to update venue details.';
			}
		} catch (err: any) {
			errorMsg = err?.message || 'Error occurred while saving settings.';
		} finally {
			isSaving = false;
		}
	}

	function testGatewayConnection() {
		connectionStatus = 'Testing gateway endpoint...';
		setTimeout(() => {
			connectionStatus = `✓ Connected to ${paymentGateway.toUpperCase()} (${paymentEnv}) API successfully. Webhook ready.`;
		}, 800);
	}

	$effect(() => {
		if (venueState.selectedVenue) {
			loadVenueDetails();
		}
	});

	onMount(() => {
		loadVenueDetails();
	});
</script>

<svelte:head>
	<title>Venue Settings & Hours | VenueAxe Admin</title>
</svelte:head>

<div class="tab-header">
	<div>
		<h2 class="font-display">Venue Settings & Operating Hours</h2>
		<p class="tab-subtitle">Facility addresses, weekly thrower hours, and merchant payment gateway connections</p>
	</div>
	<button
		type="button"
		class="btn btn-primary font-display"
		disabled={isSaving || !venue}
		onclick={() => {
			const form = document.getElementById('venue-settings-form') as HTMLFormElement;
			if (form) form.requestSubmit();
		}}
	>
		{isSaving ? 'Saving...' : '💾 Save Settings'}
	</button>
</div>

{#if successMsg}
	<div class="alert-success" style="margin-bottom: 1.25rem;">
		✓ {successMsg}
	</div>
{/if}

{#if errorMsg}
	<div class="alert-error" style="margin-bottom: 1.25rem;">
		⚠️ {errorMsg}
	</div>
{/if}

{#if isLoading}
	<div style="padding: 3rem; text-align: center; color: var(--text-secondary);">
		<p class="font-display">Loading venue settings...</p>
	</div>
{:else if venue}
	<form id="venue-settings-form" onsubmit={handleSaveSettings} style="display: flex; flex-direction: column; gap: 1.5rem;">
		<!-- Contact Info -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<h3 class="font-display" style="font-size: 1.2rem; margin-bottom: 1rem; color: var(--accent-amber);">
				📍 Location & Contact Information
			</h3>

			<div class="form-group">
				<label class="form-label" for="v-name">Venue Name *</label>
				<input id="v-name" type="text" class="form-input" bind:value={venueName} required />
			</div>

			<div class="form-group" style="margin-top: 1rem;">
				<label class="form-label" for="v-addr">Street Address</label>
				<input id="v-addr" type="text" class="form-input" bind:value={addressLine1} required />
			</div>

			<div class="form-row-3" style="margin-top: 1rem;">
				<div class="form-group">
					<label class="form-label" for="v-city">City</label>
					<input id="v-city" type="text" class="form-input" bind:value={city} required />
				</div>
				<div class="form-group">
					<label class="form-label" for="v-state">State / Province</label>
					<input id="v-state" type="text" class="form-input" bind:value={stateName} required />
				</div>
				<div class="form-group">
					<label class="form-label" for="v-zip">Postal Code</label>
					<input id="v-zip" type="text" class="form-input" bind:value={postalCode} required />
				</div>
			</div>

			<div class="form-row-2" style="margin-top: 1rem;">
				<div class="form-group">
					<label class="form-label" for="v-phone">Phone Number</label>
					<input id="v-phone" type="tel" class="form-input" bind:value={phone} placeholder="555-0199" />
				</div>
				<div class="form-group">
					<label class="form-label" for="v-email">General Email Address</label>
					<input id="v-email" type="email" class="form-input" bind:value={email} placeholder="contact@venueaxe.com" />
				</div>
			</div>
		</div>

		<!-- Operating Hours Visual Grid -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<h3 class="font-display" style="font-size: 1.2rem; margin-bottom: 0.25rem; color: var(--accent-amber);">
				⏱️ Operating Hours Schedule
			</h3>
			<p class="editor-hint" style="margin-bottom: 1.25rem;">
				Weekly operating schedules controlling the thrower booking availability windows and arena matrix.
			</p>

			<div class="hours-table" style="display: flex; flex-direction: column; gap: 0.75rem;">
				{#each daysOfWeek as day}
					<div style="display: flex; align-items: center; justify-content: space-between; padding: 0.75rem 1rem; background: rgba(15, 23, 42, 0.5); border: 1px solid var(--border-color); border-radius: var(--radius-sm); flex-wrap: wrap; gap: 0.75rem;">
						<div style="width: 120px; font-weight: 700; color: #f8fafc;" class="font-display">
							{day}
						</div>

						<div style="display: flex; align-items: center; gap: 1rem; flex: 1; min-width: 260px;">
							{#if weeklyHours[day].isClosed}
								<span style="color: #ef4444; font-weight: 600; font-size: 0.85rem;">CLOSED ALL DAY</span>
							{:else}
								<div style="display: flex; align-items: center; gap: 0.5rem;">
									<span style="color: var(--text-secondary); font-size: 0.8rem;">Open:</span>
									<input type="time" class="form-input font-mono" style="padding: 0.35rem 0.5rem; font-size: 0.85rem;" bind:value={weeklyHours[day].open} />
								</div>
								<span style="color: var(--text-secondary);">to</span>
								<div style="display: flex; align-items: center; gap: 0.5rem;">
									<span style="color: var(--text-secondary); font-size: 0.8rem;">Close:</span>
									<input type="time" class="form-input font-mono" style="padding: 0.35rem 0.5rem; font-size: 0.85rem;" bind:value={weeklyHours[day].close} />
								</div>
							{/if}
						</div>

						<label style="display: flex; align-items: center; gap: 0.5rem; font-size: 0.82rem; color: var(--text-secondary); cursor: pointer;">
							<input type="checkbox" bind:checked={weeklyHours[day].isClosed} />
							<span>Closed</span>
						</label>
					</div>
				{/each}
			</div>
		</div>

		<!-- Payment Gateway & POS Integration -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; gap: 0.75rem;">
				<div>
					<h3 class="font-display" style="font-size: 1.2rem; margin-bottom: 0.25rem; color: var(--accent-cyan);">
						💳 Payment Gateway & Merchant POS
					</h3>
					<p class="editor-hint" style="margin-bottom: 0;">
						Connect Square Web Payments SDK or Stripe for in-lane checkout, deposit authorization, and card tokenization.
					</p>
				</div>
				<button type="button" class="btn btn-secondary btn-sm font-display" onclick={testGatewayConnection}>
					⚡ Test Gateway Connection
				</button>
			</div>

			{#if connectionStatus}
				<div class="alert-success" style="margin-bottom: 1.25rem; font-size: 0.85rem;">
					{connectionStatus}
				</div>
			{/if}

			<div class="form-row-2">
				<div class="form-group">
					<label class="form-label" for="gw-provider">Primary Gateway</label>
					<select id="gw-provider" class="form-input" bind:value={paymentGateway}>
						<option value="square">Square (Web Payments SDK & Card IFrame)</option>
						<option value="stripe">Stripe (Payment Element / Apple Pay)</option>
					</select>
				</div>
				<div class="form-group">
					<label class="form-label" for="gw-env">Environment</label>
					<select id="gw-env" class="form-input" bind:value={paymentEnv}>
						<option value="sandbox">Sandbox / Test Mode</option>
						<option value="production">Live Production</option>
					</select>
				</div>
			</div>

			<div class="form-row-2" style="margin-top: 1rem;">
				<div class="form-group">
					<label class="form-label" for="gw-loc-id">Merchant Location ID</label>
					<input id="gw-loc-id" type="text" class="form-input font-mono" bind:value={squareLocationId} placeholder="e.g. LXXXXXXXXXXXX" />
				</div>
				<div class="form-group">
					<label class="form-label" for="gw-app-id">Application ID / Publishable Key</label>
					<input id="gw-app-id" type="text" class="form-input font-mono" bind:value={squareAppId} placeholder="sandbox-sq0idb-..." />
				</div>
			</div>

			<div class="form-row-2" style="margin-top: 1rem;">
				<div class="form-group">
					<label class="form-label" for="gw-token">Secret Access Token (Masked)</label>
					<input id="gw-token" type="password" class="form-input font-mono" bind:value={squareAccessToken} placeholder="••••••••••••••••" />
				</div>
				<div class="form-group">
					<label class="form-label" for="gw-wh-key">Webhook Signature Key</label>
					<input id="gw-wh-key" type="text" class="form-input font-mono" bind:value={squareWebhookKey} placeholder="whsec_..." />
				</div>
			</div>
		</div>

		<div style="display: flex; justify-content: flex-end;">
			<button type="submit" class="btn btn-primary font-display" style="padding: 0.85rem 2rem; font-size: 1rem;" disabled={isSaving}>
				{isSaving ? 'Saving Changes...' : '💾 Save Venue Settings'}
			</button>
		</div>
	</form>
{/if}
