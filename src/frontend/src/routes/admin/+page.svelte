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

	let activeTab = $state<'lanes' | 'schedule' | 'bookings' | 'waivers' | 'editor'>('lanes');
	let venues = $state<VenueDto[]>([]);
	let selectedVenue = $state<VenueDto | null>(null);

	// Tab data
	let lanes = $state<LaneDto[]>([]);
	let bookings = $state<BookingDto[]>([]);
	let waivers = $state<WaiverDto[]>([]);
	let bookingConfig = $state<BookingConfigDto | null>(null);
	let waiverSearchTerm = $state('');

	// Schedule Matrix State
	let scheduleDate = $state(new Date().toISOString().split('T')[0]);
	let scheduleMatrix = $state<any | null>(null);
	let isLoadingSchedule = $state(false);
	let selectedBookingDetail = $state<any | null>(null);
	let copiedEmbedCode = $state(false);

	// Start Session Modal
	let selectedLaneForSession = $state<LaneDto | null>(null);
	let sessionTitle = $state('Walk-in Throwers');
	let sessionDuration = $state(60);
	let sessionGameType = $state('watl_standard');
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
		} else if (activeTab === 'schedule') {
			await loadScheduleMatrix();
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

	async function loadScheduleMatrix() {
		if (!selectedVenue) return;
		isLoadingSchedule = true;
		try {
			const res = await fetch(`/api/admin/bookings/venue/${selectedVenue.id}/schedule-matrix?date=${scheduleDate}`, {
				credentials: 'include'
			});
			if (res.ok) {
				scheduleMatrix = await res.json();
			}
		} catch (e) {
			console.error(e);
		} finally {
			isLoadingSchedule = false;
		}
	}

	function changeScheduleDay(deltaDays: number) {
		const d = new Date(scheduleDate + 'T00:00:00');
		d.setDate(d.getDate() + deltaDays);
		scheduleDate = d.toISOString().split('T')[0];
		loadScheduleMatrix();
	}

	function jumpToToday() {
		scheduleDate = new Date().toISOString().split('T')[0];
		loadScheduleMatrix();
	}

	function copyEmbedCode() {
		const origin = typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173';
		const code = `<iframe id="venueaxe-booking" data-venueaxe-widget src="${origin}/book/${selectedVenue?.slug ?? 'downtown'}?embed=true" width="100%" frameborder="0" scrolling="no"></iframe>\n<script src="${origin}/venueaxe-widget.js" async><\/script>`;
		navigator.clipboard.writeText(code);
		copiedEmbedCode = true;
		setTimeout(() => (copiedEmbedCode = false), 2500);
	}

	function handleTabChange(tab: 'lanes' | 'schedule' | 'bookings' | 'waivers' | 'editor') {
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
					bookingId: null,
					gameTypeId: sessionGameType
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
			if (selectedBookingDetail && selectedBookingDetail.id === bookingId) {
				selectedBookingDetail = null;
			}
			await loadTabData();
			if (activeTab === 'schedule') {
				await loadScheduleMatrix();
			}
		} catch (e) {
			console.error(e);
		}
	}

	async function handleSaveConfig(e: SubmitEvent) {
		e.preventDefault();
		if (!selectedVenue || !bookingConfig) return;

		try {
			const res = await fetch(`/api/admin/booking-config/venue/${selectedVenue.id}`, {
				method: 'PUT',
				headers: { 'Content-Type': 'application/json' },
				credentials: 'include',
				body: JSON.stringify({
					minPartySize: Number(bookingConfig.minPartySize),
					maxPartySize: Number(bookingConfig.maxPartySize),
					slotDurationsMinutes: (bookingConfig.slotDurationsMinutes || []).map(Number),
					turnaroundBufferMinutes: Number(bookingConfig.turnaroundBufferMinutes),
					pricingModel: Number(bookingConfig.pricingModel),
					basePriceCents: Number(bookingConfig.basePriceCents),
					peakPriceCents: Number(bookingConfig.peakPriceCents),
					depositType: Number(bookingConfig.depositType),
					depositAmountCents: Number(bookingConfig.depositAmountCents),
					editorThemeJson: bookingConfig.editorThemeJson || '{}',
					customFieldsJson: bookingConfig.customFieldsJson || '[]',
					packagesJson: bookingConfig.packagesJson || '[]',
					discountRulesJson: (bookingConfig as any).discountRulesJson || '[]',
					bookingTypesJson: (bookingConfig as any).bookingTypesJson || '[]',
					addonsJson: (bookingConfig as any).addonsJson || '[]',
					cancellationPolicy: bookingConfig.cancellationPolicy || ''
				})
			});
			if (res.ok) {
				bookingConfig = await res.json();
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
			<button class="tab-btn" class:active={activeTab === 'schedule'} onclick={() => handleTabChange('schedule')}>
				📊 Lane Schedule Matrix
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

		<!-- 1.5. LANE SCHEDULE TIMELINE MATRIX TAB -->
		{:else if activeTab === 'schedule'}
			<div class="tab-header">
				<div>
					<h2 class="font-display">Daily Lane Schedule Matrix</h2>
					<p class="tab-subtitle">Interactive horizontal timeline showing physical lanes across operating hours with contiguous bay linking</p>
				</div>
				<div class="schedule-controls-row">
					<button class="btn btn-secondary btn-sm" onclick={() => changeScheduleDay(-1)}>
						&larr; Prev Day
					</button>
					<input
						type="date"
						class="form-input form-input-sm"
						style="width: 155px;"
						bind:value={scheduleDate}
						onchange={loadScheduleMatrix}
					/>
					<button class="btn btn-secondary btn-sm" onclick={jumpToToday}>
						Today
					</button>
					<button class="btn btn-secondary btn-sm" onclick={() => changeScheduleDay(1)}>
						Next Day &rarr;
					</button>
					<button class="btn btn-primary btn-sm font-display" onclick={loadScheduleMatrix} disabled={isLoadingSchedule}>
						{isLoadingSchedule ? 'Refreshing...' : '🔄 Refresh Matrix'}
					</button>
				</div>
			</div>

			{#if isLoadingSchedule}
				<div class="glass-panel" style="padding: 3rem; text-align: center;">
					<p class="text-secondary">Loading lane schedule matrix...</p>
				</div>
			{:else if scheduleMatrix}
				<div class="matrix-container glass-panel">
					<!-- Timeline Header (10 AM to 11 PM) -->
					<div class="matrix-header-row">
						<div class="matrix-lane-col-header font-display">TARGET BAY</div>
						<div class="matrix-timeline-header">
							{#each [10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23] as hour}
								<div class="time-col-header font-display">
									{hour > 12 ? `${hour - 12} PM` : (hour === 12 ? '12 PM' : `${hour} AM`)}
								</div>
							{/each}
						</div>
					</div>

					<!-- Lane Rows -->
					<div class="matrix-body">
						{#each (scheduleMatrix.lanes ?? []) as lane (lane.id)}
							<div class="matrix-lane-row">
								<div class="matrix-lane-cell">
									<strong class="font-display matrix-lane-name">{lane.name}</strong>
									<span class="matrix-lane-cap">Cap: {lane.maxThrowers}</span>
								</div>

								<div class="matrix-track">
									<!-- Hour slot guidelines -->
									{#each [10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23] as _}
										<div class="track-hour-slot"></div>
									{/each}

									<!-- Bookings mapped to this lane -->
									{#each (scheduleMatrix.bookings ?? []) as b}
										{#if (b.laneNumbers || []).includes(lane.laneNumber)}
											{@const s = new Date(b.startTime)}
											{@const e = new Date(b.endTime)}
											{@const startMin = (s.getUTCHours() - 10) * 60 + s.getUTCMinutes()}
											{@const durationMin = Math.max(30, (e.getTime() - s.getTime()) / (1000 * 60))}
											{@const leftPct = Math.max(0, (startMin / (14 * 60)) * 100)}
											{@const widthPct = Math.min(100 - leftPct, (durationMin / (14 * 60)) * 100)}

											<button
												type="button"
												class="booking-matrix-card"
												style="left: {leftPct}%; width: {widthPct}%;"
												class:multi-bay={(b.laneNumbers || []).length > 1}
												onclick={() => (selectedBookingDetail = b)}
											>
												<span class="booking-matrix-title font-display">{b.guestName}</span>
												<span class="booking-matrix-meta font-mono">
													{b.partySize}p • {b.bookingReference}
												</span>
												{#if (b.laneNumbers || []).length > 1}
													<span class="contiguous-badge font-display">Bays {b.laneNumbers.join('-')}</span>
												{/if}
											</button>
										{/if}
									{/each}
								</div>
							</div>
						{/each}
					</div>
				</div>
			{:else}
				<p class="text-secondary">No schedule data available.</p>
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
							<tr class:row-waivers-cleared={Number(b.signedWaiverCount) >= Number(b.partySize)}>
								<td><strong class="font-display text-amber">{b.bookingReference}</strong></td>
								<td>{b.guestFirstName} {b.guestLastName}<br /><small class="text-muted">{b.guestEmail}</small></td>
								<td>{b.partySize} Throwers</td>
								<td>{new Date(b.startTime).toLocaleString([], { dateStyle: 'short', timeStyle: 'short' })}</td>
								<td>Lanes {b.assignedLaneNumbers.join(', ')}</td>
								<td>
									{#if Number(b.signedWaiverCount) >= Number(b.partySize)}
										<span class="badge badge-waiver-complete font-display">
											✅ Fully Cleared ({b.signedWaiverCount}/{b.partySize})
										</span>
									{:else if Number(b.signedWaiverCount) > 0}
										<span class="badge badge-waiver-partial font-display">
											⚠️ Partial ({b.signedWaiverCount}/{b.partySize})
										</span>
									{:else}
										<span class="badge badge-waiver-missing font-display">
											❌ 0/{b.partySize} Signed
										</span>
									{/if}
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

				<div class="editor-section glass-panel">
					<h3 class="font-display">Discount & Promotion Rules (JSON)</h3>
					<p class="editor-hint">Configurable volume tiers (e.g. 10+ throwers), coupon codes (e.g. HERO10), and percentage / fixed cuts.</p>
					<textarea class="form-input font-mono" rows="7" bind:value={(bookingConfig as any).discountRulesJson}></textarea>
				</div>

				<div class="editor-section glass-panel">
					<h3 class="font-display">Booking Types & Operating Overrides (JSON)</h3>
					<p class="editor-hint">Special event types (e.g. Standard Throw, Corporate Party, League Night) with custom duration and off-hours bypass.</p>
					<textarea class="form-input font-mono" rows="7" bind:value={(bookingConfig as any).bookingTypesJson}></textarea>
				</div>

				<div class="editor-section glass-panel">
					<h3 class="font-display">Add-On Upgrades Catalog (JSON)</h3>
					<p class="editor-hint">Extra amenities available at checkout (drinks, coaching, championship trophies, merchandise).</p>
					<textarea class="form-input font-mono" rows="7" bind:value={(bookingConfig as any).addonsJson}></textarea>
				</div>

				<div class="editor-section glass-panel">
					<h3 class="font-display">Pre-Built Packages (JSON)</h3>
					<p class="editor-hint">Tiered packages combining bays, duration, and included perks.</p>
					<textarea class="form-input font-mono" rows="7" bind:value={bookingConfig.packagesJson}></textarea>
				</div>

				<!-- Embed Code Generator Box -->
				<div class="editor-section glass-panel embed-box" style="grid-column: 1 / -1;">
					<div class="embed-header">
						<div>
							<h3 class="font-display">🌐 Embeddable Booking Widget SDK</h3>
							<p class="editor-hint">Copy and paste this snippet into any external website (WordPress, Squarespace, Webflow, Shopify). The widget auto-resizes seamlessly without scrollbars.</p>
						</div>
						<button type="button" class="btn btn-secondary font-display" onclick={copyEmbedCode}>
							{copiedEmbedCode ? '✓ Copied to Clipboard!' : '📋 Copy Embed Snippet'}
						</button>
					</div>

					<pre class="embed-snippet-pre"><code>&lt;iframe id="venueaxe-booking" data-venueaxe-widget src="{typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173'}/book/{selectedVenue?.slug ?? 'downtown'}?embed=true" width="100%" frameborder="0" scrolling="no"&gt;&lt;/iframe&gt;
&lt;script src="{typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173'}/venueaxe-widget.js" async&gt;&lt;/script&gt;</code></pre>
				</div>

				<div style="grid-column: 1 / -1; display: flex; justify-content: flex-end;">
					<button type="submit" class="btn btn-primary font-display" style="padding: 0.8rem 2rem; font-size: 1rem;">
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
						<label class="form-label" for="session-game-type">Match Format & Target Rules</label>
						<select id="session-game-type" class="form-input" bind:value={sessionGameType}>
							<option value="watl_standard">WATL Standard (10 Throws, 6-Ring Target, Killshot 8 pts)</option>
							<option value="iatf_standard">IATF Standard (5 Throws, 3-Ring Target, Clutch 7 pts)</option>
							<option value="around_the_world">Around The World (Progress Rings 1-5, Bullseye, Clutch)</option>
							<option value="axe_tictactoe">Axe Tic-Tac-Toe (3x3 Target Territory Grid)</option>
							<option value="blackjack_21">Blackjack 21 (Target Exactly 21, Busts to 11)</option>
							<option value="countdown_301">Countdown 301 (Start 301, Target Exactly Zero)</option>
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

	<!-- SELECTED BOOKING DETAIL MODAL -->
	{#if selectedBookingDetail}
		<div class="modal-overlay" onclick={() => (selectedBookingDetail = null)}>
			<div class="modal-card glass-panel" onclick={(e) => e.stopPropagation()}>
				<div class="modal-header-row">
					<h3 class="modal-title font-display">Reservation Details</h3>
					<span class="ref-badge font-mono">{selectedBookingDetail.bookingReference}</span>
				</div>

				<div class="detail-grid">
					<div class="detail-item">
						<span class="detail-label">Guest Name</span>
						<strong>{selectedBookingDetail.guestName}</strong>
					</div>
					<div class="detail-item">
						<span class="detail-label">Guest Email</span>
						<span>{selectedBookingDetail.guestEmail}</span>
					</div>
					<div class="detail-item">
						<span class="detail-label">Party Size</span>
						<strong class="text-amber">{selectedBookingDetail.partySize} Throwers</strong>
					</div>
					<div class="detail-item">
						<span class="detail-label">Assigned Bays</span>
						<strong class="text-cyan">Bays {(selectedBookingDetail.laneNumbers || []).join(', ')}</strong>
					</div>
					<div class="detail-item">
						<span class="detail-label">Scheduled Slot</span>
						<span>
							{new Date(selectedBookingDetail.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} -
							{new Date(selectedBookingDetail.endTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
						</span>
					</div>
					<div class="detail-item">
						<span class="detail-label">Status</span>
						<span>
							{#if selectedBookingDetail.status === 0}
								<span class="badge badge-turnaround">Pending</span>
							{:else if selectedBookingDetail.status === 1}
								<span class="badge badge-available">Confirmed</span>
							{:else if selectedBookingDetail.status === 2}
								<span class="badge badge-active">Checked In</span>
							{:else if selectedBookingDetail.status === 3}
								<span class="badge badge-maintenance">Cancelled</span>
							{:else}
								<span class="badge">Status #{selectedBookingDetail.status}</span>
							{/if}
						</span>
					</div>
					<div class="detail-item">
						<span class="detail-label">Waiver Readiness</span>
						<span>
							{#if Number(selectedBookingDetail.signedWaiverCount || 0) >= Number(selectedBookingDetail.partySize)}
								<span class="badge badge-waiver-complete font-display">
									✅ Fully Cleared ({selectedBookingDetail.signedWaiverCount || 0}/{selectedBookingDetail.partySize})
								</span>
							{:else if Number(selectedBookingDetail.signedWaiverCount || 0) > 0}
								<span class="badge badge-waiver-partial font-display">
									⚠️ Partial ({selectedBookingDetail.signedWaiverCount || 0}/{selectedBookingDetail.partySize})
								</span>
							{:else}
								<span class="badge badge-waiver-missing font-display">
									❌ 0/{selectedBookingDetail.partySize} Signed
								</span>
							{/if}
						</span>
					</div>
				</div>

				<div class="pricing-summary-box">
					<div class="price-row">
						<span>Total Paid:</span>
						<strong class="text-amber font-display" style="font-size: 1.1rem;">
							${((selectedBookingDetail.totalPriceCents || 0) / 100).toFixed(2)}
						</strong>
					</div>
					{#if selectedBookingDetail.squarePaymentId}
						<div class="price-row" style="font-size: 0.75rem; color: var(--text-muted);">
							<span>Square Payment ID:</span>
							<span class="font-mono">{selectedBookingDetail.squarePaymentId}</span>
						</div>
					{/if}
				</div>

				<div class="modal-actions" style="margin-top: 1.5rem;">
					{#if selectedBookingDetail.status !== 2 && selectedBookingDetail.status !== 3}
						<button
							type="button"
							class="btn btn-primary font-display"
							onclick={() => handleUpdateBookingStatus(selectedBookingDetail.id, 2)}
						>
							✓ Mark Checked In
						</button>
					{/if}
					{#if selectedBookingDetail.status !== 3}
						<button
							type="button"
							class="btn btn-secondary btn-delete font-display"
							onclick={() => handleUpdateBookingStatus(selectedBookingDetail.id, 3)}
						>
							Cancel Reservation
						</button>
					{/if}
					<button
						type="button"
						class="btn btn-secondary"
						onclick={() => (selectedBookingDetail = null)}
					>
						Close
					</button>
				</div>
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

	/* Schedule Matrix Timeline Styles */
	.schedule-controls-row {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		flex-wrap: wrap;
	}

	.matrix-container {
		overflow-x: auto;
		border-radius: var(--radius-lg);
		padding: 1rem;
		background: rgba(15, 23, 42, 0.75);
		border: 1px solid var(--border-color);
	}

	.matrix-header-row {
		display: grid;
		grid-template-columns: 140px 1fr;
		border-bottom: 2px solid var(--border-color);
		padding-bottom: 0.5rem;
		margin-bottom: 0.5rem;
	}

	.matrix-lane-col-header {
		font-size: 0.75rem;
		color: var(--text-muted);
		letter-spacing: 0.08em;
		display: flex;
		align-items: center;
	}

	.matrix-timeline-header {
		display: grid;
		grid-template-columns: repeat(14, 1fr);
		text-align: center;
	}

	.time-col-header {
		font-size: 0.72rem;
		color: var(--text-secondary);
		border-left: 1px solid rgba(255, 255, 255, 0.07);
		padding: 0.2rem 0;
	}

	.matrix-body {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
		min-width: 900px;
	}

	.matrix-lane-row {
		display: grid;
		grid-template-columns: 140px 1fr;
		min-height: 52px;
		align-items: stretch;
		border-bottom: 1px solid rgba(255, 255, 255, 0.05);
		padding: 0.25rem 0;
	}

	.matrix-lane-cell {
		display: flex;
		flex-direction: column;
		justify-content: center;
		padding-right: 0.75rem;
	}

	.matrix-lane-name {
		font-size: 0.9rem;
		color: var(--text-primary);
	}

	.matrix-lane-cap {
		font-size: 0.7rem;
		color: var(--text-muted);
	}

	.matrix-track {
		position: relative;
		display: grid;
		grid-template-columns: repeat(14, 1fr);
		background: rgba(10, 15, 25, 0.6);
		border-radius: var(--radius-sm);
		overflow: hidden;
	}

	.track-hour-slot {
		border-left: 1px solid rgba(255, 255, 255, 0.05);
		height: 100%;
	}

	.booking-matrix-card {
		position: absolute;
		top: 4px;
		bottom: 4px;
		background: linear-gradient(135deg, rgba(245, 158, 11, 0.85), rgba(217, 119, 6, 0.95));
		border: 1px solid rgba(251, 191, 36, 0.7);
		border-radius: var(--radius-sm);
		padding: 0.2rem 0.5rem;
		color: #fff;
		text-align: left;
		cursor: pointer;
		overflow: hidden;
		display: flex;
		flex-direction: column;
		justify-content: center;
		z-index: 2;
		transition: transform 0.15s ease, box-shadow 0.15s ease;
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.4);
	}

	.booking-matrix-card:hover {
		transform: translateY(-1px);
		box-shadow: 0 4px 14px rgba(245, 158, 11, 0.4);
		z-index: 5;
	}

	.booking-matrix-card.multi-bay {
		background: linear-gradient(135deg, rgba(6, 182, 212, 0.85), rgba(14, 116, 144, 0.95));
		border-color: rgba(103, 232, 249, 0.7);
	}

	.booking-matrix-card.multi-bay:hover {
		box-shadow: 0 4px 14px rgba(6, 182, 212, 0.4);
	}

	.booking-matrix-title {
		font-size: 0.75rem;
		font-weight: 700;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		line-height: 1.1;
	}

	.booking-matrix-meta {
		font-size: 0.65rem;
		opacity: 0.9;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.contiguous-badge {
		position: absolute;
		right: 4px;
		top: 2px;
		font-size: 0.55rem;
		background: rgba(0, 0, 0, 0.4);
		padding: 0.05rem 0.3rem;
		border-radius: 3px;
		letter-spacing: 0.04em;
	}

	/* Embed snippet styling */
	.embed-header {
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		gap: 1rem;
		margin-bottom: 1rem;
	}

	.embed-snippet-pre {
		background: #090d14;
		border: 1px solid var(--border-color);
		border-radius: var(--radius-md);
		padding: 1rem;
		font-size: 0.8rem;
		color: #38bdf8;
		overflow-x: auto;
		white-space: pre-wrap;
		word-break: break-all;
	}

	.editor-hint {
		font-size: 0.8rem;
		color: var(--text-secondary);
		margin: 0.25rem 0 0.75rem 0;
	}

	/* Booking details modal styling */
	.modal-header-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 1.25rem;
	}

	.ref-badge {
		font-size: 0.8rem;
		background: rgba(245, 158, 11, 0.15);
		color: var(--accent-amber);
		padding: 0.2rem 0.5rem;
		border-radius: 4px;
	}

	.detail-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1rem;
		margin-bottom: 1.25rem;
	}

	.detail-item {
		display: flex;
		flex-direction: column;
		gap: 0.2rem;
	}

	.detail-label {
		font-size: 0.7rem;
		text-transform: uppercase;
		color: var(--text-muted);
		letter-spacing: 0.05em;
	}

	.detail-item span, .detail-item strong {
		font-size: 0.9rem;
	}

	.pricing-summary-box {
		background: #090d14;
		border: 1px solid var(--border-color);
		border-radius: var(--radius-md);
		padding: 0.85rem 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.4rem;
	}

	.price-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.text-cyan {
		color: #06b6d4;
	}

	.row-waivers-cleared {
		background: rgba(16, 185, 129, 0.04);
	}

	.row-waivers-cleared:hover {
		background: rgba(16, 185, 129, 0.08);
	}

	.badge-waiver-complete {
		background: rgba(16, 185, 129, 0.15);
		color: #10b981;
		border: 1px solid rgba(16, 185, 129, 0.4);
		font-size: 0.75rem;
		padding: 0.25rem 0.6rem;
		border-radius: var(--radius-sm);
	}

	.badge-waiver-partial {
		background: rgba(245, 158, 11, 0.15);
		color: #f59e0b;
		border: 1px solid rgba(245, 158, 11, 0.4);
		font-size: 0.75rem;
		padding: 0.25rem 0.6rem;
		border-radius: var(--radius-sm);
	}

	.badge-waiver-missing {
		background: rgba(239, 68, 68, 0.15);
		color: #ef4444;
		border: 1px solid rgba(239, 68, 68, 0.4);
		font-size: 0.75rem;
		padding: 0.25rem 0.6rem;
		border-radius: var(--radius-sm);
	}
</style>
