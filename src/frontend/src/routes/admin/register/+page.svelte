<script lang="ts">
	import { auth } from '$lib/stores/auth.svelte';
	import { goto } from '$app/navigation';

	let organizationName = $state('');
	let venueName = $state('');
	let firstName = $state('');
	let lastName = $state('');
	let email = $state('');
	let password = $state('');
	let city = $state('');
	let timezone = $state('America/New_York');

	let errorMessage = $state<string | null>(null);
	let isLoading = $state(false);

	async function handleSubmit(e: SubmitEvent) {
		e.preventDefault();
		errorMessage = null;

		if (!organizationName || !venueName || !firstName || !lastName || !email || !password) {
			errorMessage = 'Please complete all required fields.';
			return;
		}

		if (password.length < 6) {
			errorMessage = 'Password must be at least 6 characters long.';
			return;
		}

		isLoading = true;
		try {
			const res = await auth.register({
				organizationName,
				venueName,
				firstName,
				lastName,
				email,
				password,
				city,
				timezone
			});

			if (res.success) {
				goto('/admin');
			} else {
				errorMessage = res.error || 'Registration failed. Please try again.';
			}
		} catch (err: any) {
			errorMessage = err.message || 'An unexpected error occurred.';
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>Register Venue Organization | VenueAxe</title>
</svelte:head>

<div class="register-container">
	<div class="register-card glass-panel">
		<div class="register-header">
			<div class="brand-badge">
				<span class="axe-icon">🪓</span>
				<span class="brand-text font-display">VENUE<span class="text-amber">AXE</span></span>
			</div>
			<h1 class="register-title font-display">Create Your Venue Account</h1>
			<p class="register-subtitle">
				Launch your commercial axe throwing venue operating system with real-time scoring, online booking, and digital waivers.
			</p>
		</div>

		{#if errorMessage}
			<div class="alert alert-danger font-display">
				⚠️ {errorMessage}
			</div>
		{/if}

		<form onsubmit={handleSubmit} class="register-form">
			<div class="form-section">
				<h3 class="section-heading font-display">1. Business & Location</h3>
				<div class="form-row">
					<div class="form-group">
						<label for="orgName">Organization / Company Name *</label>
						<input
							id="orgName"
							type="text"
							class="form-input"
							placeholder="e.g. Apex Throwing Group"
							bind:value={organizationName}
							required
						/>
					</div>
					<div class="form-group">
						<label for="venueName">First Venue Name *</label>
						<input
							id="venueName"
							type="text"
							class="form-input"
							placeholder="e.g. Apex Axe Lounge - Downtown"
							bind:value={venueName}
							required
						/>
					</div>
				</div>

				<div class="form-row">
					<div class="form-group">
						<label for="city">City / Metro Area</label>
						<input
							id="city"
							type="text"
							class="form-input"
							placeholder="e.g. Austin, TX"
							bind:value={city}
						/>
					</div>
					<div class="form-group">
						<label for="timezone">Timezone</label>
						<select id="timezone" class="form-input" bind:value={timezone}>
							<option value="America/New_York">Eastern Time (US & Canada)</option>
							<option value="America/Chicago">Central Time (US & Canada)</option>
							<option value="America/Denver">Mountain Time (US & Canada)</option>
							<option value="America/Los_Angeles">Pacific Time (US & Canada)</option>
							<option value="Europe/London">London (GMT / BST)</option>
							<option value="Australia/Sydney">Sydney (AEST)</option>
						</select>
					</div>
				</div>
			</div>

			<div class="form-section">
				<h3 class="section-heading font-display">2. Primary Operator / Owner Account</h3>
				<div class="form-row">
					<div class="form-group">
						<label for="firstName">First Name *</label>
						<input
							id="firstName"
							type="text"
							class="form-input"
							placeholder="e.g. Dave"
							bind:value={firstName}
							required
						/>
					</div>
					<div class="form-group">
						<label for="lastName">Last Name *</label>
						<input
							id="lastName"
							type="text"
							class="form-input"
							placeholder="e.g. Miller"
							bind:value={lastName}
							required
						/>
					</div>
				</div>

				<div class="form-group">
					<label for="email">Work Email Address *</label>
					<input
						id="email"
						type="email"
						class="form-input"
						placeholder="owner@yourvenue.com"
						bind:value={email}
						required
					/>
				</div>

				<div class="form-group">
					<label for="password">Create Secure Password *</label>
					<input
						id="password"
						type="password"
						class="form-input"
						placeholder="Minimum 6 characters"
						bind:value={password}
						required
					/>
				</div>
			</div>

			<button
				type="submit"
				class="btn btn-primary btn-block btn-lg font-display"
				disabled={isLoading}
			>
				{isLoading ? 'Setting Up Your Operating System...' : '🚀 Launch VenueAxe Platform'}
			</button>
		</form>

		<div class="register-footer">
			<p>
				Already have a registered account?
				<a href="/admin/login" class="link-amber">Sign In to Admin Portal</a>
			</p>
		</div>
	</div>
</div>

<style>
	.register-container {
		min-height: calc(100vh - 75px);
		display: flex;
		align-items: center;
		justify-content: center;
		padding: 2.5rem 1rem;
		background: radial-gradient(circle at top center, rgba(245, 158, 11, 0.08) 0%, rgba(10, 12, 16, 1) 70%);
	}

	.register-card {
		width: 100%;
		max-width: 680px;
		padding: 2.5rem;
		border-radius: var(--radius-xl);
		background: rgba(19, 23, 34, 0.95);
		border: 1px solid var(--border-color);
		box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.7);
	}

	.register-header {
		text-align: center;
		margin-bottom: 2rem;
	}

	.brand-badge {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.4rem 1rem;
		background: rgba(245, 158, 11, 0.1);
		border: 1px solid rgba(245, 158, 11, 0.25);
		border-radius: 9999px;
		margin-bottom: 1.25rem;
	}

	.axe-icon {
		font-size: 1.25rem;
	}

	.brand-text {
		font-size: 1.1rem;
		font-weight: 800;
		letter-spacing: 0.05em;
	}

	.text-amber {
		color: var(--accent-amber);
	}

	.register-title {
		font-size: 2rem;
		font-weight: 800;
		color: var(--text-primary);
		margin: 0 0 0.5rem 0;
	}

	.register-subtitle {
		color: var(--text-secondary);
		font-size: 0.95rem;
		line-height: 1.5;
		margin: 0;
	}

	.form-section {
		margin-bottom: 1.75rem;
		padding-bottom: 1.5rem;
		border-bottom: 1px solid rgba(255, 255, 255, 0.06);
	}

	.section-heading {
		font-size: 1.1rem;
		font-weight: 700;
		color: var(--accent-amber);
		margin: 0 0 1rem 0;
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.form-row {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 1rem;
	}

	.form-group {
		margin-bottom: 1.25rem;
	}

	.form-group label {
		display: block;
		font-size: 0.85rem;
		font-weight: 600;
		color: var(--text-secondary);
		margin-bottom: 0.4rem;
	}

	.form-input {
		width: 100%;
		padding: 0.75rem 1rem;
		background: rgba(10, 12, 16, 0.8);
		border: 1px solid var(--border-color);
		border-radius: var(--radius-md);
		color: var(--text-primary);
		font-size: 0.95rem;
		transition: border-color 0.2s ease, box-shadow 0.2s ease;
	}

	.form-input:focus {
		outline: none;
		border-color: var(--accent-amber);
		box-shadow: 0 0 0 3px rgba(245, 158, 11, 0.2);
	}

	.btn-block {
		width: 100%;
	}

	.btn-lg {
		padding: 1rem 1.5rem;
		font-size: 1.05rem;
		letter-spacing: 0.04em;
	}

	.register-footer {
		text-align: center;
		margin-top: 1.75rem;
		padding-top: 1.25rem;
		border-top: 1px solid var(--border-color);
		font-size: 0.9rem;
		color: var(--text-muted);
	}

	.link-amber {
		color: var(--accent-amber);
		text-decoration: none;
		font-weight: 600;
		margin-left: 0.35rem;
	}

	.link-amber:hover {
		text-decoration: underline;
	}

	.alert {
		padding: 0.85rem 1.25rem;
		border-radius: var(--radius-md);
		margin-bottom: 1.5rem;
		font-size: 0.9rem;
	}

	.alert-danger {
		background: rgba(239, 68, 68, 0.15);
		border: 1px solid rgba(239, 68, 68, 0.4);
		color: #fca5a5;
	}

	@media (max-width: 640px) {
		.form-row {
			grid-template-columns: 1fr;
		}
		.register-card {
			padding: 1.5rem;
		}
	}
</style>
