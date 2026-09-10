<script lang="ts">
	import { onMount } from 'svelte';
	import { venueState } from '$lib/stores/venueState.svelte';
	import {
		getApiAdminUsersVenueByVenueId,
		postApiAdminUsers,
		putApiAdminUsersByIdRole,
		deleteApiAdminUsersById
	} from '$lib/api/client';
	import type { UserSummaryDto, UserRole } from '$lib/api/generated/types.gen';

	let staff = $state<UserSummaryDto[]>([]);
	let isLoading = $state(true);

	// Add staff modal
	let showAddStaffModal = $state(false);
	let newStaffFullName = $state('');
	let newStaffEmail = $state('');
	let newStaffPassword = $state('');
	let newStaffRole = $state<UserRole>(3); // LaneMaster default
	let isAddingStaff = $state(false);
	let addStaffError = $state<string | null>(null);

	// Deleting user
	let deletingStaff = $state<UserSummaryDto | null>(null);

	async function loadStaff() {
		if (!venueState.selectedVenue) return;
		isLoading = true;
		try {
			const res = await getApiAdminUsersVenueByVenueId({
				path: { venueId: venueState.selectedVenue.id }
			});
			staff = res.data || [];
		} catch (e) {
			console.error('Failed to load staff', e);
		} finally {
			isLoading = false;
		}
	}

	async function handleCreateStaff(e: SubmitEvent) {
		e.preventDefault();
		if (!venueState.selectedVenue) return;

		isAddingStaff = true;
		addStaffError = null;

		try {
			const res = await postApiAdminUsers({
				body: {
					venueId: venueState.selectedVenue.id,
					fullName: newStaffFullName.trim(),
					email: newStaffEmail.trim(),
					password: newStaffPassword,
					role: Number(newStaffRole) as any
				}
			});

			if (res.data) {
				showAddStaffModal = false;
				newStaffFullName = '';
				newStaffEmail = '';
				newStaffPassword = '';
				await loadStaff();
			} else {
				addStaffError = 'Failed to create staff account.';
			}
		} catch (err: any) {
			addStaffError = err?.message || 'Error creating staff user.';
		} finally {
			isAddingStaff = false;
		}
	}

	async function handleRoleChange(user: UserSummaryDto, newRole: number) {
		try {
			await putApiAdminUsersByIdRole({
				path: { id: user.id },
				body: { role: newRole as any }
			});
			await loadStaff();
		} catch (e) {
			console.error('Failed to update role', e);
		}
	}

	async function handleDeleteStaff() {
		if (!deletingStaff) return;
		try {
			await deleteApiAdminUsersById({
				path: { id: deletingStaff.id }
			});
			deletingStaff = null;
			await loadStaff();
		} catch (e) {
			console.error('Failed to delete staff user', e);
		}
	}

	function getRoleName(role: number): string {
		switch (role) {
			case 0:
				return 'SuperAdmin';
			case 1:
				return 'Owner';
			case 2:
				return 'General Manager';
			case 3:
				return 'Lane Master (Staff)';
			default:
				return 'Staff';
		}
	}

	$effect(() => {
		if (venueState.selectedVenue) {
			loadStaff();
		}
	});

	onMount(() => {
		loadStaff();
	});
</script>

<svelte:head>
	<title>Staff & Roles | VenueAxe Admin</title>
</svelte:head>

<div class="tab-header">
	<div>
		<h2 class="font-display">Staff Directory & Role Permissions</h2>
		<p class="tab-subtitle">Manage venue operators, lane masters, and administrative credentials</p>
	</div>
	<button class="btn btn-primary font-display" onclick={() => (showAddStaffModal = true)}>
		+ Add Staff Member
	</button>
</div>

{#if isLoading}
	<div style="padding: 3rem; text-align: center; color: var(--text-secondary);">
		<p class="font-display">Loading staff directory...</p>
	</div>
{:else if staff.length === 0}
	<div class="glass-panel" style="padding: 3rem; text-align: center; border-radius: var(--radius-lg);">
		<h3 class="font-display" style="font-size: 1.3rem; margin-bottom: 0.5rem;">No Staff Found</h3>
		<p style="color: var(--text-secondary); margin-bottom: 1.5rem;">There are no staff members assigned to this venue yet.</p>
		<button class="btn btn-primary font-display" onclick={() => (showAddStaffModal = true)}>
			+ Add Staff Member
		</button>
	</div>
{:else}
	<div class="glass-panel" style="overflow-x: auto; border-radius: var(--radius-lg);">
		<table style="width: 100%; border-collapse: collapse; text-align: left; font-size: 0.9rem;">
			<thead>
				<tr style="border-bottom: 1px solid var(--border-color); background: rgba(10, 15, 25, 0.4);">
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">NAME</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">EMAIL ADDRESS</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">ASSIGNED ROLE</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">STATUS</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">LAST LOGIN</th>
					<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem; text-align: right;">ACTIONS</th>
				</tr>
			</thead>
			<tbody>
				{#each staff as user (user.id)}
					<tr style="border-bottom: 1px solid rgba(255, 255, 255, 0.05);">
						<td style="padding: 0.85rem 1rem;">
							<strong class="font-display" style="font-size: 0.95rem;">{user.fullName}</strong>
						</td>
						<td style="padding: 0.85rem 1rem;">
							<span class="font-mono">{user.email}</span>
						</td>
						<td style="padding: 0.85rem 1rem;">
							<select
								class="form-select font-display"
								style="padding: 0.25rem 0.6rem; font-size: 0.8rem; background: rgba(10, 15, 25, 0.7);"
								value={Number(user.role)}
								onchange={(e) => handleRoleChange(user, Number(e.currentTarget.value))}
							>
								<option value={1}>Owner</option>
								<option value={2}>General Manager</option>
								<option value={3}>Lane Master</option>
							</select>
						</td>
						<td style="padding: 0.85rem 1rem;">
							{#if user.isActive}
								<span class="badge badge-available font-display">Active</span>
							{:else}
								<span class="badge badge-deactivated font-display">Inactive</span>
							{/if}
						</td>
						<td style="padding: 0.85rem 1rem;">
							<span class="font-mono" style="font-size: 0.82rem; color: var(--text-secondary);">
								{user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'Never'}
							</span>
						</td>
						<td style="padding: 0.85rem 1rem; text-align: right;">
							<button
								type="button"
								class="btn btn-secondary btn-xs btn-delete font-display"
								onclick={() => (deletingStaff = user)}
							>
								Remove
							</button>
						</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
{/if}

<!-- ADD STAFF MODAL -->
{#if showAddStaffModal}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (showAddStaffModal = false)}
		onkeydown={(e) => {
			if (e.key === 'Escape') showAddStaffModal = false;
		}}
	>
		<div
			class="modal-card glass-panel"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			style="max-width: 500px;"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<div class="modal-header-row">
				<div>
					<h3 class="modal-title font-display">Add Staff Member</h3>
					<p class="editor-hint" style="margin-bottom: 0;">Create a login account for venue personnel</p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (showAddStaffModal = false)}>✕</button>
			</div>

			{#if addStaffError}
				<div class="alert-error" style="margin-top: 1rem;">
					⚠️ {addStaffError}
				</div>
			{/if}

			<form onsubmit={handleCreateStaff} style="margin-top: 1.25rem;">
				<div class="form-group">
					<label class="form-label" for="st-name">Full Name *</label>
					<input id="st-name" type="text" class="form-input" bind:value={newStaffFullName} required placeholder="Marcus Vance" />
				</div>

				<div class="form-group" style="margin-top: 0.75rem;">
					<label class="form-label" for="st-email">Email Address *</label>
					<input id="st-email" type="email" class="form-input" bind:value={newStaffEmail} required placeholder="marcus@venueaxe.com" />
				</div>

				<div class="form-row-2" style="margin-top: 0.75rem;">
					<div class="form-group">
						<label class="form-label" for="st-role">Role *</label>
						<select id="st-role" class="form-input" bind:value={newStaffRole}>
							<option value={3}>Lane Master (Scorekeeping & Operations)</option>
							<option value={2}>General Manager (All Features)</option>
							<option value={1}>Venue Owner (Full Access)</option>
						</select>
					</div>
					<div class="form-group">
						<label class="form-label" for="st-pass">Initial Password *</label>
						<input id="st-pass" type="password" class="form-input" bind:value={newStaffPassword} required minlength="8" placeholder="••••••••" />
					</div>
				</div>

				<div class="modal-actions" style="margin-top: 1.5rem;">
					<button type="button" class="btn btn-secondary" onclick={() => (showAddStaffModal = false)}>
						Cancel
					</button>
					<button type="submit" class="btn btn-primary font-display" disabled={isAddingStaff}>
						{isAddingStaff ? 'Creating...' : '+ Create Account'}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

<!-- DELETE STAFF MODAL -->
{#if deletingStaff}
	<div
		class="modal-overlay"
		role="button"
		tabindex="0"
		onclick={() => (deletingStaff = null)}
		onkeydown={(e) => {
			if (e.key === 'Escape') deletingStaff = null;
		}}
	>
		<div
			class="modal-card glass-panel modal-danger"
			role="dialog"
			aria-modal="true"
			tabindex="-1"
			onclick={(e) => e.stopPropagation()}
			onkeydown={(e) => e.stopPropagation()}
		>
			<h3 class="modal-title font-display" style="color: var(--accent-crimson);">Remove Staff Member</h3>
			<p style="color: var(--text-secondary); margin-top: 0.5rem; line-height: 1.5;">
				Are you sure you want to revoke access for <strong>{deletingStaff.fullName}</strong> ({deletingStaff.email})?
			</p>
			<div class="modal-actions" style="margin-top: 1.5rem;">
				<button type="button" class="btn btn-secondary" onclick={() => (deletingStaff = null)}>Cancel</button>
				<button type="button" class="btn btn-danger font-display" onclick={handleDeleteStaff}>
					Revoke Access
				</button>
			</div>
		</div>
	</div>
{/if}
