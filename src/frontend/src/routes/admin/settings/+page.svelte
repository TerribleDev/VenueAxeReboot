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
	let squareLocationId = $state('');
	let squareAppId = $state('');
	let squareAccessToken = $state('');
	let squareWebhookKey = $state('');
	let paymentEnv = $state<'sandbox' | 'production'>('sandbox');
	let isTestingConnection = $state(false);
	let connectionStatus = $state<{ success: boolean; message: string } | null>(null);

	interface ClosedDateItem {
		date: string;
		reason: string;
	}

	let closedDates = $state<ClosedDateItem[]>([]);
	let newClosedDate = $state('');
	let newClosedReason = $state('');

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
			if (Array.isArray(parsed.closedDates)) {
				closedDates = parsed.closedDates
					.map((item: any) =>
						typeof item === 'string'
							? { date: item, reason: 'Holiday / Closed' }
							: { date: item.date || '', reason: item.reason || 'Holiday / Closed' }
					)
					.filter((item: ClosedDateItem) => !!item.date);
			} else {
				closedDates = [];
			}
		} catch {
			closedDates = [];
		}
	}

	function syncHoursToJson() {
		const out: any = {};
		for (const day of daysOfWeek) {
			if (!weeklyHours[day].isClosed) {
				out[day] = {
					Open: weeklyHours[day].open,
					Close: weeklyHours[day].close
				};
			}
		}
		if (closedDates.length > 0) {
			out.closedDates = closedDates;
		}
		businessHoursJson = JSON.stringify(out, null, 2);
	}

	function addClosedDate() {
		if (!newClosedDate) return;
		if (closedDates.some((cd) => cd.date === newClosedDate)) return;
		closedDates = [
			...closedDates,
			{
				date: newClosedDate,
				reason: newClosedReason.trim() || 'Holiday / Venue Closed'
			}
		].sort((a, b) => a.date.localeCompare(b.date));
		newClosedDate = '';
		newClosedReason = '';
	}

	function removeClosedDate(dateStr: string) {
		closedDates = closedDates.filter((cd) => cd.date !== dateStr);
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
					const vData = res.data as any;
					if (vData.squareConfig) {
						paymentGateway = 'square';
						squareLocationId = vData.squareConfig.locationId || '';
						squareAppId = vData.squareConfig.applicationId || '';
						paymentEnv = (vData.squareConfig.environment?.toLowerCase() as 'sandbox' | 'production') || 'sandbox';
						if (vData.squareConfig.hasAccessToken) {
							squareAccessToken = vData.squareConfig.maskedAccessToken || '••••••••••••••••';
						} else {
							squareAccessToken = '';
						}
						if (vData.squareConfig.webhookSignatureKey) {
							squareWebhookKey = vData.squareConfig.webhookSignatureKey;
						} else {
							squareWebhookKey = '';
						}
					} else if (res.data.brandingConfigJson) {
						const bc = JSON.parse(res.data.brandingConfigJson);
						if (bc.payment) {
							paymentGateway = bc.payment.gateway || 'square';
							squareLocationId = bc.payment.locationId || '';
							squareAppId = bc.payment.appId || bc.payment.applicationId || '';
							paymentEnv = bc.payment.environment || 'sandbox';
							if (bc.payment.accessToken) {
								squareAccessToken = bc.payment.accessToken.startsWith('••') ? bc.payment.accessToken : '••••••••••••••••';
							}
							if (bc.payment.webhookKey || bc.payment.webhookSignatureKey) {
								squareWebhookKey = bc.payment.webhookKey || bc.payment.webhookSignatureKey;
							}
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
			accessToken: squareAccessToken.trim(),
			webhookKey: squareWebhookKey.trim(),
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
				successMsg = 'Venue details, hours & Square payment credentials saved successfully!';
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

	async function testGatewayConnection() {
		if (!venue) return;
		isTestingConnection = true;
		connectionStatus = null;
		try {
			const res = await fetch(`/api/admin/venues/${venue.id}/test-square-connection`, {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json'
				},
				body: JSON.stringify({
					applicationId: squareAppId.trim(),
					locationId: squareLocationId.trim(),
					accessToken: squareAccessToken.trim(),
					environment: paymentEnv
				})
			});
			const data = await res.json();
			if (res.ok && data.success) {
				connectionStatus = {
					success: true,
					message: data.message || `✓ Connected to Square (${paymentEnv}) successfully.`
				};
			} else {
				connectionStatus = {
					success: false,
					message: data.message || 'Failed to authenticate with Square API. Please check credentials.'
				};
			}
		} catch (err: any) {
			connectionStatus = {
				success: false,
				message: `Network error: ${err?.message || 'Unable to reach backend gateway.'}`
			};
		} finally {
			isTestingConnection = false;
		}
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

		<!-- Special Holiday & Closed Days Section -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.5rem; flex-wrap: wrap; gap: 0.5rem;">
				<div>
					<h3 class="font-display" style="font-size: 1.2rem; margin-bottom: 0.25rem; color: #ef4444;">
						🗓️ Holiday & Special Closed Days
					</h3>
					<p class="editor-hint" style="margin-bottom: 0;">
						Specify dates when the venue is completely closed (e.g. Christmas Day, Thanksgiving, Labor Day, or team events). Public throwers cannot book slots on these dates.
					</p>
				</div>
			</div>

			<!-- Add Closed Date Form -->
			<div style="display: flex; gap: 0.75rem; margin-top: 1.25rem; margin-bottom: 1.5rem; flex-wrap: wrap; align-items: flex-end; background: rgba(15, 23, 42, 0.4); padding: 1rem; border-radius: var(--radius-sm); border: 1px dashed var(--border-color);">
				<div style="flex: 1; min-width: 180px;">
					<label class="form-label" for="closed-date" style="font-size: 0.8rem;">Select Date *</label>
					<input id="closed-date" type="date" class="form-input" bind:value={newClosedDate} />
				</div>
				<div style="flex: 2; min-width: 220px;">
					<label class="form-label" for="closed-reason" style="font-size: 0.8rem;">Reason / Holiday Name</label>
					<input id="closed-reason" type="text" class="form-input" bind:value={newClosedReason} placeholder="e.g. Christmas Day, Labor Day, Private Renovation" />
				</div>
				<button type="button" class="btn btn-secondary font-display" style="height: 42px; white-space: nowrap;" onclick={addClosedDate} disabled={!newClosedDate}>
					➕ Add Closed Date
				</button>
			</div>

			<!-- Configured Closed Dates List -->
			{#if closedDates.length === 0}
				<div style="padding: 1.5rem; text-align: center; color: var(--text-muted); font-size: 0.88rem; background: rgba(15, 23, 42, 0.3); border-radius: var(--radius-sm); border: 1px solid rgba(255, 255, 255, 0.04);">
					No special closed dates configured. The venue operates strictly according to the weekly operating hours above.
				</div>
			{:else}
				<div style="display: flex; flex-direction: column; gap: 0.6rem;">
					{#each closedDates as cd (cd.date)}
						<div style="display: flex; justify-content: space-between; align-items: center; padding: 0.65rem 1rem; background: rgba(239, 68, 68, 0.08); border: 1px solid rgba(239, 68, 68, 0.25); border-radius: var(--radius-sm);">
							<div style="display: flex; align-items: center; gap: 1rem;">
								<span style="font-weight: 700; color: #fca5a5; font-family: monospace; font-size: 0.95rem;">
									📅 {cd.date}
								</span>
								<span style="color: #f8fafc; font-size: 0.88rem;">
									{cd.reason}
								</span>
								<span style="background: rgba(239, 68, 68, 0.2); color: #f87171; font-size: 0.7rem; padding: 1px 6px; border-radius: 4px; font-weight: 700;">
									CLOSED
								</span>
							</div>
							<button type="button" class="btn-clear" style="color: #ef4444; font-size: 0.8rem;" onclick={() => removeClosedDate(cd.date)}>
								✕ Remove
							</button>
						</div>
					{/each}
				</div>
			{/if}
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
				<button type="button" class="btn btn-secondary btn-sm font-display" onclick={testGatewayConnection} disabled={isTestingConnection}>
					{isTestingConnection ? 'Testing...' : '⚡ Test Gateway Connection'}
				</button>
			</div>

			{#if connectionStatus}
				<div class={connectionStatus.success ? "alert-success" : "alert-danger"} style="margin-bottom: 1.25rem; font-size: 0.85rem; padding: 0.75rem 1rem; border-radius: var(--radius-sm); border: 1px solid {connectionStatus.success ? 'rgba(34, 197, 94, 0.4)' : 'rgba(239, 68, 68, 0.4)'}; background: {connectionStatus.success ? 'rgba(34, 197, 94, 0.1)' : 'rgba(239, 68, 68, 0.1)'}; color: {connectionStatus.success ? '#86efac' : '#fca5a5'};">
					{connectionStatus.success ? '✓' : '⚠️'} {connectionStatus.message}
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
