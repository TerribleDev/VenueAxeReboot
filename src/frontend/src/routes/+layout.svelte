<script lang="ts">
	import '../app.css';
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { auth } from '$lib/stores/auth.svelte';
	import { venueState } from '$lib/stores/venueState.svelte';

	let { children } = $props();

	onMount(async () => {
		await auth.checkAuth();
		if (auth.isAuthenticated) {
			await venueState.loadVenues();
		}
	});

	$effect(() => {
		if (auth.isAuthenticated && venueState.venues.length === 0 && !venueState.isLoading) {
			venueState.loadVenues();
		}
	});

	const hideNavHeader = $derived(
		page.url.pathname.startsWith('/sign') ||
		page.url.pathname.startsWith('/book') ||
		page.url.pathname.startsWith('/tablet') ||
		page.url.pathname.startsWith('/screen')
	);

	const venueSlug = $derived(venueState.selectedVenue?.slug || 'downtown');
</script>

<div class="app-layout">
	{#if !hideNavHeader}
		<header class="app-header">
			<div class="header-inner">
				<a href="/" class="brand-link">
					<span class="brand-icon">🪓</span>
					<span class="brand-name font-display">VENUE<span class="text-amber">AXE</span></span>
				</a>

				{#if auth.isAuthenticated}
					<nav class="nav-links">
						<a href="/admin" class="nav-item font-display">Venue Admin</a>
						<a href="/tablet" class="nav-item font-display">Lane Tablet</a>
						<a href="/screen" class="nav-item font-display">Lane TV</a>
						<a href="/book/{venueSlug}" class="nav-item font-display">Customer Booking</a>
						<a href="/sign/{venueSlug}" class="nav-item font-display">Waiver Kiosk</a>
					</nav>
				{/if}

				<div class="auth-box">
					{#if auth.isAuthenticated && auth.user}
						<div class="user-pill">
							<span class="user-dot"></span>
							<span class="user-name">{auth.user.firstName} ({auth.user.role})</span>
							<button type="button" class="btn-logout" onclick={() => auth.logout()}>Logout</button>
						</div>
					{:else}
						<a href="/admin/login" class="btn btn-secondary btn-sm">Staff Login</a>
					{/if}
				</div>
			</div>
		</header>
	{/if}

	<main class="app-main">
		{@render children()}
	</main>
</div>

<style>
	.app-layout {
		display: flex;
		flex-direction: column;
		min-height: 100vh;
	}

	.app-header {
		background: rgba(10, 12, 16, 0.85);
		backdrop-filter: blur(16px);
		-webkit-backdrop-filter: blur(16px);
		border-bottom: 1px solid var(--border-color);
		position: sticky;
		top: 0;
		z-index: 50;
	}

	.header-inner {
		max-width: 1400px;
		margin: 0 auto;
		padding: 0.75rem 1.5rem;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 1.5rem;
	}

	.brand-link {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		text-decoration: none;
		color: var(--text-primary);
	}

	.brand-icon {
		font-size: 1.5rem;
	}

	.brand-name {
		font-size: 1.3rem;
		font-weight: 800;
		letter-spacing: 0.05em;
	}

	.text-amber {
		color: var(--accent-amber);
	}

	.nav-links {
		display: flex;
		align-items: center;
		gap: 1.25rem;
	}

	.nav-item {
		color: var(--text-secondary);
		text-decoration: none;
		font-weight: 600;
		font-size: 0.9rem;
		text-transform: uppercase;
		letter-spacing: 0.04em;
		transition: color 0.15s ease;
	}

	.nav-item:hover {
		color: var(--accent-amber);
	}

	.user-pill {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		background: var(--bg-surface-elevated);
		border: 1px solid var(--border-color);
		padding: 0.35rem 0.75rem;
		border-radius: 9999px;
		font-size: 0.85rem;
	}

	.user-dot {
		width: 8px;
		height: 8px;
		border-radius: 50%;
		background: var(--accent-emerald);
		box-shadow: 0 0 6px var(--accent-emerald);
	}

	.btn-logout {
		background: transparent;
		border: none;
		color: var(--text-muted);
		cursor: pointer;
		font-size: 0.75rem;
		text-transform: uppercase;
		margin-left: 0.5rem;
	}

	.btn-logout:hover {
		color: var(--accent-crimson);
	}

	.btn-sm {
		padding: 0.4rem 0.9rem;
		font-size: 0.8rem;
	}

	.app-main {
		flex: 1;
	}

	@media (max-width: 900px) {
		.nav-links {
			display: none;
		}
	}
</style>
