<script lang="ts">
	import { onMount } from 'svelte';
	import { venueState } from '$lib/stores/venueState.svelte';
	import { getTodayDateString } from '$lib/utils/dateTime';

	let activeReportTab = $state<'daily' | 'weekly' | 'range' | 'lanes'>('daily');
	let isLoading = $state(true);
	let errorMessage = $state<string | null>(null);

	// Daily Report State
	let dailyDate = $state(getTodayDateString());
	let dailyData = $state<any>(null);

	// Weekly Report State
	let weeklyStartDate = $state(getMondayOfCurrentWeek());
	let weeklyData = $state<any>(null);

	// Date Range Report State
	let rangeStartDate = $state(getDateDaysAgo(30));
	let rangeEndDate = $state(getTodayDateString());
	let rangeData = $state<any>(null);

	function getMondayOfCurrentWeek(): string {
		const d = new Date();
		const day = d.getDay();
		const diff = d.getDate() - day + (day === 0 ? -6 : 1);
		const monday = new Date(d.setDate(diff));
		return monday.toISOString().split('T')[0];
	}

	function getDateDaysAgo(days: number): string {
		const d = new Date();
		d.setDate(d.getDate() - days);
		return d.toISOString().split('T')[0];
	}

	function shiftDailyDate(days: number) {
		const [y, m, d] = dailyDate.split('-').map(Number);
		const current = new Date(y, m - 1, d);
		current.setDate(current.getDate() + days);
		dailyDate = current.toISOString().split('T')[0];
		loadDailyReport();
	}

	function shiftWeeklyDate(weeks: number) {
		const [y, m, d] = weeklyStartDate.split('-').map(Number);
		const current = new Date(y, m - 1, d);
		current.setDate(current.getDate() + weeks * 7);
		weeklyStartDate = current.toISOString().split('T')[0];
		loadWeeklyReport();
	}

	function applyPreset(preset: '7d' | '30d' | 'thisMonth' | 'lastMonth') {
		const today = new Date();
		if (preset === '7d') {
			rangeStartDate = getDateDaysAgo(7);
			rangeEndDate = getTodayDateString();
		} else if (preset === '30d') {
			rangeStartDate = getDateDaysAgo(30);
			rangeEndDate = getTodayDateString();
		} else if (preset === 'thisMonth') {
			const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
			rangeStartDate = firstDay.toISOString().split('T')[0];
			rangeEndDate = getTodayDateString();
		} else if (preset === 'lastMonth') {
			const firstDay = new Date(today.getFullYear(), today.getMonth() - 1, 1);
			const lastDay = new Date(today.getFullYear(), today.getMonth(), 0);
			rangeStartDate = firstDay.toISOString().split('T')[0];
			rangeEndDate = lastDay.toISOString().split('T')[0];
		}
		loadRangeReport();
	}

	async function loadDailyReport() {
		if (!venueState.selectedVenue) return;
		isLoading = true;
		errorMessage = null;
		try {
			const res = await fetch(`/api/admin/reports/venue/${venueState.selectedVenue.id}/daily?date=${dailyDate}`, {
				headers: { 'Accept': 'application/json' }
			});
			if (!res.ok) throw new Error('Failed to load daily report');
			dailyData = await res.json();
		} catch (err: any) {
			errorMessage = err?.message || 'Error loading daily report';
		} finally {
			isLoading = false;
		}
	}

	async function loadWeeklyReport() {
		if (!venueState.selectedVenue) return;
		isLoading = true;
		errorMessage = null;
		try {
			const res = await fetch(`/api/admin/reports/venue/${venueState.selectedVenue.id}/weekly?weekStart=${weeklyStartDate}`, {
				headers: { 'Accept': 'application/json' }
			});
			if (!res.ok) throw new Error('Failed to load weekly report');
			weeklyData = await res.json();
		} catch (err: any) {
			errorMessage = err?.message || 'Error loading weekly report';
		} finally {
			isLoading = false;
		}
	}

	async function loadRangeReport() {
		if (!venueState.selectedVenue) return;
		isLoading = true;
		errorMessage = null;
		try {
			const res = await fetch(`/api/admin/reports/venue/${venueState.selectedVenue.id}/range?startDate=${rangeStartDate}&endDate=${rangeEndDate}`, {
				headers: { 'Accept': 'application/json' }
			});
			if (!res.ok) throw new Error('Failed to load date range report');
			rangeData = await res.json();
		} catch (err: any) {
			errorMessage = err?.message || 'Error loading range report';
		} finally {
			isLoading = false;
		}
	}

	function handleTabChange(tab: 'daily' | 'weekly' | 'range' | 'lanes') {
		activeReportTab = tab;
		if (tab === 'daily') loadDailyReport();
		else if (tab === 'weekly') loadWeeklyReport();
		else if (tab === 'range' || tab === 'lanes') loadRangeReport();
	}

	function downloadCsv(type: 'daily' | 'weekly' | 'range') {
		if (!venueState.selectedVenue) return;
		let url = `/api/admin/reports/venue/${venueState.selectedVenue.id}/export?reportType=${type}`;
		if (type === 'daily') {
			url += `&startDate=${dailyDate}&endDate=${dailyDate}`;
		} else if (type === 'weekly') {
			const [y, m, d] = weeklyStartDate.split('-').map(Number);
			const end = new Date(y, m - 1, d);
			end.setDate(end.getDate() + 6);
			url += `&startDate=${weeklyStartDate}&endDate=${end.toISOString().split('T')[0]}`;
		} else {
			url += `&startDate=${rangeStartDate}&endDate=${rangeEndDate}`;
		}
		window.open(url, '_blank');
	}

	onMount(() => {
		loadDailyReport();
	});

	$effect(() => {
		if (venueState.selectedVenue) {
			if (activeReportTab === 'daily') loadDailyReport();
			else if (activeReportTab === 'weekly') loadWeeklyReport();
			else loadRangeReport();
		}
	});
</script>

<svelte:head>
	<title>VenueAxe - Reports & Analytics</title>
</svelte:head>

<div class="reports-page">
	<!-- Page Header -->
	<div class="reports-header-row">
		<div>
			<h1 class="font-display reports-title">📈 Reports & Revenue Analytics</h1>
			<p class="reports-subtitle">
				Financial performance, booking volumes, lane utilization, and game mode telemetry for {venueState.selectedVenue?.name ?? 'Venue'}.
			</p>
		</div>

		<div class="reports-actions">
			{#if activeReportTab === 'daily'}
				<button type="button" class="btn btn-secondary font-display btn-export" onclick={() => downloadCsv('daily')}>
					📥 Export Day CSV
				</button>
			{:else if activeReportTab === 'weekly'}
				<button type="button" class="btn btn-secondary font-display btn-export" onclick={() => downloadCsv('weekly')}>
					📥 Export Week CSV
				</button>
			{:else}
				<button type="button" class="btn btn-secondary font-display btn-export" onclick={() => downloadCsv('range')}>
					📥 Export Range CSV
				</button>
			{/if}
		</div>
	</div>

	<!-- Sub-Navigation Tabs -->
	<div class="reports-tabs-bar">
		<button
			type="button"
			class="subtab-btn font-display"
			class:active={activeReportTab === 'daily'}
			onclick={() => handleTabChange('daily')}
		>
			📅 Daily Report
		</button>
		<button
			type="button"
			class="subtab-btn font-display"
			class:active={activeReportTab === 'weekly'}
			onclick={() => handleTabChange('weekly')}
		>
			📆 Weekly Report
		</button>
		<button
			type="button"
			class="subtab-btn font-display"
			class:active={activeReportTab === 'range'}
			onclick={() => handleTabChange('range')}
		>
			📜 Past & Date Range
		</button>
		<button
			type="button"
			class="subtab-btn font-display"
			class:active={activeReportTab === 'lanes'}
			onclick={() => handleTabChange('lanes')}
		>
			🏟️ Lane & Game Analytics
		</button>
	</div>

	{#if errorMessage}
		<div class="alert-error" style="margin-bottom: 1.5rem;">
			⚠️ {errorMessage}
		</div>
	{/if}

	<!-- TAB 1: DAILY REPORT -->
	{#if activeReportTab === 'daily'}
		<div class="report-section">
			<!-- Date Picker Ribbon -->
			<div class="control-ribbon glass-panel">
				<div class="date-navigator">
					<button type="button" class="btn btn-secondary btn-sm" onclick={() => shiftDailyDate(-1)}>
						◀ Previous Day
					</button>
					<input
						type="date"
						class="form-input date-input font-display"
						bind:value={dailyDate}
						onchange={loadDailyReport}
					/>
					<button type="button" class="btn btn-secondary btn-sm" onclick={() => shiftDailyDate(1)}>
						Next Day ▶
					</button>
					<button type="button" class="btn btn-outline btn-sm font-display" onclick={() => { dailyDate = getTodayDateString(); loadDailyReport(); }}>
						Today
					</button>
				</div>
				<span class="report-date-badge font-display">
					{dailyDate}
				</span>
			</div>

			{#if isLoading}
				<div class="loading-box glass-panel font-display">
					<div class="spinner"></div>
					Generating daily report...
				</div>
			{:else if dailyData}
				<!-- Key Metrics Cards -->
				<div class="kpi-grid">
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Gross Revenue</span>
						<span class="kpi-val text-green font-display">
							${(dailyData.grossRevenueCents / 100).toFixed(2)}
						</span>
						<span class="kpi-sub">Total billed for date</span>
					</div>
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Total Bookings</span>
						<span class="kpi-val text-cyan font-display">
							{dailyData.totalBookings}
						</span>
						<span class="kpi-sub">{dailyData.completedSessions} completed</span>
					</div>
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Total Throwers</span>
						<span class="kpi-val text-amber font-display">
							{dailyData.totalThrowers}
						</span>
						<span class="kpi-sub">Guests through venue</span>
					</div>
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Deposits Collected</span>
						<span class="kpi-val font-display" style="color: #60a5fa;">
							${(dailyData.depositsCollectedCents / 100).toFixed(2)}
						</span>
						<span class="kpi-sub">Remaining balance: ${(dailyData.balanceDueCents / 100).toFixed(2)}</span>
					</div>
				</div>

				<!-- Hourly Traffic Bar Breakdown -->
				<div class="glass-panel content-card">
					<h3 class="font-display section-title">⏱️ Hourly Booking Distribution</h3>
					<div class="hourly-chart">
						{#each dailyData.hourlyDistribution as hour}
							<div class="hour-bar-col">
								<div class="hour-bar-track">
									<div
										class="hour-bar-fill"
										style="height: {Math.min(100, (hour.throwersCount / Math.max(1, dailyData.totalThrowers || 1)) * 300)}%;"
									></div>
								</div>
								<span class="hour-val">{hour.throwersCount}</span>
								<span class="hour-label">{hour.timeLabel.split(' ')[0]}</span>
							</div>
						{/each}
					</div>
				</div>

				<!-- Bookings Table -->
				<div class="glass-panel content-card">
					<h3 class="font-display section-title">📋 Scheduled Reservations ({dailyData.bookings.length})</h3>
					{#if dailyData.bookings.length === 0}
						<div class="empty-notice">No bookings recorded for this date.</div>
					{:else}
						<div class="table-container">
							<table class="data-table">
								<thead>
									<tr>
										<th>Ref</th>
										<th>Guest Name</th>
										<th>Bays</th>
										<th>Time</th>
										<th>Party</th>
										<th>Total</th>
										<th>Paid</th>
										<th>Status</th>
										<th>Waivers</th>
									</tr>
								</thead>
								<tbody>
									{#each dailyData.bookings as b}
										<tr>
											<td class="font-mono text-cyan" style="font-weight: 700;">#{b.reference}</td>
											<td style="font-weight: 600;">{b.customerName}</td>
											<td><span class="badge-lane">{b.laneNumbers}</span></td>
											<td class="font-mono text-muted">{new Date(b.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</td>
											<td>{b.partySize}</td>
											<td style="font-weight: 700;">${(b.totalCents / 100).toFixed(2)}</td>
											<td class="text-green">${(b.paidCents / 100).toFixed(2)}</td>
											<td><span class="status-pill status-{b.status.toLowerCase()}">{b.status}</span></td>
											<td><span class="waiver-pill" class:complete={b.signedWaivers >= b.partySize}>{b.signedWaivers}/{b.partySize}</span></td>
										</tr>
									{/each}
								</tbody>
							</table>
						</div>
					{/if}
				</div>
			{/if}
		</div>
	{/if}

	<!-- TAB 2: WEEKLY REPORT -->
	{#if activeReportTab === 'weekly'}
		<div class="report-section">
			<!-- Week Navigator -->
			<div class="control-ribbon glass-panel">
				<div class="date-navigator">
					<button type="button" class="btn btn-secondary btn-sm" onclick={() => shiftWeeklyDate(-1)}>
						◀ Previous Week
					</button>
					<span class="font-display week-range-text">
						Week of {weeklyStartDate}
					</span>
					<button type="button" class="btn btn-secondary btn-sm" onclick={() => shiftWeeklyDate(1)}>
						Next Week ▶
					</button>
					<button type="button" class="btn btn-outline btn-sm font-display" onclick={() => { weeklyStartDate = getMondayOfCurrentWeek(); loadWeeklyReport(); }}>
						This Week
					</button>
				</div>
				{#if weeklyData}
					<span class="report-date-badge font-display">
						{weeklyData.weekStartDate} — {weeklyData.weekEndDate}
					</span>
				{/if}
			</div>

			{#if isLoading}
				<div class="loading-box glass-panel font-display">
					<div class="spinner"></div>
					Generating weekly report...
				</div>
			{:else if weeklyData}
				<!-- Weekly KPI Cards -->
				<div class="kpi-grid">
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Week Gross Revenue</span>
						<span class="kpi-val text-green font-display">
							${(weeklyData.grossRevenueCents / 100).toFixed(2)}
						</span>
						<span class="kpi-sub">Monday through Sunday</span>
					</div>
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Week Total Bookings</span>
						<span class="kpi-val text-cyan font-display">
							{weeklyData.totalBookings}
						</span>
						<span class="kpi-sub">Total reservations</span>
					</div>
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Week Throwers</span>
						<span class="kpi-val text-amber font-display">
							{weeklyData.totalThrowers}
						</span>
						<span class="kpi-sub">Total participants</span>
					</div>
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Avg Daily Revenue</span>
						<span class="kpi-val font-display" style="color: #a78bfa;">
							${(weeklyData.grossRevenueCents / 700).toFixed(2)}
						</span>
						<span class="kpi-sub">Per day average</span>
					</div>
				</div>

				<!-- 7-Day Day-by-Day Cards -->
				<div class="glass-panel content-card">
					<h3 class="font-display section-title">📅 Daily Revenue & Volume Breakdown</h3>
					<div class="week-days-grid">
						{#each weeklyData.dailyBreakdown as day}
							<div class="day-card" class:has-rev={day.revenueCents > 0}>
								<div class="day-card-header">
									<span class="day-name font-display">{day.dayOfWeek}</span>
									<span class="day-date font-mono">{day.date.split('-').slice(1).join('/')}</span>
								</div>
								<div class="day-card-rev font-display">
									${(day.revenueCents / 100).toFixed(2)}
								</div>
								<div class="day-card-stats">
									<span>{day.bookingsCount} bookings</span>
									<span>{day.throwersCount} throwers</span>
								</div>
							</div>
						{/each}
					</div>
				</div>

				<!-- Peak Hours of Week -->
				<div class="glass-panel content-card">
					<h3 class="font-display section-title">🔥 Peak Operating Hours This Week</h3>
					<div class="peak-hours-grid">
						{#each weeklyData.peakHours as ph, idx}
							<div class="peak-hour-chip">
								<span class="peak-rank">#{idx + 1}</span>
								<div class="peak-hour-info">
									<span class="peak-time font-display">{ph.timeLabel}</span>
									<span class="peak-stats text-muted">{ph.totalBookings} bookings • {ph.totalThrowers} throwers</span>
								</div>
							</div>
						{/each}
					</div>
				</div>
			{/if}
		</div>
	{/if}

	<!-- TAB 3: PAST & DATE RANGE REPORTS -->
	{#if activeReportTab === 'range'}
		<div class="report-section">
			<!-- Range Controls -->
			<div class="control-ribbon glass-panel">
				<div class="range-controls-row">
					<div class="date-range-inputs">
						<div class="input-group">
							<label class="form-label" for="range-start">From</label>
							<input id="range-start" type="date" class="form-input date-input" bind:value={rangeStartDate} onchange={loadRangeReport} />
						</div>
						<div class="input-group">
							<label class="form-label" for="range-end">To</label>
							<input id="range-end" type="date" class="form-input date-input" bind:value={rangeEndDate} onchange={loadRangeReport} />
						</div>
					</div>

					<div class="presets-row">
						<button type="button" class="preset-chip" onclick={() => applyPreset('7d')}>Last 7D</button>
						<button type="button" class="preset-chip" onclick={() => applyPreset('30d')}>Last 30D</button>
						<button type="button" class="preset-chip" onclick={() => applyPreset('thisMonth')}>This Month</button>
						<button type="button" class="preset-chip" onclick={() => applyPreset('lastMonth')}>Last Month</button>
					</div>
				</div>
			</div>

			{#if isLoading}
				<div class="loading-box glass-panel font-display">
					<div class="spinner"></div>
					Generating date range report...
				</div>
			{:else if rangeData}
				<!-- Range KPI Cards -->
				<div class="kpi-grid">
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Total Range Revenue</span>
						<span class="kpi-val text-green font-display">
							${(rangeData.grossRevenueCents / 100).toFixed(2)}
						</span>
						<span class="kpi-sub">Collected: ${(rangeData.depositsCollectedCents / 100).toFixed(2)}</span>
					</div>
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Total Bookings</span>
						<span class="kpi-val text-cyan font-display">
							{rangeData.totalBookings}
						</span>
						<span class="kpi-sub">Total throwers: {rangeData.totalThrowers}</span>
					</div>
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Average Booking Value</span>
						<span class="kpi-val text-amber font-display">
							${(rangeData.averageBookingValueCents / 100).toFixed(2)}
						</span>
						<span class="kpi-sub">Per reservation average</span>
					</div>
					<div class="kpi-card glass-panel">
						<span class="kpi-label">Waiver Completion Rate</span>
						<span class="kpi-val font-display" style="color: #34d399;">
							{rangeData.waiverCompletionRatePercent}%
						</span>
						<span class="kpi-sub">Legal compliance score</span>
					</div>
				</div>

				<!-- Daily Trends Table -->
				<div class="glass-panel content-card">
					<h3 class="font-display section-title">📈 Historical Daily Timeline</h3>
					<div class="table-container">
						<table class="data-table">
							<thead>
								<tr>
									<th>Date</th>
									<th>Day</th>
									<th>Bookings</th>
									<th>Throwers</th>
									<th>Gross Revenue</th>
								</tr>
							</thead>
							<tbody>
								{#each rangeData.dailyTrends as day}
									<tr>
										<td class="font-mono">{day.date}</td>
										<td style="font-weight: 600;">{day.dayOfWeek}</td>
										<td>{day.bookingsCount}</td>
										<td>{day.throwersCount}</td>
										<td style="font-weight: 700; color: #34d399;">${(day.revenueCents / 100).toFixed(2)}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				</div>
			{/if}
		</div>
	{/if}

	<!-- TAB 4: LANE & GAME ANALYTICS -->
	{#if activeReportTab === 'lanes'}
		<div class="report-section">
			{#if isLoading}
				<div class="loading-box glass-panel font-display">
					<div class="spinner"></div>
					Generating lane analytics...
				</div>
			{:else if rangeData}
				<!-- Lane Performance Table -->
				<div class="glass-panel content-card">
					<h3 class="font-display section-title">🏟️ Physical Bay Utilization Matrix</h3>
					<div class="table-container">
						<table class="data-table">
							<thead>
								<tr>
									<th>Bay</th>
									<th>Total Sessions</th>
									<th>Hours Booked</th>
									<th>Revenue Generated</th>
									<th>Bay Utilization %</th>
								</tr>
							</thead>
							<tbody>
								{#each rangeData.lanePerformance as lane}
									<tr>
										<td style="font-weight: 700; color: var(--accent-amber);">{lane.laneName}</td>
										<td>{lane.sessionsCount} sessions</td>
										<td>{lane.hoursBooked} hrs</td>
										<td style="font-weight: 700; color: #34d399;">${(lane.revenueCents / 100).toFixed(2)}</td>
										<td>
											<div style="display: flex; align-items: center; gap: 0.5rem;">
												<div class="progress-bar-track">
													<div class="progress-bar-fill" style="width: {Math.min(100, lane.utilizationPercent)}%;"></div>
												</div>
												<span class="font-mono text-cyan" style="font-weight: 700;">{lane.utilizationPercent}%</span>
											</div>
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				</div>

				<!-- Game Popularity Grid -->
				<div class="glass-panel content-card">
					<h3 class="font-display section-title">🎮 Target Game Modes Popularity</h3>
					<div class="games-popularity-grid">
						{#each rangeData.gamePopularity as gm}
							<div class="game-pop-card">
								<h4 class="font-display game-pop-title">{gm.gameName}</h4>
								<div class="game-pop-stats">
									<div class="pop-stat">
										<span class="pop-stat-val text-cyan font-display">{gm.matchesCount}</span>
										<span class="pop-stat-label">Matches</span>
									</div>
									<div class="pop-stat">
										<span class="pop-stat-val text-amber font-display">{gm.throwsCount}</span>
										<span class="pop-stat-label">Throws</span>
									</div>
								</div>
							</div>
						{/each}
					</div>
				</div>
			{/if}
		</div>
	{/if}
</div>

<style>
	.reports-page {
		max-width: 1400px;
		margin: 0 auto;
		padding: 1.5rem 2rem 4rem;
	}

	.reports-header-row {
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		margin-bottom: 1.5rem;
		flex-wrap: wrap;
		gap: 1rem;
	}

	.reports-title {
		font-size: 2rem;
		color: #f8fafc;
		margin: 0 0 0.25rem 0;
	}

	.reports-subtitle {
		color: var(--text-secondary);
		margin: 0;
		font-size: 0.92rem;
	}

	.reports-tabs-bar {
		display: flex;
		gap: 0.5rem;
		margin-bottom: 1.75rem;
		border-bottom: 1px solid var(--border-color);
		padding-bottom: 0.75rem;
		flex-wrap: wrap;
	}

	.subtab-btn {
		background: transparent;
		border: 1px solid transparent;
		color: var(--text-secondary);
		padding: 0.5rem 1rem;
		border-radius: var(--radius-sm);
		font-weight: 700;
		font-size: 0.9rem;
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.subtab-btn:hover {
		color: #f8fafc;
		background: rgba(255, 255, 255, 0.05);
	}

	.subtab-btn.active {
		background: rgba(245, 158, 11, 0.15);
		border-color: rgba(245, 158, 11, 0.4);
		color: var(--accent-amber);
	}

	.control-ribbon {
		padding: 1rem 1.25rem;
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 1.5rem;
		flex-wrap: wrap;
		gap: 1rem;
	}

	.date-navigator {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		flex-wrap: wrap;
	}

	.date-input {
		padding: 0.4rem 0.6rem;
		font-size: 0.9rem;
		background: rgba(15, 23, 42, 0.8);
		border-color: var(--border-color);
		color: #fff;
	}

	.report-date-badge {
		background: rgba(6, 182, 212, 0.15);
		border: 1px solid rgba(6, 182, 212, 0.35);
		color: var(--accent-cyan);
		padding: 0.3rem 0.8rem;
		border-radius: 9999px;
		font-size: 0.85rem;
		font-weight: 700;
	}

	.kpi-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
		gap: 1.25rem;
		margin-bottom: 1.5rem;
	}

	.kpi-card {
		padding: 1.25rem 1.5rem;
		display: flex;
		flex-direction: column;
	}

	.kpi-label {
		font-size: 0.8rem;
		color: var(--text-secondary);
		text-transform: uppercase;
		letter-spacing: 0.05em;
		margin-bottom: 0.25rem;
	}

	.kpi-val {
		font-size: 2rem;
		font-weight: 900;
		line-height: 1.1;
		margin-bottom: 0.35rem;
	}

	.kpi-sub {
		font-size: 0.75rem;
		color: var(--text-muted);
	}

	.content-card {
		padding: 1.5rem;
		margin-bottom: 1.5rem;
	}

	.section-title {
		font-size: 1.15rem;
		color: #f8fafc;
		margin: 0 0 1.25rem 0;
	}

	.hourly-chart {
		display: flex;
		align-items: flex-end;
		gap: 0.5rem;
		height: 160px;
		padding-top: 1rem;
		overflow-x: auto;
	}

	.hour-bar-col {
		flex: 1;
		min-width: 45px;
		display: flex;
		flex-direction: column;
		align-items: center;
		height: 100%;
		justify-content: flex-end;
		gap: 0.3rem;
	}

	.hour-bar-track {
		width: 100%;
		height: 100px;
		background: rgba(255, 255, 255, 0.04);
		border-radius: 4px;
		display: flex;
		align-items: flex-end;
		overflow: hidden;
	}

	.hour-bar-fill {
		width: 100%;
		background: linear-gradient(180deg, var(--accent-amber), #d97706);
		border-radius: 4px 4px 0 0;
		transition: height 0.3s ease;
	}

	.hour-val {
		font-size: 0.75rem;
		font-weight: 700;
		color: #f8fafc;
	}

	.hour-label {
		font-size: 0.7rem;
		color: var(--text-secondary);
	}

	.table-container {
		overflow-x: auto;
	}

	.data-table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.88rem;
		text-align: left;
	}

	.data-table th {
		padding: 0.65rem 0.85rem;
		background: rgba(15, 23, 42, 0.6);
		color: var(--text-secondary);
		font-weight: 700;
		border-bottom: 1px solid var(--border-color);
		font-size: 0.75rem;
		text-transform: uppercase;
		letter-spacing: 0.04em;
	}

	.data-table td {
		padding: 0.75rem 0.85rem;
		border-bottom: 1px solid rgba(255, 255, 255, 0.05);
		color: #e2e8f0;
	}

	.data-table tr:hover td {
		background: rgba(255, 255, 255, 0.02);
	}

	.badge-lane {
		background: rgba(245, 158, 11, 0.15);
		color: var(--accent-amber);
		padding: 2px 6px;
		border-radius: 4px;
		font-size: 0.75rem;
		font-weight: 700;
	}

	.status-pill {
		padding: 2px 8px;
		border-radius: 9999px;
		font-size: 0.72rem;
		font-weight: 700;
		text-transform: uppercase;
	}

	.status-confirmed {
		background: rgba(6, 182, 212, 0.15);
		color: var(--accent-cyan);
	}

	.status-completed {
		background: rgba(16, 185, 129, 0.15);
		color: #34d399;
	}

	.status-cancelled {
		background: rgba(239, 68, 68, 0.15);
		color: #ef4444;
	}

	.waiver-pill {
		padding: 2px 6px;
		border-radius: 4px;
		font-size: 0.75rem;
		font-weight: 700;
		background: rgba(245, 158, 11, 0.15);
		color: var(--accent-amber);
	}

	.waiver-pill.complete {
		background: rgba(16, 185, 129, 0.15);
		color: #34d399;
	}

	.week-days-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
		gap: 1rem;
	}

	.day-card {
		background: rgba(15, 23, 42, 0.5);
		border: 1px solid var(--border-color);
		border-radius: var(--radius-sm);
		padding: 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.day-card.has-rev {
		border-color: rgba(245, 158, 11, 0.35);
		background: rgba(245, 158, 11, 0.04);
	}

	.day-card-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.day-name {
		font-weight: 700;
		color: #f8fafc;
		font-size: 0.95rem;
	}

	.day-date {
		font-size: 0.75rem;
		color: var(--text-muted);
	}

	.day-card-rev {
		font-size: 1.4rem;
		font-weight: 800;
		color: #34d399;
	}

	.day-card-stats {
		display: flex;
		justify-content: space-between;
		font-size: 0.75rem;
		color: var(--text-secondary);
	}

	.peak-hours-grid {
		display: flex;
		gap: 1rem;
		flex-wrap: wrap;
	}

	.peak-hour-chip {
		background: rgba(15, 23, 42, 0.6);
		border: 1px solid rgba(255, 255, 255, 0.08);
		border-radius: var(--radius-sm);
		padding: 0.75rem 1.25rem;
		display: flex;
		align-items: center;
		gap: 0.85rem;
	}

	.peak-rank {
		font-size: 1.2rem;
		font-weight: 900;
		color: var(--accent-amber);
	}

	.peak-time {
		font-size: 1rem;
		font-weight: 700;
		color: #f8fafc;
	}

	.peak-stats {
		font-size: 0.78rem;
		display: block;
	}

	.range-controls-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
		width: 100%;
		flex-wrap: wrap;
		gap: 1rem;
	}

	.date-range-inputs {
		display: flex;
		gap: 1rem;
		align-items: center;
		flex-wrap: wrap;
	}

	.input-group {
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.presets-row {
		display: flex;
		gap: 0.5rem;
		flex-wrap: wrap;
	}

	.preset-chip {
		background: rgba(255, 255, 255, 0.06);
		border: 1px solid rgba(255, 255, 255, 0.1);
		color: var(--text-secondary);
		padding: 0.35rem 0.75rem;
		border-radius: 9999px;
		font-size: 0.78rem;
		font-weight: 600;
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.preset-chip:hover {
		background: rgba(245, 158, 11, 0.15);
		border-color: rgba(245, 158, 11, 0.4);
		color: var(--accent-amber);
	}

	.progress-bar-track {
		width: 100px;
		height: 8px;
		background: rgba(255, 255, 255, 0.08);
		border-radius: 9999px;
		overflow: hidden;
	}

	.progress-bar-fill {
		height: 100%;
		background: linear-gradient(90deg, var(--accent-cyan), #38bdf8);
		border-radius: 9999px;
	}

	.games-popularity-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
		gap: 1.25rem;
	}

	.game-pop-card {
		background: rgba(15, 23, 42, 0.6);
		border: 1px solid var(--border-color);
		border-radius: var(--radius-md);
		padding: 1.25rem;
	}

	.game-pop-title {
		font-size: 1.05rem;
		color: #f8fafc;
		margin: 0 0 1rem 0;
	}

	.game-pop-stats {
		display: flex;
		justify-content: space-between;
	}

	.pop-stat {
		display: flex;
		flex-direction: column;
	}

	.pop-stat-val {
		font-size: 1.6rem;
		font-weight: 900;
	}

	.pop-stat-label {
		font-size: 0.75rem;
		color: var(--text-secondary);
	}

	.loading-box {
		padding: 3rem;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 1rem;
		color: var(--text-secondary);
	}

	.spinner {
		width: 32px;
		height: 32px;
		border: 3px solid rgba(245, 158, 11, 0.2);
		border-top-color: var(--accent-amber);
		border-radius: 50%;
		animation: spin 0.8s linear infinite;
	}

	@keyframes spin {
		to {
			transform: rotate(360deg);
		}
	}

	.empty-notice {
		padding: 2rem;
		text-align: center;
		color: var(--text-muted);
	}
</style>
