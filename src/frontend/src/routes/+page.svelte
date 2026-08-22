<script lang="ts">
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth.svelte';
	import { goto } from '$app/navigation';

	let email = $state('');
	let password = $state('');
	let errorMsg = $state('');
	let isSubmitting = $state(false);

	onMount(async () => {
		await auth.init();
		if (auth.isAuthenticated) {
			goto('/admin');
		}
	});

	async function handleLogin(e: SubmitEvent) {
		e.preventDefault();
		errorMsg = '';
		isSubmitting = true;

		try {
			const ok = await auth.login(email, password);
			if (ok) {
				goto('/admin');
			} else {
				errorMsg = 'Invalid email or password. Please verify your credentials.';
			}
		} catch (err) {
			errorMsg = 'Network or server error connecting to VenueAxe API.';
		} finally {
			isSubmitting = false;
		}
	}
</script>

<div class="login-page">
	<div class="login-card glass-panel">
		<div class="login-header">
			<span class="icon">🪓</span>
			<h1 class="title font-display">VENUE<span class="text-amber">AXE</span></h1>
			<p class="subtitle">Operator & Staff Portal</p>
		</div>

		{#if errorMsg}
			<div class="alert-error">
				<span>⚠️</span>
				<span>{errorMsg}</span>
			</div>
		{/if}

		<form onsubmit={handleLogin} class="login-form">
			<div class="form-group">
				<label class="form-label" for="staff-email">Staff Email</label>
				<input
					id="staff-email"
					type="email"
					class="form-input"
					bind:value={email}
					required
					placeholder="operator@venue.com"
				/>
			</div>

			<div class="form-group">
				<label class="form-label" for="staff-password">Password</label>
				<input
					id="staff-password"
					type="password"
					class="form-input"
					bind:value={password}
					required
					placeholder="••••••••"
				/>
			</div>

			<button type="submit" class="btn btn-primary btn-block" disabled={isSubmitting}>
				{isSubmitting ? 'Authenticating...' : 'Sign In to Venue Console'}
			</button>
		</form>

		<div class="login-footer">
			<p class="note">
				🔒 Secured with HttpOnly Cookie Authentication. End-players and throwers do not require logins.
			</p>
		</div>
	</div>
</div>

<style>
	.login-page {
		min-height: calc(100vh - 80px);
		display: flex;
		align-items: center;
		justify-content: center;
		padding: 2rem 1rem;
	}

	.login-card {
		width: 100%;
		max-width: 440px;
		padding: 2.5rem;
	}

	.login-header {
		text-align: center;
		margin-bottom: 2rem;
	}

	.icon {
		font-size: 2.5rem;
		display: block;
		margin-bottom: 0.5rem;
	}

	.title {
		font-size: 1.8rem;
		font-weight: 800;
		letter-spacing: 0.05em;
	}

	.text-amber {
		color: var(--accent-amber);
	}

	.subtitle {
		color: var(--text-secondary);
		font-size: 0.95rem;
		margin-top: 0.25rem;
	}

	.alert-error {
		background: rgba(239, 68, 68, 0.15);
		border: 1px solid var(--accent-crimson);
		color: #fca5a5;
		padding: 0.75rem 1rem;
		border-radius: var(--radius-md);
		font-size: 0.85rem;
		margin-bottom: 1.5rem;
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.login-form {
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
	}

	.form-group {
		display: flex;
		flex-direction: column;
	}

	.btn-block {
		width: 100%;
		margin-top: 0.5rem;
	}

	.login-footer {
		margin-top: 2rem;
		text-align: center;
		border-top: 1px solid var(--border-color);
		padding-top: 1.25rem;
	}

	.note {
		color: var(--text-muted);
		font-size: 0.8rem;
		line-height: 1.4;
	}
</style>
