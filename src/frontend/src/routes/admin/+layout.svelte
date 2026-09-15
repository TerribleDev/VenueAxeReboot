<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth.svelte';
	import { venueState } from '$lib/stores/venueState.svelte';
	import { postApiAdminVenues } from '$lib/api/client';
	import './admin.css';

	let { children } = $props();
	let isChecking = $state(true);

	const isPublicAdminRoute = $derived(
		page.url.pathname === '/admin/login' || page.url.pathname === '/admin/register'
	);

	const currentPath = $derived(page.url.pathname);

	// New Venue Modal state
	let newVenueName = $state('');
	let newVenueAddress = $state('');
	let newVenueCity = $state('');
	let newVenueState = $state('');
	let newVenuePostalCode = $state('');
	let newVenueTimezone = $state('America/New_York');
	let newVenueIconFile = $state<File | null>(null);
	let isCreatingVenue = $state(false);
	let createVenueError = $state<string | null>(null);

	onMount(async () => {
		await auth.checkAuth();
		isChecking = false;

		if (!auth.isAuthenticated && !isPublicAdminRoute) {
			goto('/admin/login');
			return;
		}

		if (auth.isAuthenticated) {
			await venueState.loadVenues(auth.user?.id, auth.user?.venueId);
		}
	});

	$effect(() => {
		if (!isChecking && !auth.isAuthenticated && !isPublicAdminRoute) {
			goto('/admin/login');
		} else if (auth.isAuthenticated && (!venueState.selectedVenue || venueState.currentUserId !== auth.user?.id) && !venueState.isLoading) {
			venueState.loadVenues(auth.user?.id, auth.user?.venueId);
		}
	});

	async function handleLogout() {
		await auth.logout();
		venueState.reset();
		goto('/admin/login');
	}

	async function handleCreateVenue(e: SubmitEvent) {
		e.preventDefault();
		if (!newVenueName.trim()) return;

		isCreatingVenue = true;
		createVenueError = null;
		try {
			const res = await postApiAdminVenues({
				body: {
					name: newVenueName.trim(),
					slug: newVenueName.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, ''),
					addressLine1: newVenueAddress.trim() || '100 Main St',
					city: newVenueCity.trim() || 'Austin',
					state: newVenueState.trim() || 'TX',
					postalCode: newVenuePostalCode.trim() || '78701',
					phone: null,
					email: null,
					timezone: newVenueTimezone,
					currency: 'USD'
				}
			});

			if (res.data) {
				if (newVenueIconFile && res.data.id) {
					try {
						const fd = new FormData();
						fd.append('file', newVenueIconFile);
						await fetch(`/api/admin/venues/${res.data.id}/icon`, {
							method: 'POST',
							body: fd
						});
					} catch (e) {
						console.error('Failed to upload icon on venue creation', e);
					}
				}
				await venueState.loadVenues();
				venueState.setSelectedVenueId(res.data.id);
				venueState.showCreateVenueModal = false;
				newVenueName = '';
				newVenueAddress = '';
				newVenueCity = '';
				newVenueIconFile = null;
			} else {
				createVenueError = 'Failed to create venue.';
			}
		} catch (err: any) {
			createVenueError = err?.message || 'Error creating venue.';
		} finally {
			isCreatingVenue = false;
		}
	}
</script>

{#if isPublicAdminRoute}
	{@render children()}
{:else if isChecking}
	<div class="admin-auth-guard" style="min-height: 80vh; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 1rem;">
		<div class="spinner" style="width: 40px; height: 40px; border: 3px solid rgba(245, 158, 11, 0.2); border-top-color: var(--accent-amber); border-radius: 50%; animation: spin 0.8s linear infinite;"></div>
		<p class="font-display" style="color: var(--text-secondary);">Verifying Operator Credentials...</p>
	</div>
{:else if auth.isAuthenticated}
	<div class="admin-layout">
		<!-- Top Bar -->
		<header class="top-nav">
			<div class="brand">
				<span class="icon">🪓</span>
				<div>
					<div class="brand-title-row">
						<a href="/admin/lanes" style="text-decoration: none; color: inherit;">
							<h1 class="title font-display">VENUE<span class="text-amber">AXE</span> OPS</h1>
						</a>
						{#if (auth.user as any)?.tenantName}
							<span class="tenant-badge font-display">{(auth.user as any).tenantName}</span>
						{/if}
					</div>
					{#if venueState.venues.length > 0}
						<div class="venue-selector-container" style="display: flex; align-items: center; gap: 0.5rem;">
							{#if venueState.selectedVenue?.iconUrl}
								<img src={venueState.selectedVenue.iconUrl} alt={venueState.selectedVenue.name} style="width: 26px; height: 26px; border-radius: 6px; object-fit: contain; background: rgba(15, 23, 42, 0.6); border: 1px solid rgba(255, 255, 255, 0.2); flex-shrink: 0;" />
							{/if}
							<select
								class="venue-select-dropdown font-display"
								value={venueState.selectedVenue?.id}
								onchange={(e) => venueState.setSelectedVenueId(e.currentTarget.value)}
							>
								{#each venueState.venues as v (v.id)}
									<option value={v.id}>📍 {v.name} ({v.city})</option>
								{/each}
								<option value="__new__">➕ Add New Venue...</option>
							</select>
						</div>
					{/if}
				</div>
			</div>

			<!-- Nav Tabs (Separate Routes) -->
			<nav class="tabs">
				<a
					href="/admin/lanes"
					class="tab-btn"
					class:active={currentPath.startsWith('/admin/lanes') || currentPath === '/admin'}
				>
					🏟️ Lanes Overview
				</a>
				<a
					href="/admin/schedule"
					class="tab-btn"
					class:active={currentPath.startsWith('/admin/schedule')}
				>
					📊 Lane Schedule Matrix
				</a>
				<a
					href="/admin/bookings"
					class="tab-btn"
					class:active={currentPath.startsWith('/admin/bookings')}
				>
					📅 Reservations
				</a>
				<a
					href="/admin/reports"
					class="tab-btn"
					class:active={currentPath.startsWith('/admin/reports')}
				>
					📈 Reports & Analytics
				</a>
				<a
					href="/admin/waivers"
					class="tab-btn"
					class:active={currentPath.startsWith('/admin/waivers')}
				>
					✍️ Waiver Vault
				</a>
				<a
					href="/admin/editor"
					class="tab-btn"
					class:active={currentPath.startsWith('/admin/editor')}
				>
					🎨 Booking Page Editor
				</a>
				<a
					href="/admin/emails"
					class="tab-btn"
					class:active={currentPath.startsWith('/admin/emails')}
				>
					✉️ Email & SMTP
				</a>
				<a
					href="/admin/staff"
					class="tab-btn"
					class:active={currentPath.startsWith('/admin/staff')}
				>
					👥 Staff & Roles
				</a>
				<a
					href="/admin/settings"
					class="tab-btn"
					class:active={currentPath.startsWith('/admin/settings')}
				>
					⚙️ Settings & Hours
				</a>
			</nav>

			<!-- User Identity & Logout -->
			<div class="user-profile">
				{#if auth.user}
					<div class="user-info">
						<span class="user-name font-display">{auth.user.firstName} {auth.user.lastName}</span>
						<span class="role-badge">Owner</span>
					</div>
				{/if}
				<button class="btn btn-secondary btn-sm font-display" onclick={handleLogout}>
					Log Out
				</button>
			</div>
		</header>

		<!-- Subpage Content -->
		<main class="content-container">
			{@render children()}
		</main>

		{#if venueState.showCreateVenueModal}
			<div class="modal-overlay" role="button" tabindex="0" onclick={() => (venueState.showCreateVenueModal = false)} onkeydown={(e) => { if (e.key === 'Escape') venueState.showCreateVenueModal = false; }}>
				<div class="modal-card glass-panel" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.stopPropagation()}>
					<div class="modal-header-row">
						<div>
							<h3 class="modal-title font-display">Add New Venue</h3>
							<p class="editor-hint" style="margin-bottom: 0;">Create a new physical axe throwing facility</p>
						</div>
						<button type="button" class="btn-clear" onclick={() => (venueState.showCreateVenueModal = false)}>✕</button>
					</div>

					{#if createVenueError}
						<div class="alert-error" style="margin-top: 1rem;">
							⚠️ {createVenueError}
						</div>
					{/if}

					<form onsubmit={handleCreateVenue} style="margin-top: 1.25rem;">
						<div class="form-group">
							<label class="form-label" for="new-v-name">Venue Name *</label>
							<input id="new-v-name" type="text" class="form-input" bind:value={newVenueName} required placeholder="e.g. Apex Axes North" />
						</div>

						<div class="form-group" style="margin-top: 0.75rem;">
							<label class="form-label" for="new-v-addr">Street Address</label>
							<input id="new-v-addr" type="text" class="form-input" bind:value={newVenueAddress} placeholder="123 Thrower Way" />
						</div>

						<div class="form-row-2" style="margin-top: 0.75rem;">
							<div class="form-group">
								<label class="form-label" for="new-v-city">City</label>
								<input id="new-v-city" type="text" class="form-input" bind:value={newVenueCity} placeholder="Austin" />
							</div>
							<div class="form-group">
								<label class="form-label" for="new-v-state">State / Province</label>
								<input id="new-v-state" type="text" class="form-input" bind:value={newVenueState} placeholder="TX" />
							</div>
						</div>

						<div class="form-row-2" style="margin-top: 0.75rem;">
							<div class="form-group">
								<label class="form-label" for="new-v-zip">Postal Code</label>
								<input id="new-v-zip" type="text" class="form-input" bind:value={newVenuePostalCode} placeholder="78701" />
							</div>
							<div class="form-group">
								<label class="form-label" for="new-v-tz">Timezone</label>
								<select id="new-v-tz" class="form-input" bind:value={newVenueTimezone}>
									<option value="America/New_York">Eastern (US)</option>
									<option value="America/Chicago">Central (US)</option>
									<option value="America/Denver">Mountain (US)</option>
									<option value="America/Los_Angeles">Pacific (US)</option>
								</select>
							</div>
						</div>

						<div class="form-group" style="margin-top: 0.75rem;">
							<label class="form-label" for="new-v-icon">
								Venue Icon (Optional) <span style="font-size: 0.75rem; color: var(--text-secondary);">(Recommended: 512x512 square • PNG/WebP/SVG)</span>
							</label>
							<input
								id="new-v-icon"
								type="file"
								accept=".png,.jpg,.jpeg,.webp,.svg"
								class="form-input"
								onchange={(e) => {
									const f = (e.target as HTMLInputElement).files?.[0];
									newVenueIconFile = f || null;
								}}
							/>
						</div>

						<div class="modal-actions" style="margin-top: 1.5rem;">
							<button type="button" class="btn btn-secondary" onclick={() => (venueState.showCreateVenueModal = false)}>
								Cancel
							</button>
							<button type="submit" class="btn btn-primary font-display" disabled={isCreatingVenue}>
								{isCreatingVenue ? 'Creating...' : '+ Create Venue'}
							</button>
						</div>
					</form>
				</div>
			</div>
		{/if}
	</div>
{:else}
	<div class="admin-auth-guard" style="min-height: 80vh; display: flex; align-items: center; justify-content: center;">
		<p class="font-display" style="color: var(--text-secondary);">Redirecting to Operator Login...</p>
	</div>
{/if}
