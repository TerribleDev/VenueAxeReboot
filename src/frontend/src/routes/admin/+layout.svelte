<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth.svelte';

	let { children } = $props();
	let isChecking = $state(true);

	const isPublicAdminRoute = $derived(
		page.url.pathname === '/admin/login' || page.url.pathname === '/admin/register'
	);

	onMount(async () => {
		await auth.checkAuth();
		isChecking = false;

		if (!auth.isAuthenticated && !isPublicAdminRoute) {
			goto('/admin/login');
		}
	});

	$effect(() => {
		if (!isChecking && !auth.isAuthenticated && !isPublicAdminRoute) {
			goto('/admin/login');
		}
	});
</script>

{#if isPublicAdminRoute}
	{@render children()}
{:else if isChecking}
	<div class="admin-auth-guard">
		<div class="spinner"></div>
		<p class="font-display">Verifying Operator Credentials...</p>
	</div>
{:else if auth.isAuthenticated}
	{@render children()}
{:else}
	<div class="admin-auth-guard">
		<p class="font-display">Redirecting to Operator Login...</p>
	</div>
{/if}

<style>
	.admin-auth-guard {
		min-height: 80vh;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 1.5rem;
		color: var(--text-secondary);
	}

	.spinner {
		width: 48px;
		height: 48px;
		border: 4px solid rgba(245, 158, 11, 0.15);
		border-top-color: var(--accent-amber);
		border-radius: 50%;
		animation: spin 0.8s linear infinite;
	}

	@keyframes spin {
		to {
			transform: rotate(360deg);
		}
	}
</style>
