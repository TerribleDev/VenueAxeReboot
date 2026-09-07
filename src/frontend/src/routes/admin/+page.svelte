<script lang="ts">
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth.svelte';
	import { goto } from '$app/navigation';
	import {
		getApiAdminVenues,
		getApiAdminLanesVenueByVenueId,
		postApiAdminLanesVenueByVenueId,
		putApiAdminLanesById,
		deleteApiAdminLanesById,
		putApiAdminLanesByIdStatus,
		postApiAdminLanesByIdRegeneratePairing,
		getApiAdminBookingsVenueByVenueId,
		putApiAdminBookingsByIdStatus,
		getApiAdminWaiversSearch,
		getApiAdminBookingConfigVenueByVenueId,
		putApiAdminBookingConfigVenueByVenueId,
		postApiLanesOperationsByLaneIdStartSession,
		postApiLanesOperationsByLaneIdSafetyStop
	} from '$lib/api/client';
	import type {
		VenueDto,
		LaneDto,
		BookingDto,
		WaiverDto,
		BookingConfigDto
	} from '$lib/api/generated/types.gen';

	let activeTab = $state<'lanes' | 'bookings' | 'waivers' | 'editor'>('lanes');
	let venues = $state<VenueDto[]>([]);
	let selectedVenue = $state<VenueDto | null>(null);

	// Tab data
	let lanes = $state<LaneDto[]>([]);
	let bookings = $state<BookingDto[]>([]);
	let waivers = $state<WaiverDto[]>([]);
	let bookingConfig = $state<BookingConfigDto | null>(null);
	let waiverSearchTerm = $state('');

	// Start Session Modal
	let selectedLaneForSession = $state<LaneDto | null>(null);
	let sessionTitle = $state('Walk-in Throwers');
	let sessionDuration = $state(60);
	let playerNames = $state('Player 1, Player 2');
	let isStartingSession = $state(false);

	// Create Lane Modal
	let showCreateLaneModal = $state(false);
	let newLaneNumber = $state(1);
	let newLaneName = $state('Lane 09');
	let newLaneMaxThrowers = $state(6);
	let isCreatingLane = $state(false);

	// Edit Lane Modal
	let editingLane = $state<LaneDto | null>(null);
	let editLaneNumber = $state(1);
	let editLaneName = $state('');
	let editLaneMaxThrowers = $state(6);
	let editLaneStatus = $state<number>(0);
	let isUpdatingLane = $state(false);

	// Delete Lane Modal
	let deletingLane = $state<LaneDto | null>(null);
	let isDeletingLane = $state(false);

	// Create Venue Modal
	let showCreateVenueModal = $state(false);
	let newVenueName = $state('');
	let newVenueAddress = $state('');
	let newVenueCity = $state('');
	let newVenueState = $state('TX');
	let newVenueTimezone = $state('America/Chicago');
	let isCreatingVenue = $state(false);

	onMount(async () => {
		await auth.init();
		if (!auth.isAuthenticated) {
			goto('/admin/login');
			return;
		}
		await loadVenues();
	});

	async function handleLogout() {
		await auth.logout();
		goto('/admin/login');
	}

	async function loadVenues() {
		try {
			const res = await getApiAdminVenues();
			if (res.data && res.data.length > 0) {
				venues = res.data;
				const cur = selectedVenue;
				if (!cur || !venues.some((v) => v.id === cur.id)) {
					selectedVenue = venues[0];
				}
				await loadTabData();
			}
		} catch (e) {
			console.error(e);
		}
	}

	async function handleVenueChange(venueId: string) {
		if (venueId === '__new__') {
			newVenueName = '';
			newVenueAddress = '';
			newVenueCity = '';
			showCreateVenueModal = true;
			return;
		}
		const found = venues.find((v) => v.id === venueId);
		if (found) {
			selectedVenue = found;
			await loadTabData();
		}
	}

	async function handleCreateVenue(e: SubmitEvent) {
		e.preventDefault();
		if (!newVenueName) return;
		isCreatingVenue = true;

		try {
			const res = await fetch('/api/admin/venues', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				credentials: 'include',
				body: JSON.stringify({
					name: newVenueName,
					addressLine1: newVenueAddress || '100 Main St',
					city: newVenueCity || 'Austin',
					state: newVenueState || 'TX',
					postalCode: '78701',
					timezone: newVenueTimezone
				})
			});

			if (res.ok) {
				const newVenue: VenueDto = await res.json();
				showCreateVenueModal = false;
				await loadVenues();
				selectedVenue = newVenue;
				await loadTabData();
			}
		} catch (err) {
			console.error(err);
		} finally {
			isCreatingVenue = false;
		}
	}

	async function loadTabData() {
		if (!selectedVenue) return;

		if (activeTab === 'lanes') {
			const res = await getApiAdminLanesVenueByVenueId({ path: { venueId: selectedVenue.id } });
			if (res.data) lanes = res.data;
		} else if (activeTab === 'bookings') {
			const res = await getApiAdminBookingsVenueByVenueId({ path: { venueId: selectedVenue.id } });
			if (res.data) bookings = res.data;
		} else if (activeTab === 'waivers') {
			const res = await getApiAdminWaiversSearch({
				query: { venueId: selectedVenue.id, term: waiverSearchTerm }
			});
			if (res.data) waivers = res.data;
		} else if (activeTab === 'editor') {
			const res = await getApiAdminBookingConfigVenueByVenueId({ path: { venueId: selectedVenue.id } });
			if (res.data) bookingConfig = res.data;
		}
	}

	function handleTabChange(tab: 'lanes' | 'bookings' | 'waivers' | 'editor') {
		activeTab = tab;
		loadTabData();
	}

	// --- LANE CRUD ACTIONS ---

	function openCreateLaneModal() {
		const maxNum = lanes.reduce((acc, l) => Math.max(acc, Number(l.laneNumber || 0)), 0);
		newLaneNumber = maxNum + 1;
		newLaneName = `Lane ${String(newLaneNumber).padStart(2, '0')}`;
		newLaneMaxThrowers = 6;
		showCreateLaneModal = true;
	}

	async function handleCreateLane(e: SubmitEvent) {
		e.preventDefault();
		if (!selectedVenue) return;
		isCreatingLane = true;

		try {
			const res = await postApiAdminLanesVenueByVenueId({
				path: { venueId: selectedVenue.id },
				body: {
					laneNumber: newLaneNumber,
					name: newLaneName,
					maxThrowers: newLaneMaxThrowers
				}
			});
			if (res.data) {
				showCreateLaneModal = false;
				await loadTabData();
			}
		} catch (e) {
			alert('Failed to create lane.');
		} finally {
			isCreatingLane = false;
		}
	}

	function openEditLaneModal(lane: LaneDto) {
		editingLane = lane;
		editLaneNumber = Number(lane.laneNumber || 1);
		editLaneName = lane.name || '';
		editLaneMaxThrowers = Number(lane.maxThrowers || 6);
		editLaneStatus = Number(lane.currentStatus || 0);
	}

	async function handleUpdateLane(e: SubmitEvent) {
		e.preventDefault();
		if (!editingLane) return;
		isUpdatingLane = true;

		try {
			const res = await putApiAdminLanesById({
				path: { id: editingLane.id },
				body: {
					laneNumber: editLaneNumber,
					name: editLaneName,
					maxThrowers: editLaneMaxThrowers,
					status: editLaneStatus as any
				}
			});
			if (res.data) {
				editingLane = null;
				await loadTabData();
			}
		} catch (e) {
			alert('Failed to update lane.');
		} finally {
			isUpdatingLane = false;
		}
	}

	async function handleDeleteLane() {
		if (!deletingLane) return;
		isDeletingLane = true;

		try {
			await deleteApiAdminLanesById({
				path: { id: deletingLane.id }
			});
			deletingLane = null;
			await loadTabData();
		} catch (e) {
			alert('Failed to delete lane.');
		} finally {
			isDeletingLane = false;
		}
	}

	async function handleRegeneratePairing(laneId: string) {
		try {
			const res = await postApiAdminLanesByIdRegeneratePairing({ path: { id: laneId } });
			if (res.data) {
				await loadTabData();
			}
		} catch (e) {
			console.error(e);
		}
	}

	async function handleUpdateLaneStatus(laneId: string, status: number) {
		try {
			await putApiAdminLanesByIdStatus({
				path: { id: laneId },
				query: { status: status as any }
			});
			await loadTabData();
		} catch (e) {
			console.error(e);
		}
	}

	async function handleStartSession(e: SubmitEvent) {
		e.preventDefault();
		if (!selectedLaneForSession) return;

		isStartingSession = true;
		try {
			const names = playerNames
				.split(',')
				.map((n) => n.trim())
				.filter(Boolean);

			const colors = ['#f59e0b', '#06b6d4', '#ef4444', '#10b981', '#8b5cf6', '#ec4899'];
			const roster = (names.length > 0 ? names : ['Player 1', 'Player 2']).map((name, idx) => ({
				id: `p-${idx + 1}`,
				name,
				avatarColor: colors[idx % colors.length],
				score: 0,
				throwsTaken: 0,
				bullseyesHit: 0,
				clutchesHit: 0,
				streak: 0,
				throwHistory: []
			}));

			const res = await postApiLanesOperationsByLaneIdStartSession({
				path: { laneId: selectedLaneForSession.id },
				body: {
					sessionTitle,
					durationMinutes: sessionDuration,
					initialRoster: roster,
					bookingId: null
				}
			});

			if (res.data) {
				selectedLaneForSession = null;
				await loadTabData();
			}
		} catch (e) {
			console.error('Failed to start session:', e);
			alert('Failed to start session. Please try again.');
		} finally {
			isStartingSession = false;
		}
	}

	async function handleUpdateBookingStatus(bookingId: string, status: number) {
		try {
			await putApiAdminBookingsByIdStatus({
				path: { id: bookingId },
				query: { status: status as any }
			});
			await loadTabData();
		} catch (e) {
			console.error(e);
		}
	}

	async function handleSaveConfig(e: SubmitEvent) {
		e.preventDefault();
		if (!selectedVenue || !bookingConfig) return;

		try {
			const res = await putApiAdminBookingConfigVenueByVenueId({
				path: { venueId: selectedVenue.id },
				body: {
					minPartySize: Number(bookingConfig.minPartySize),
					maxPartySize: Number(bookingConfig.maxPartySize),
					slotDurationsMinutes: (bookingConfig.slotDurationsMinutes || []).map(Number),
					turnaroundBufferMinutes: Number(bookingConfig.turnaroundBufferMinutes),
					pricingModel: Number(bookingConfig.pricingModel) as any,
					basePriceCents: Number(bookingConfig.basePriceCents),
					peakPriceCents: Number(bookingConfig.peakPriceCents),
					depositType: Number(bookingConfig.depositType) as any,
					depositAmountCents: Number(bookingConfig.depositAmountCents),
					editorThemeJson: bookingConfig.editorThemeJson || '',
					customFieldsJson: bookingConfig.customFieldsJson || '',
					packagesJson: bookingConfig.packagesJson || '',
					cancellationPolicy: bookingConfig.cancellationPolicy || ''
				}
			});
			if (res.data) {
				alert('Booking Page Configuration saved successfully!');
			}
		} catch (e) {
			console.error(e);
		}
	}

	function getStatusBadge(status: number | string | undefined) {
		const s = Number(status);
		switch (s) {
			case 0:
				return { label: 'Available', class: 'badge-available' };
			case 1:
				return { label: 'Active Match', class: 'badge-active' };
			case 2:
				return { label: 'Turnaround', class: 'badge-turnaround' };
			case 3:
				return { label: 'Maintenance', class: 'badge-maintenance' };
			case 4:
				return { label: 'Out of Service', class: 'badge-maintenance' };
			default:
				return { label: 'Available', class: 'badge-available' };
		}
	}
</script>

<div class="admin-layout">
	<!-- Top Bar -->
	<div class="top-nav glass-panel">
		<div class="brand">
			<span class="icon">🪓</span>
			<div>
				<div class="brand-title-row">
					<h1 class="title font-display">VENUE<span class="text-amber">AXE</span> OPS</h1>
					{#if (auth.user as any)?.tenantName}
						<span class="tenant-badge font-display">{(auth.user as any).tenantName}</span>
					{/if}
				</div>
				{#if venues.length > 0}
					<div class="venue-selector-container">
						<select
							class="venue-select-dropdown font-display"
							value={selectedVenue?.id}
							onchange={(e) => handleVenueChange(e.currentTarget.value)}
						>
							{#each venues as v (v.id)}
								<option value={v.id}>📍 {v.name} ({v.city})</option>
							{/each}
							<option value="__new__">➕ Add New Venue...</option>
						</select>
					</div>
				{/if}
			</div>
		</div>

		<!-- Nav Tabs -->
		<div class="tabs">
			<button class="tab-btn" class:active={activeTab === 'lanes'} onclick={() => handleTabChange('lanes')}>
				🏟️ Lanes Overview
			</button>
			<button class="tab-btn" class:active={activeTab === 'bookings'} onclick={() => handleTabChange('bookings')}>
				📅 Reservations
			</button>
			<button class="tab-btn" class:active={activeTab === 'waivers'} onclick={() => handleTabChange('waivers')}>
				✍️ Waiver Vault
			</button>
			<button class="tab-btn" class:active={activeTab === 'editor'} onclick={() => handleTabChange('editor')}>
				🎨 Booking Page Editor
			</button>
		</div>

		<!-- User Identity & Logout -->
		<div class="user-profile">
			{#if auth.user}
				<div class="user-info">
					<span class="user-name font-display">{auth.user.firstName} {auth.user.lastName}</span>
					<span class="role-badge">Owner</span>
				</div>
				<button class="btn btn-secondary btn-sm" onclick={handleLogout}>Log Out</button>
			{:else}
				<a href="/admin/login" class="btn btn-primary btn-sm">Staff Login</a>
			{/if}
		</div>
	</div>

	<!-- Main Content Body -->
	<div class="content-container">
		<!-- 1. LANES OVERVIEW TAB -->
		{#if activeTab === 'lanes'}
			<div class="tab-header">
				<div>
					<h2 class="font-display">Live Lane Management</h2>
					<p class="tab-subtitle">Real-time telemetry, session dispatch, and hardware terminal status</p>
				</div>
				<button class="btn btn-primary font-display" onclick={openCreateLaneModal}>
					+ Add New Lane
				</button>
			</div>

			{#if lanes.length === 0}
				<div class="empty-lanes-state glass-panel">
					<span class="empty-icon">🏟️</span>
					<h3 class="font-display empty-title">No Target Lanes Created Yet</h3>
					<p class="empty-desc">
						This venue currently has no lanes configured. Add your first target throwing bay to generate hardware pairing PINs and launch matches.
					</p>
					<button class="btn btn-primary font-display" onclick={openCreateLaneModal}>
						+ Add Your First Lane
					</button>
				</div>
			{:else}
				<div class="lanes-grid">
					{#each lanes as lane (lane.id)}
						{@const status = getStatusBadge(lane.currentStatus)}
						<div class="lane-card glass-panel" class:card-active={Number(lane.currentStatus) === 1}>
							<div class="lane-header">
								<span class="lane-name font-display">{lane.name}</span>
								<span class="badge {status.class}">{status.label}</span>
							</div>

							<!-- Active Session Timer -->
							{#if lane.activeSession}
								<div class="session-box">
									<div class="session-title font-display">{lane.activeSession.sessionTitle}</div>
									<div class="timer-display font-display">
										⏱️ {lane.activeSession.minutesRemaining} MIN REMAINING
									</div>
									{#if lane.activeSession.currentGame}
										<div class="game-info">
											<span>Mode: {lane.activeSession.currentGame.gameName}</span>
											<span>Round: {lane.activeSession.currentGame.currentRound}/{lane.activeSession.currentGame.totalRounds}</span>
										</div>
									{/if}
								</div>
							{:else}
								<div class="empty-lane-box">
									<span class="text-secondary">Bay Ready For Throwers</span>
									<span class="capacity-tag font-display">Max {lane.maxThrowers} Throwers</span>
								</div>
							{/if}

							<!-- Pairing Codes Section -->
							<div class="pair-row">
								<div class="pair-chip">
									<small>Tablet PIN</small>
									<strong class="font-display">{lane.tabletPairingCode || '---'}</strong>
								</div>
								<div class="pair-chip">
									<small>TV PIN</small>
									<strong class="font-display">{lane.screenPairingCode || '---'}</strong>
								</div>
								<button
									class="btn-icon"
									title="Regenerate Pairing PINs"
									onclick={() => handleRegeneratePairing(lane.id)}
								>
									🔄
								</button>
							</div>

						<!-- Status Quick Selector -->
						<div class="status-select-row">
							<label class="status-lbl" for="status-{lane.id}">Status:</label>
							<select
								id="status-{lane.id}"
								class="form-select-sm"
								value={Number(lane.currentStatus)}
								onchange={(e) => handleUpdateLaneStatus(lane.id, Number(e.currentTarget.value))}
							>
								<option value={0}>Available</option>
								<option value={1}>Active Match</option>
								<option value={2}>Turnaround</option>
								<option value={3}>Maintenance</option>
								<option value={4}>Out of Service</option>
							</select>
						</div>

						<!-- Action Buttons -->
						<div class="lane-actions">
							{#if !lane.activeSession}
								<button
									class="btn btn-primary btn-sm flex-1 font-display"
									onclick={() => {
										selectedLaneForSession = lane;
										sessionTitle = `Walk-in (${lane.name})`;
									}}
								>
									+ Start Session
								</button>
							{/if}

							<button class="btn btn-secondary btn-sm" onclick={() => openEditLaneModal(lane)}>
								✏️ Edit
							</button>
							<button class="btn btn-secondary btn-sm btn-delete" onclick={() => (deletingLane = lane)}>
								🗑️
							</button>
						</div>
					</div>
				{/each}
			</div>
		{/if}

		<!-- 2. RESERVATIONS TAB -->
		{:else if activeTab === 'bookings'}
			<div class="tab-header">
				<div>
					<h2 class="font-display">Customer Reservations</h2>
					<p class="tab-subtitle">Upcoming party bookings, capacity allocation, and check-in</p>
				</div>
			</div>

			<div class="table-card glass-panel">
				<table class="data-table">
					<thead>
						<tr>
							<th>Ref #</th>
							<th>Guest Name</th>
							<th>Party Size</th>
							<th>Start Time</th>
							<th>Assigned Bays</th>
							<th>Waivers</th>
							<th>Total</th>
							<th>Status</th>
							<th>Actions</th>
						</tr>
					</thead>
					<tbody>
						{#each bookings as b (b.id)}
							<tr>
								<td><strong class="font-display text-amber">{b.bookingReference}</strong></td>
								<td>{b.guestFirstName} {b.guestLastName}<br /><small class="text-muted">{b.guestEmail}</small></td>
								<td>{b.partySize} Throwers</td>
								<td>{new Date(b.startTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })}</td>
								<td>Lanes {b.assignedLaneNumbers.join(', ')}</td>
								<td>
									<span class="badge {Number(b.signedWaiverCount) >= Number(b.partySize) ? 'badge-available' : 'badge-turnaround'}">
										{b.signedWaiverCount} / {b.partySize} Signed
									</span>
								</td>
								<td>${(Number(b.totalAmountCents) / 100).toFixed(2)}</td>
								<td>
									<span class="badge {Number(b.status) === 1 ? 'badge-available' : 'badge-active'}">
										{Number(b.status) === 1 ? 'Confirmed' : Number(b.status) === 2 ? 'Checked In' : 'Completed'}
									</span>
								</td>
								<td>
									{#if Number(b.status) === 1}
										<button class="btn btn-primary btn-sm" onclick={() => handleUpdateBookingStatus(b.id, 2)}>
											Check In
										</button>
									{:else if Number(b.status) === 2}
										<button class="btn btn-secondary btn-sm" onclick={() => handleUpdateBookingStatus(b.id, 3)}>
											Complete
										</button>
									{/if}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>

		<!-- 3. WAIVER VAULT TAB -->
		{:else if activeTab === 'waivers'}
			<div class="tab-header">
				<div>
					<h2 class="font-display">Digital Waiver Vault</h2>
					<p class="tab-subtitle">Cryptographic safety release records and minor consent tracking</p>
				</div>
				<div class="search-box">
					<input
						type="text"
						class="form-input"
						placeholder="Search by name, email or phone..."
						bind:value={waiverSearchTerm}
						oninput={() => loadTabData()}
					/>
				</div>
			</div>

			<div class="table-card glass-panel">
				<table class="data-table">
					<thead>
						<tr>
							<th>Signer Name</th>
							<th>Contact Info</th>
							<th>Date of Birth</th>
							<th>Guardian Signing</th>
							<th>Minors Covered</th>
							<th>Signed At</th>
							<th>Signature Preview</th>
						</tr>
					</thead>
					<tbody>
						{#each waivers as w (w.id)}
							<tr>
								<td><strong>{w.signerFirstName} {w.signerLastName}</strong></td>
								<td>{w.signerEmail}<br /><small class="text-muted">{w.signerPhone}</small></td>
								<td>{w.dateOfBirth}</td>
								<td>{w.isGuardianSigning ? 'Yes (Parent/Guardian)' : 'Self'}</td>
								<td>
									{#if w.minorsCoveredJson}
										<span class="text-secondary">{w.minorsCoveredJson}</span>
									{:else}
										<span class="text-muted">None</span>
									{/if}
								</td>
								<td>{new Date(w.signedAtUtc || (w as any).signedAt).toLocaleString()}</td>
								<td>
									{#if w.signatureImagePngBase64}
										<img src={w.signatureImagePngBase64} alt="Signature" class="sig-thumb" />
									{/if}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>

		<!-- 4. BOOKING PAGE EDITOR TAB -->
		{:else if activeTab === 'editor' && bookingConfig}
			<div class="tab-header">
				<div>
					<h2 class="font-display">Visual Booking Page Editor</h2>
					<p class="tab-subtitle">Customize pricing, party sizes, time increments, and packages</p>
				</div>
			</div>

			<form onsubmit={handleSaveConfig} class="editor-grid">
				<div class="editor-section glass-panel">
					<h3 class="font-display">Capacity & Duration Rules</h3>

					<div class="form-grid">
						<div class="form-group">
							<label class="form-label" for="min-party">Min Party Size</label>
							<input id="min-party" type="number" class="form-input" bind:value={bookingConfig.minPartySize} />
						</div>
						<div class="form-group">
							<label class="form-label" for="max-party">Max Party Size</label>
							<input id="max-party" type="number" class="form-input" bind:value={bookingConfig.maxPartySize} />
						</div>
					</div>

					<div class="form-grid" style="margin-top: 1rem;">
						<div class="form-group">
							<label class="form-label" for="buffer-min">Turnaround Buffer (Minutes)</label>
							<input id="buffer-min" type="number" class="form-input" bind:value={bookingConfig.turnaroundBufferMinutes} />
						</div>
					</div>
				</div>

				<div class="editor-section glass-panel">
					<h3 class="font-display">Pricing & Deposit Model</h3>

					<div class="form-grid">
						<div class="form-group">
							<label class="form-label" for="base-price">Base Price ($)</label>
							<input
								id="base-price"
								type="number"
								step="0.01"
								class="form-input"
								value={Number(bookingConfig.basePriceCents) / 100}
								oninput={(e) => (bookingConfig!.basePriceCents = Number(e.currentTarget.value) * 100)}
							/>
						</div>
						<div class="form-group">
							<label class="form-label" for="peak-price">Peak Weekend Price ($)</label>
							<input
								id="peak-price"
								type="number"
								step="0.01"
								class="form-input"
								value={Number(bookingConfig.peakPriceCents) / 100}
								oninput={(e) => (bookingConfig!.peakPriceCents = Number(e.currentTarget.value) * 100)}
							/>
						</div>
					</div>
				</div>

				<div class="editor-section glass-panel" style="grid-column: 1 / -1;">
					<h3 class="font-display">Packages & Add-ons (JSON Configuration)</h3>
					<textarea class="form-input font-mono" rows="8" bind:value={bookingConfig.packagesJson}></textarea>

					<button type="submit" class="btn btn-primary font-display" style="margin-top: 1.5rem;">
						💾 Save Configuration Changes
					</button>
				</div>
			</form>
		{/if}
	</div>

	<!-- START SESSION MODAL -->
	{#if selectedLaneForSession}
		<div class="modal-overlay">
			<div class="modal-card glass-panel">
				<h3 class="modal-title font-display">Start Throwing Session - {selectedLaneForSession.name}</h3>

				<form onsubmit={handleStartSession}>
					<div class="form-group">
						<label class="form-label" for="session-title">Session / Group Title</label>
						<input id="session-title" type="text" class="form-input" bind:value={sessionTitle} required />
					</div>

					<div class="form-group" style="margin-top: 1rem;">
						<label class="form-label" for="session-duration">Duration (Minutes)</label>
						<select id="session-duration" class="form-input" bind:value={sessionDuration}>
							<option value={30}>30 Minutes</option>
							<option value={60}>60 Minutes (Standard)</option>
							<option value={90}>90 Minutes</option>
							<option value={120}>120 Minutes</option>
						</select>
					</div>

					<div class="form-group" style="margin-top: 1rem;">
						<label class="form-label" for="player-names">Player Names (comma-separated)</label>
						<input id="player-names" type="text" class="form-input" bind:value={playerNames} placeholder="Sarah, Marcus, Alex" required />
					</div>

					<div class="modal-actions">
						<button type="button" class="btn btn-secondary" onclick={() => (selectedLaneForSession = null)}>
							Cancel
						</button>
						<button type="submit" class="btn btn-primary font-display" disabled={isStartingSession}>
							{isStartingSession ? 'Launching...' : '🚀 Launch Match Session'}
						</button>
					</div>
				</form>
			</div>
		</div>
	{/if}

	<!-- CREATE LANE MODAL -->
	{#if showCreateLaneModal}
		<div class="modal-overlay">
			<div class="modal-card glass-panel">
				<h3 class="modal-title font-display">Create New Lane Bay</h3>

				<form onsubmit={handleCreateLane}>
					<div class="form-grid">
						<div class="form-group">
							<label class="form-label" for="new-lane-num">Lane Number</label>
							<input id="new-lane-num" type="number" class="form-input" bind:value={newLaneNumber} required min="1" />
						</div>
						<div class="form-group">
							<label class="form-label" for="new-lane-cap">Max Throwers</label>
							<input id="new-lane-cap" type="number" class="form-input" bind:value={newLaneMaxThrowers} required min="1" max="24" />
						</div>
					</div>

					<div class="form-group" style="margin-top: 1rem;">
						<label class="form-label" for="new-lane-name">Lane Display Name</label>
						<input id="new-lane-name" type="text" class="form-input" bind:value={newLaneName} required placeholder="Lane 09 - Target Bay" />
					</div>

					<div class="modal-actions">
						<button type="button" class="btn btn-secondary" onclick={() => (showCreateLaneModal = false)}>
							Cancel
						</button>
						<button type="submit" class="btn btn-primary font-display" disabled={isCreatingLane}>
							{isCreatingLane ? 'Creating...' : '+ Create Lane'}
						</button>
					</div>
				</form>
			</div>
		</div>
	{/if}

	<!-- EDIT LANE MODAL -->
	{#if editingLane}
		<div class="modal-overlay">
			<div class="modal-card glass-panel">
				<h3 class="modal-title font-display">Edit Lane Details</h3>

				<form onsubmit={handleUpdateLane}>
					<div class="form-grid">
						<div class="form-group">
							<label class="form-label" for="edit-lane-num">Lane Number</label>
							<input id="edit-lane-num" type="number" class="form-input" bind:value={editLaneNumber} required min="1" />
						</div>
						<div class="form-group">
							<label class="form-label" for="edit-lane-cap">Max Throwers</label>
							<input id="edit-lane-cap" type="number" class="form-input" bind:value={editLaneMaxThrowers} required min="1" max="24" />
						</div>
					</div>

					<div class="form-group" style="margin-top: 1rem;">
						<label class="form-label" for="edit-lane-name">Lane Display Name</label>
						<input id="edit-lane-name" type="text" class="form-input" bind:value={editLaneName} required />
					</div>

					<div class="form-group" style="margin-top: 1rem;">
						<label class="form-label" for="edit-lane-status">Status</label>
						<select id="edit-lane-status" class="form-input" bind:value={editLaneStatus}>
							<option value={0}>Available</option>
							<option value={1}>Active Match</option>
							<option value={2}>Turnaround</option>
							<option value={3}>Maintenance</option>
							<option value={4}>Out of Service</option>
						</select>
					</div>

					<div class="modal-actions">
						<button type="button" class="btn btn-secondary" onclick={() => (editingLane = null)}>
							Cancel
						</button>
						<button type="submit" class="btn btn-primary font-display" disabled={isUpdatingLane}>
							{isUpdatingLane ? 'Saving...' : '💾 Save Lane'}
						</button>
					</div>
				</form>
			</div>
		</div>
	{/if}

	<!-- DELETE LANE CONFIRMATION MODAL -->
	{#if deletingLane}
		<div class="modal-overlay">
			<div class="modal-card glass-panel modal-danger">
				<h3 class="modal-title font-display">⚠️ Confirm Lane Deletion</h3>
				<p style="margin: 1rem 0; color: var(--text-secondary);">
					Are you sure you want to delete <strong class="text-amber">{deletingLane.name}</strong>? This action cannot be undone.
				</p>

				<div class="modal-actions">
					<button type="button" class="btn btn-secondary" onclick={() => (deletingLane = null)}>
						Cancel
					</button>
					<button type="button" class="btn btn-danger font-display" disabled={isDeletingLane} onclick={handleDeleteLane}>
						{isDeletingLane ? 'Deleting...' : '🗑️ Confirm Delete Lane'}
					</button>
				</div>
			</div>
		</div>
	{/if}

	<!-- CREATE VENUE MODAL -->
	{#if showCreateVenueModal}
		<div class="modal-overlay">
			<div class="modal-card glass-panel">
				<h3 class="modal-title font-display">➕ Add New Venue Location</h3>
				<p style="margin-bottom: 1.25rem; color: var(--text-secondary); font-size: 0.9rem;">
					Add an additional venue under your organization with automatic lane seeding, digital waiver, and booking engine.
				</p>

				<form onsubmit={handleCreateVenue}>
					<div class="form-group">
						<label class="form-label" for="new-v-name">Venue Name *</label>
						<input id="new-v-name" type="text" class="form-input" bind:value={newVenueName} required placeholder="e.g. Apex Axe House - Northside" />
					</div>

					<div class="form-grid" style="margin-top: 1rem;">
						<div class="form-group">
							<label class="form-label" for="new-v-city">City *</label>
							<input id="new-v-city" type="text" class="form-input" bind:value={newVenueCity} required placeholder="e.g. Austin" />
						</div>
						<div class="form-group">
							<label class="form-label" for="new-v-state">State</label>
							<input id="new-v-state" type="text" class="form-input" bind:value={newVenueState} placeholder="TX" />
						</div>
					</div>

					<div class="form-group" style="margin-top: 1rem;">
						<label class="form-label" for="new-v-address">Street Address</label>
						<input id="new-v-address" type="text" class="form-input" bind:value={newVenueAddress} placeholder="e.g. 500 Lamar Blvd" />
					</div>

					<div class="form-group" style="margin-top: 1rem;">
						<label class="form-label" for="new-v-tz">Timezone</label>
						<select id="new-v-tz" class="form-input" bind:value={newVenueTimezone}>
							<option value="America/New_York">Eastern Time (US & Canada)</option>
							<option value="America/Chicago">Central Time (US & Canada)</option>
							<option value="America/Denver">Mountain Time (US & Canada)</option>
							<option value="America/Los_Angeles">Pacific Time (US & Canada)</option>
						</select>
					</div>

					<div class="modal-actions">
						<button type="button" class="btn btn-secondary" onclick={() => (showCreateVenueModal = false)}>
							Cancel
						</button>
						<button type="submit" class="btn btn-primary font-display" disabled={isCreatingVenue}>
							{isCreatingVenue ? 'Creating Venue...' : '🚀 Launch Venue Location'}
						</button>
					</div>
				</form>
			</div>
		</div>
	{/if}
</div>

<style>
	.brand-title-row {
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.tenant-badge {
		font-size: 0.75rem;
		background: rgba(245, 158, 11, 0.15);
		border: 1px solid rgba(245, 158, 11, 0.4);
		color: var(--accent-amber);
		padding: 0.15rem 0.5rem;
		border-radius: 4px;
		letter-spacing: 0.05em;
		text-transform: uppercase;
	}

	.venue-selector-container {
		margin-top: 0.35rem;
	}

	.venue-select-dropdown {
		background: #0f131a;
		color: #38bdf8;
		border: 1px solid #0284c7;
		padding: 0.3rem 0.6rem;
		border-radius: var(--radius-sm);
		font-size: 0.85rem;
		font-weight: 700;
		cursor: pointer;
		outline: none;
		transition: border-color 0.2s ease, box-shadow 0.2s ease;
	}

	.venue-select-dropdown:hover,
	.venue-select-dropdown:focus {
		border-color: #38bdf8;
		box-shadow: 0 0 8px rgba(56, 189, 248, 0.3);
	}

	.admin-layout {
		min-height: 100vh;
		display: flex;
		flex-direction: column;
	}

	.top-nav {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 0.75rem 2rem;
		border-radius: 0;
		border-left: none;
		border-right: none;
		border-top: none;
	}

	.brand {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}

	.icon { font-size: 2rem; }
	.title { font-size: 1.4rem; font-weight: 900; line-height: 1; }
	.text-amber { color: var(--accent-amber); }
	.venue-badge { font-size: 0.75rem; color: var(--text-secondary); }

	.tabs {
		display: flex;
		gap: 0.5rem;
	}

	.tab-btn {
		background: transparent;
		border: 1px solid transparent;
		color: var(--text-secondary);
		padding: 0.6rem 1rem;
		border-radius: var(--radius-md);
		cursor: pointer;
		font-weight: 600;
		font-size: 0.9rem;
		transition: all 0.15s ease;
	}

	.tab-btn:hover {
		color: var(--text-primary);
		background: var(--bg-surface-elevated);
	}

	.tab-btn.active {
		color: var(--accent-amber);
		background: rgba(245, 158, 11, 0.12);
		border-color: var(--accent-amber);
	}

	.user-profile {
		display: flex;
		align-items: center;
		gap: 1rem;
	}

	.user-info {
		text-align: right;
	}

	.user-name {
		font-size: 0.95rem;
		font-weight: 700;
		display: block;
	}

	.role-badge {
		font-size: 0.7rem;
		text-transform: uppercase;
		color: var(--accent-amber);
		letter-spacing: 0.05em;
	}

	.content-container {
		max-width: 1400px;
		margin: 0 auto;
		padding: 2rem 1.5rem 4rem;
		width: 100%;
	}

	.tab-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 1.5rem;
	}

	.tab-subtitle {
		color: var(--text-secondary);
		font-size: 0.9rem;
		margin-top: 0.2rem;
	}

	.lanes-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
		gap: 1.5rem;
	}

	.empty-lanes-state {
		text-align: center;
		padding: 4rem 2rem;
		border-radius: var(--radius-xl);
		display: flex;
		flex-direction: column;
		align-items: center;
		max-width: 600px;
		margin: 2rem auto;
		background: rgba(17, 21, 31, 0.85);
		border: 1px dashed var(--border-color);
	}

	.empty-icon {
		font-size: 3.5rem;
		margin-bottom: 1rem;
		display: block;
	}

	.empty-title {
		font-size: 1.5rem;
		font-weight: 800;
		color: var(--text-primary);
		margin-bottom: 0.5rem;
	}

	.empty-desc {
		color: var(--text-secondary);
		font-size: 0.95rem;
		line-height: 1.6;
		margin-bottom: 1.75rem;
	}

	.lane-card {
		padding: 1.5rem;
		display: flex;
		flex-direction: column;
		gap: 1rem;
		transition: all 0.2s ease;
	}

	.card-active {
		border-color: var(--accent-amber);
		box-shadow: 0 0 20px rgba(245, 158, 11, 0.2);
	}

	.lane-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.lane-name {
		font-size: 1.3rem;
		font-weight: 800;
	}

	.session-box {
		background: #101520;
		border: 1px solid var(--border-color);
		padding: 1rem;
		border-radius: var(--radius-md);
	}

	.session-title {
		font-weight: 700;
		margin-bottom: 0.35rem;
	}

	.timer-display {
		color: var(--accent-amber);
		font-weight: 800;
		font-size: 1.1rem;
	}

	.game-info {
		display: flex;
		justify-content: space-between;
		font-size: 0.8rem;
		color: var(--text-secondary);
		margin-top: 0.5rem;
		border-top: 1px solid var(--border-color);
		padding-top: 0.5rem;
	}

	.empty-lane-box {
		background: var(--bg-surface);
		border: 1px dashed var(--border-color);
		padding: 1.25rem;
		border-radius: var(--radius-md);
		text-align: center;
		display: flex;
		flex-direction: column;
		gap: 0.35rem;
	}

	.capacity-tag {
		font-size: 0.8rem;
		color: var(--accent-amber);
		font-weight: 700;
	}

	.pair-row {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		background: var(--bg-surface);
		padding: 0.5rem 0.75rem;
		border-radius: var(--radius-sm);
	}

	.pair-chip {
		flex: 1;
		display: flex;
		flex-direction: column;
	}

	.pair-chip small {
		font-size: 0.65rem;
		color: var(--text-muted);
		text-transform: uppercase;
	}

	.pair-chip strong {
		color: var(--accent-amber);
		letter-spacing: 0.1em;
	}

	.btn-icon {
		background: transparent;
		border: none;
		cursor: pointer;
		font-size: 1.1rem;
	}

	.status-select-row {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}

	.status-lbl {
		font-size: 0.8rem;
		color: var(--text-secondary);
	}

	.form-select-sm {
		flex: 1;
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		color: var(--text-primary);
		padding: 0.3rem 0.5rem;
		border-radius: var(--radius-sm);
		font-size: 0.85rem;
	}

	.lane-actions {
		display: flex;
		gap: 0.5rem;
		margin-top: 0.25rem;
	}

	.btn-delete {
		color: #ef4444;
	}

	.table-card {
		overflow-x: auto;
	}

	.data-table {
		width: 100%;
		border-collapse: collapse;
		text-align: left;
		font-size: 0.9rem;
	}

	.data-table th {
		background: #0f131a;
		padding: 0.9rem 1rem;
		color: var(--text-secondary);
		text-transform: uppercase;
		font-size: 0.75rem;
		letter-spacing: 0.05em;
	}

	.data-table td {
		padding: 0.9rem 1rem;
		border-top: 1px solid var(--border-color);
	}

	.sig-thumb {
		height: 35px;
		filter: invert(1);
	}

	.editor-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1.5rem;
	}

	.editor-section {
		padding: 1.5rem;
	}

	.form-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1rem;
	}

	/* Modals */
	.modal-overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.75);
		backdrop-filter: blur(4px);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 100;
	}

	.modal-card {
		width: 100%;
		max-width: 500px;
		padding: 2rem;
	}

	.modal-danger {
		border-color: var(--accent-crimson);
	}

	.modal-title {
		font-size: 1.4rem;
		margin-bottom: 1.25rem;
	}

	.modal-actions {
		display: flex;
		justify-content: flex-end;
		gap: 0.75rem;
		margin-top: 1.75rem;
	}
</style>
