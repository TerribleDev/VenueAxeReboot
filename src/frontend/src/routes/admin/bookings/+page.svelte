<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { venueState } from '$lib/stores/venueState.svelte';
	import {
		getApiAdminBookingsVenueByVenueId,
		putApiAdminBookingsByIdStatus
	} from '$lib/api/client';
	import CreateBookingModal from '$lib/components/admin/CreateBookingModal.svelte';
	import { formatDateInTz, formatTimeInTz } from '$lib/utils/dateTime';
	import type { BookingDto } from '$lib/api/generated/types.gen';

	let bookings = $state<BookingDto[]>([]);
	let isLoading = $state(true);
	let bookingFilterDate = $state<string>(page.url.searchParams.get('date') || '');
	let bookingSearchTerm = $state<string>(page.url.searchParams.get('q') || '');

	// Reservation modal state
	let showCreateBookingModal = $state(false);
	let createModalLaneNumber = $state<number | null>(null);
	let createModalDate = $state<string>('');
	let createModalTime = $state<string>('');

	let selectedBookingDetail = $state<BookingDto | null>(null);

	async function loadBookings() {
		if (!venueState.selectedVenue) return;
		isLoading = true;
		try {
			const res = await getApiAdminBookingsVenueByVenueId({
				path: { venueId: venueState.selectedVenue.id },
				query: { date: bookingFilterDate || undefined }
			});
			bookings = res.data || [];
		} catch (e) {
			console.error('Failed to load bookings', e);
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		if (venueState.selectedVenue) {
			const _d = bookingFilterDate;
			loadBookings();
		}
	});

	$effect(() => {
		if (typeof window === 'undefined') return;
		const url = new URL(window.location.href);
		let changed = false;

		const currentQ = url.searchParams.get('q') || '';
		if (bookingSearchTerm.trim() !== currentQ) {
			if (bookingSearchTerm.trim()) {
				url.searchParams.set('q', bookingSearchTerm.trim());
			} else {
				url.searchParams.delete('q');
			}
			changed = true;
		}

		const currentDate = url.searchParams.get('date') || '';
		if ((bookingFilterDate || '') !== currentDate) {
			if (bookingFilterDate) {
				url.searchParams.set('date', bookingFilterDate);
			} else {
				url.searchParams.delete('date');
			}
			changed = true;
		}

		if (changed) {
			goto(url.toString(), { replaceState: true, keepFocus: true, noScroll: true });
		}
	});

	onMount(() => {
		loadBookings();

		const createParam = page.url.searchParams.get('create');
		if (createParam === '1') {
			const prefillLane = page.url.searchParams.get('lane');
			if (prefillLane) {
				createModalLaneNumber = Number(prefillLane);
			}
			const prefillDate = page.url.searchParams.get('date');
			if (prefillDate) {
				createModalDate = prefillDate;
			}
			const prefillTime = page.url.searchParams.get('time');
			if (prefillTime) {
				createModalTime = prefillTime;
			}
			showCreateBookingModal = true;
		}
	});

	// Filtered bookings
	const filteredBookings = $derived(
		bookings.filter((b) => {
			if (!bookingSearchTerm.trim()) return true;
			const term = bookingSearchTerm.toLowerCase();
			return (
				b.bookingReference.toLowerCase().includes(term) ||
				b.guestFirstName.toLowerCase().includes(term) ||
				b.guestLastName.toLowerCase().includes(term) ||
				b.guestEmail.toLowerCase().includes(term)
			);
		})
	);

	function openCreateBookingModal() {
		createModalLaneNumber = null;
		createModalDate = new Date().toISOString().split('T')[0];
		createModalTime = '17:00';
		showCreateBookingModal = true;
	}

	async function handleUpdateBookingStatus(bookingId: string, status: number) {
		try {
			await putApiAdminBookingsByIdStatus({
				path: { id: bookingId },
				query: { status }
			});
			selectedBookingDetail = null;
			await loadBookings();
		} catch (e) {
			console.error(e);
		}
	}

	function getBookingFinancials(b: { totalAmountCents?: number | string | null; paidAmountCents?: number | string | null }) {
		const total = Number(b.totalAmountCents) || 0;
		const paid = Number(b.paidAmountCents) || 0;
		const balDue = Math.max(0, total - paid);
		return { total, paid, balDue };
	}

	async function handleCollectBalance(b: BookingDto) {
		const { balDue: remaining } = getBookingFinancials(b);
		if (!confirm(`Collect remaining balance of $${(remaining / 100).toFixed(2)} for ${b.guestFirstName} ${b.guestLastName} (${b.bookingReference})?`)) {
			return;
		}
		try {
			const res = await fetch(`/api/admin/bookings/${b.id}/payment`, {
				method: 'PUT',
				headers: { 'Content-Type': 'application/json' },
				credentials: 'include',
				body: JSON.stringify({ amountCents: remaining, paymentMethod: 'Card' })
			});
			if (res.ok) {
				await loadBookings();
				if (selectedBookingDetail?.id === b.id) {
					selectedBookingDetail = await res.json();
				}
			} else {
				alert('Failed to record balance payment.');
			}
		} catch (e) {
			console.error('Failed to collect balance:', e);
		}
	}

	function getBookingStatusBadge(status: number) {
		switch (status) {
			case 0:
				return { label: 'Pending', class: 'badge-turnaround' };
			case 1:
				return { label: 'Confirmed', class: 'badge-available' };
			case 2:
				return { label: 'Checked In', class: 'badge-active' };
			case 3:
				return { label: 'Completed', class: 'badge-available' };
			case 4:
				return { label: 'Cancelled', class: 'badge-deactivated' };
			default:
				return { label: 'Confirmed', class: 'badge-available' };
		}
	}

	const bookingAnalytics = $derived.by(() => {
		let totalRevCents = 0;
		let totalPaidCents = 0;
		let totalBalCents = 0;
		let totalThrowers = 0;

		for (const b of filteredBookings) {
			const { total, paid, balDue } = getBookingFinancials(b);
			totalRevCents += total;
			totalPaidCents += paid;
			totalBalCents += balDue;
			totalThrowers += Number(b.partySize) || 0;
		}

		const avgPartySize = filteredBookings.length > 0 ? (totalThrowers / filteredBookings.length).toFixed(1) : '0';

		return {
			count: filteredBookings.length,
			totalRevenue: (totalRevCents / 100).toFixed(2),
			totalPaid: (totalPaidCents / 100).toFixed(2),
			totalBalanceDue: (totalBalCents / 100).toFixed(2),
			totalThrowers,
			avgPartySize
		};
	});

	function exportBookingsToCsv() {
		if (filteredBookings.length === 0) return;
		const headers = [
			'Booking Reference',
			'Guest First Name',
			'Guest Last Name',
			'Email',
			'Phone',
			'Party Size',
			'Start Time (UTC)',
			'Duration (min)',
			'Assigned Lanes',
			'Total Amount ($)',
			'Paid Amount ($)',
			'Balance Due ($)',
			'Status'
		];

		const rows = filteredBookings.map((b) => {
			const { total, paid, balDue } = getBookingFinancials(b);
			const st = getBookingStatusBadge(Number(b.status)).label;
			const lanes = (b.assignedLaneNumbers || []).map((n) => 'Lane ' + n).join('; ');
			return [
				`"${b.bookingReference}"`,
				`"${b.guestFirstName.replace(/"/g, '""')}"`,
				`"${b.guestLastName.replace(/"/g, '""')}"`,
				`"${b.guestEmail.replace(/"/g, '""')}"`,
				`"${(b.guestPhone || '').replace(/"/g, '""')}"`,
				b.partySize,
				`"${b.startTime}"`,
				`"${b.endTime}"`,
				`"${lanes}"`,
				(total / 100).toFixed(2),
				(paid / 100).toFixed(2),
				(balDue / 100).toFixed(2),
				`"${st}"`
			].join(',');
		});

		const csvContent = 'data:text/csv;charset=utf-8,' + [headers.join(','), ...rows].join('\n');
		const encodedUri = encodeURI(csvContent);
		const link = document.createElement('a');
		link.setAttribute('href', encodedUri);
		link.setAttribute('download', `venueaxe-bookings-${bookingFilterDate || 'all'}.csv`);
		document.body.appendChild(link);
		link.click();
		document.body.removeChild(link);
	}
</script>

<svelte:head>
	<title>Customer Reservations | VenueAxe Admin</title>
</svelte:head>

<div class="tab-header">
	<div>
		<h2 class="font-display">Customer Reservations</h2>
		<p class="tab-subtitle">Upcoming party bookings, capacity allocation, check-ins, and walk-ins</p>
	</div>
	<div style="display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap;">
		<button
			type="button"
			class="btn btn-secondary font-display"
			disabled={filteredBookings.length === 0}
			onclick={exportBookingsToCsv}
		>
			📥 Export CSV ({filteredBookings.length})
		</button>
		<a href="/admin/schedule" class="btn btn-secondary font-display">
			📊 Timeline View
		</a>
		<button
			type="button"
			class="btn btn-primary font-display"
			onclick={openCreateBookingModal}
		>
			+ New Reservation
		</button>
	</div>
</div>

<!-- Financial & Volume Analytics KPI Ribbon -->
<div class="kpi-ribbon" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 1rem; margin-bottom: 1.5rem;">
	<div class="glass-panel" style="padding: 1rem 1.25rem;">
		<div style="font-size: 0.75rem; color: var(--text-secondary); text-transform: uppercase; font-weight: 600; letter-spacing: 0.05em;">Total Bookings</div>
		<div class="font-display" style="font-size: 1.6rem; color: #f8fafc; margin-top: 0.25rem;">{bookingAnalytics.count}</div>
	</div>
	<div class="glass-panel" style="padding: 1rem 1.25rem;">
		<div style="font-size: 0.75rem; color: var(--text-secondary); text-transform: uppercase; font-weight: 600; letter-spacing: 0.05em;">Gross Revenue</div>
		<div class="font-display" style="font-size: 1.6rem; color: var(--accent-cyan); margin-top: 0.25rem;">${bookingAnalytics.totalRevenue}</div>
	</div>
	<div class="glass-panel" style="padding: 1rem 1.25rem;">
		<div style="font-size: 0.75rem; color: var(--text-secondary); text-transform: uppercase; font-weight: 600; letter-spacing: 0.05em;">Total Throwers</div>
		<div class="font-display" style="font-size: 1.6rem; color: var(--accent-amber); margin-top: 0.25rem;">{bookingAnalytics.totalThrowers} <span style="font-size: 0.8rem; color: var(--text-secondary); font-weight: normal;">(avg {bookingAnalytics.avgPartySize})</span></div>
	</div>
	<div class="glass-panel" style="padding: 1rem 1.25rem;">
		<div style="font-size: 0.75rem; color: var(--text-secondary); text-transform: uppercase; font-weight: 600; letter-spacing: 0.05em;">Balance Due</div>
		<div class="font-display" style="font-size: 1.6rem; color: {Number(bookingAnalytics.totalBalanceDue) > 0 ? '#ef4444' : '#10b981'}; margin-top: 0.25rem;">${bookingAnalytics.totalBalanceDue}</div>
	</div>
</div>

<!-- Filters Bar -->
<div class="glass-panel" style="padding: 1rem 1.25rem; margin-bottom: 1.5rem; display: flex; gap: 1rem; align-items: center; flex-wrap: wrap;">
	<div style="flex: 1; min-width: 240px;">
		<input
			type="text"
			class="form-input"
			style="width: 100%;"
			placeholder="🔍 Search by guest name, email, or reference code..."
			bind:value={bookingSearchTerm}
		/>
	</div>
	<div style="display: flex; align-items: center; gap: 0.5rem;">
		<label class="form-label" for="filter-date" style="margin: 0;">Date:</label>
		<input
			id="filter-date"
			type="date"
			class="form-input"
			bind:value={bookingFilterDate}
			onchange={loadBookings}
		/>
		{#if bookingFilterDate}
			<button
				type="button"
				class="btn btn-secondary btn-sm"
				onclick={() => {
					bookingFilterDate = '';
					loadBookings();
				}}
			>
				Clear
			</button>
		{/if}
	</div>
</div>

<!-- Bookings List Table -->
{#if isLoading}
	<div style="padding: 3rem; text-align: center; color: var(--text-secondary);">
		<p class="font-display">Loading reservations...</p>
	</div>
{:else if filteredBookings.length === 0}
	<div class="glass-panel" style="padding: 3rem; text-align: center; border-radius: var(--radius-lg);">
		<h3 class="font-display" style="font-size: 1.3rem; margin-bottom: 0.5rem;">No Reservations Found</h3>
		<p style="color: var(--text-secondary); margin-bottom: 1.5rem;">There are no bookings matching the selected date or search criteria.</p>
		<button class="btn btn-primary font-display" onclick={openCreateBookingModal}>
			+ Record a Reservation
		</button>
	</div>
{:else}
	<div class="glass-panel" style="overflow-x: auto; border-radius: var(--radius-lg);">
		<table style="width: 100%; border-collapse: collapse; text-align: left; font-size: 0.9rem;">
			<thead>
				<tr style="border-bottom: 1px solid var(--border-color); background: rgba(10, 15, 25, 0.4);">
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">REF CODE</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">GUEST NAME</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">DATE & TIME</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">PARTY</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">ASSIGNED LANES</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">STATUS</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">PAYMENT</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">WAIVERS</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem; text-align: right;">ACTIONS</th>
				</tr>
			</thead>
			<tbody>
				{#each filteredBookings as b (b.id)}
					{@const statusBadge = getBookingStatusBadge(b.status)}
					{@const fin = getBookingFinancials(b)}
					<tr style="border-bottom: 1px solid rgba(255, 255, 255, 0.05); transition: background 0.15s;" class="booking-row">
						<td style="padding: 0.85rem 1rem;">
							<strong class="font-mono" style="color: var(--accent-amber); font-weight: 800;">{b.bookingReference}</strong>
						</td>
						<td style="padding: 0.85rem 1rem;">
							<span class="font-display" style="font-weight: 700; display: block;">{b.guestFirstName} {b.guestLastName}</span>
							<span style="font-size: 0.78rem; color: var(--text-muted);">{b.guestEmail}</span>
						</td>
						<td style="padding: 0.85rem 1rem;">
							<span style="display: block;">{formatDateInTz(b.startTime, venueState.selectedVenue?.timezone)}</span>
							<span class="font-mono" style="font-size: 0.8rem; color: var(--text-secondary);">
								{formatTimeInTz(b.startTime, venueState.selectedVenue?.timezone)} - {formatTimeInTz(b.endTime, venueState.selectedVenue?.timezone)}
							</span>
						</td>
						<td style="padding: 0.85rem 1rem;">
							<span class="font-display" style="font-weight: 700;">{b.partySize}p</span>
						</td>
						<td style="padding: 0.85rem 1rem;">
							{#if b.assignedLaneNumbers && b.assignedLaneNumbers.length > 0}
								<span class="badge badge-available font-display">
									Lanes {b.assignedLaneNumbers.join(', ')}
								</span>
							{:else}
								<span style="color: var(--text-muted); font-size: 0.82rem;">Unassigned</span>
							{/if}
						</td>
						<td style="padding: 0.85rem 1rem;">
							<span class="badge {statusBadge.class} font-display">{statusBadge.label}</span>
						</td>
						<td style="padding: 0.85rem 1rem;">
							{#if b.paymentStatus === 'DepositPaid' || (fin.balDue > 0 && fin.paid > 0)}
								<div class="deposit-due-badge font-display" title="Deposit paid. Balance due at arrival.">
									<span>🟡</span> DEPOSIT PAID — ${(fin.balDue / 100).toFixed(2)} DUE
								</div>
								<button
									type="button"
									class="btn btn-xs btn-collect-bal font-display"
									onclick={() => handleCollectBalance(b)}
								>
									Collect ${(fin.balDue / 100).toFixed(2)}
								</button>
							{:else if b.paymentStatus === 'PaidInFull' || fin.balDue === 0}
								<span class="badge badge-paid-full font-display">✓ PAID (${(fin.paid / 100).toFixed(2)})</span>
							{:else}
								<div style="display: flex; flex-direction: column; gap: 0.25rem;">
									<span class="badge badge-unpaid font-display">UNPAID (${(fin.total / 100).toFixed(2)})</span>
									<button
										type="button"
										class="btn btn-xs btn-collect-bal font-display"
										onclick={() => handleCollectBalance(b)}
									>
										Collect Balance
									</button>
								</div>
							{/if}
						</td>
						<td style="padding: 0.85rem 1rem;">
							<span style="font-size: 0.85rem; font-weight: 700; color: {b.signedWaiverCount >= b.partySize ? '#10b981' : 'var(--accent-amber)'};">
								{b.signedWaiverCount} / {b.partySize}
							</span>
						</td>
						<td style="padding: 0.85rem 1rem; text-align: right;">
							<button
								type="button"
								class="btn btn-secondary btn-xs font-display"
								onclick={() => (selectedBookingDetail = b)}
							>
								View Details
							</button>
						</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
{/if}

<!-- WALK-IN / RESERVATION MODAL (DELIV-4.1) -->
<CreateBookingModal
	isOpen={showCreateBookingModal}
	prefillLaneNumber={createModalLaneNumber}
	prefillDate={createModalDate}
	prefillStartTime={createModalTime}
	onClose={() => {
		showCreateBookingModal = false;
	}}
	onSuccess={async () => {
		showCreateBookingModal = false;
		await loadBookings();
	}}
/>

<!-- BOOKING DETAIL MODAL -->
{#if selectedBookingDetail}
	{@const modalFin = getBookingFinancials(selectedBookingDetail)}
	<div class="modal-overlay" role="button" tabindex="0" onclick={() => (selectedBookingDetail = null)} onkeydown={(e) => { if (e.key === 'Escape') selectedBookingDetail = null; }}>
		<div class="modal-card glass-panel" style="max-width: 500px;" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.stopPropagation()}>
			<div class="modal-header-row">
				<div>
					<h3 class="modal-title font-display">Booking {selectedBookingDetail.bookingReference}</h3>
					<p class="editor-hint" style="margin-bottom: 0;">{selectedBookingDetail.guestFirstName} {selectedBookingDetail.guestLastName}</p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (selectedBookingDetail = null)}>✕</button>
			</div>

			<div style="margin-top: 1rem; display: flex; flex-direction: column; gap: 0.75rem;">
				<div class="form-row-2">
					<div>
						<span class="form-label" style="display: block;">Email</span>
						<span>{selectedBookingDetail.guestEmail}</span>
					</div>
					<div>
						<span class="form-label" style="display: block;">Phone</span>
						<span>{selectedBookingDetail.guestPhone}</span>
					</div>
				</div>

				<div class="form-row-2">
					<div>
						<span class="form-label" style="display: block;">Party Size</span>
						<strong class="font-display">{selectedBookingDetail.partySize} Throwers</strong>
					</div>
					<div>
						<span class="form-label" style="display: block;">Assigned Lanes</span>
						<span class="badge badge-available font-display">
							{selectedBookingDetail.assignedLaneNumbers && selectedBookingDetail.assignedLaneNumbers.length > 0 ? `Lanes ${selectedBookingDetail.assignedLaneNumbers.join(', ')}` : 'Unassigned'}
						</span>
					</div>
				</div>

				<div class="form-row-2">
					<div>
						<span class="form-label" style="display: block;">Financial Status & Balance</span>
						<div style="margin-top: 0.25rem;">
							{#if modalFin.balDue > 0}
								<div class="deposit-due-badge font-display" style="margin-bottom: 0.4rem;">
									🟡 ${(modalFin.balDue / 100).toFixed(2)} DUE (Paid: ${(modalFin.paid / 100).toFixed(2)})
								</div>
								<button
									type="button"
									class="btn btn-xs btn-collect-bal font-display"
									onclick={() => handleCollectBalance(selectedBookingDetail!)}
								>
									Collect ${(modalFin.balDue / 100).toFixed(2)} Remaining
								</button>
							{:else}
								<span class="badge badge-paid-full font-display">
									✓ Paid In Full (${(modalFin.paid / 100).toFixed(2)})
								</span>
							{/if}
						</div>
					</div>
					<div>
						<span class="form-label" style="display: block;">Signed Waivers</span>
						<strong style="color: {selectedBookingDetail.signedWaiverCount >= selectedBookingDetail.partySize ? '#10b981' : 'var(--accent-amber)'}; font-size: 1.1rem; display: block; margin-top: 0.25rem;">
							{selectedBookingDetail.signedWaiverCount} / {selectedBookingDetail.partySize} Signed
						</strong>
					</div>
				</div>
			</div>

			<div class="modal-actions" style="margin-top: 1.5rem;">
				{#if selectedBookingDetail.status !== 2}
					<button
						type="button"
						class="btn btn-primary font-display"
						onclick={() => handleUpdateBookingStatus(selectedBookingDetail!.id, 2)}
					>
						Check In Party
					</button>
				{/if}
				{#if selectedBookingDetail.status !== 4}
					<button
						type="button"
						class="btn btn-secondary btn-delete font-display"
						onclick={() => handleUpdateBookingStatus(selectedBookingDetail!.id, 4)}
					>
						Cancel Reservation
					</button>
				{/if}
				<button type="button" class="btn btn-secondary" onclick={() => (selectedBookingDetail = null)}>
					Close
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	.deposit-due-badge {
		display: inline-flex;
		align-items: center;
		gap: 0.35rem;
		background: rgba(245, 158, 11, 0.18);
		border: 1px solid var(--accent-amber);
		color: var(--accent-amber);
		padding: 0.25rem 0.6rem;
		border-radius: var(--radius-sm);
		font-size: 0.78rem;
		font-weight: 800;
		letter-spacing: 0.03em;
		white-space: nowrap;
	}

	.btn-collect-bal {
		background: var(--accent-amber);
		color: #000;
		font-weight: 800;
		border: none;
		padding: 0.2rem 0.55rem;
		border-radius: 4px;
		cursor: pointer;
		margin-top: 0.25rem;
		transition: all 0.15s ease;
	}

	.btn-collect-bal:hover {
		filter: brightness(1.15);
		box-shadow: 0 0 10px rgba(245, 158, 11, 0.4);
	}

	.badge-paid-full {
		background: rgba(16, 185, 129, 0.18);
		border: 1px solid #10b981;
		color: #10b981;
		padding: 0.25rem 0.6rem;
		border-radius: var(--radius-sm);
		font-size: 0.78rem;
		font-weight: 800;
	}

	.badge-unpaid {
		background: rgba(239, 68, 68, 0.18);
		border: 1px solid var(--accent-crimson, #ef4444);
		color: #fca5a5;
		padding: 0.25rem 0.6rem;
		border-radius: var(--radius-sm);
		font-size: 0.78rem;
		font-weight: 800;
	}
</style>
