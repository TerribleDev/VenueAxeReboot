<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { venueState } from '$lib/stores/venueState.svelte';
	import {
		getApiAdminWaiversSearch,
		getApiAdminWaiversTemplatesVenueByVenueId,
		putApiAdminWaiversTemplatesByTemplateId
	} from '$lib/api/client';
	import type { WaiverDto, WaiverTemplateDto } from '$lib/api/generated/types.gen';

	let waivers = $state<WaiverDto[]>([]);
	let templates = $state<WaiverTemplateDto[]>([]);
	let isLoading = $state(true);
	let searchTerm = $state('');

	// Pagination state (Server-side scalable for thousands of signatures)
	let pageNumber = $state(1);
	let pageSize = $state(20);
	let totalCount = $state(0);
	let totalPages = $state(1);
	let searchDebounceTimer: any = null;

	// Active tab: 'vault' | 'template'
	let activeSubTab = $state<'vault' | 'template'>('vault');

	// Template editing state
	let editingTemplate = $state<WaiverTemplateDto | null>(null);
	let templateTitle = $state('');
	let templateMarkdown = $state('');
	let isSavingTemplate = $state(false);
	let templateSuccessMsg = $state<string | null>(null);
	let templateErrorMsg = $state<string | null>(null);

	// Selected waiver modal
	let selectedWaiver = $state<WaiverDto | null>(null);
	let downloadingWaiverId = $state<string | null>(null);

	function formatMinors(json: string | null | undefined): string[] {
		if (!json) return [];
		try {
			const parsed = JSON.parse(json);
			if (Array.isArray(parsed)) {
				return parsed
					.map((item: any) => {
						if (typeof item === 'string') return item.trim();
						if (item && typeof item === 'object' && item.name) return String(item.name).trim();
						return JSON.stringify(item);
					})
					.filter(Boolean);
			}
			if (parsed && typeof parsed === 'object' && parsed.name) {
				return [String(parsed.name).trim()];
			}
			return [String(parsed).trim()];
		} catch {
			return [json.trim()];
		}
	}

	async function downloadWaiverPdf(id: string, signerName?: string) {
		downloadingWaiverId = id;
		try {
			const res = await fetch(`/api/admin/waivers/${id}/pdf`, {
				credentials: 'include'
			});
			if (!res.ok) {
				alert('Could not download PDF. Waiver record may not be found or server error.');
				return;
			}
			const blob = await res.blob();
			const url = URL.createObjectURL(blob);
			const a = document.createElement('a');
			a.href = url;
			const cleanName = (signerName || 'Signature').replace(/[^a-zA-Z0-9_-]/g, '_');
			a.download = `Waiver-${cleanName}-${id.substring(0, 8)}.pdf`;
			document.body.appendChild(a);
			a.click();
			document.body.removeChild(a);
			URL.revokeObjectURL(url);
		} catch (err) {
			console.error('Error downloading waiver PDF:', err);
			alert('An error occurred while downloading the PDF.');
		} finally {
			downloadingWaiverId = null;
		}
	}

	async function loadWaivers() {
		if (!venueState.selectedVenue) return;
		isLoading = true;
		try {
			const res = await getApiAdminWaiversSearch({
				query: {
					venueId: venueState.selectedVenue.id,
					term: searchTerm.trim() || undefined,
					page: pageNumber,
					pageSize: pageSize
				}
			});
			if (res.data) {
				waivers = res.data.items || [];
				totalCount = Number(res.data.totalCount) || 0;
				pageNumber = Number(res.data.pageNumber) || 1;
				pageSize = Number(res.data.pageSize) || 20;
				totalPages = Math.max(1, Number(res.data.totalPages) || 1);
			} else {
				waivers = [];
				totalCount = 0;
				totalPages = 1;
			}
		} catch (e) {
			console.error('Failed to load waivers', e);
		} finally {
			isLoading = false;
		}
	}

	function handleSearchInput() {
		if (searchDebounceTimer) clearTimeout(searchDebounceTimer);
		searchDebounceTimer = setTimeout(() => {
			pageNumber = 1;
			loadWaivers();
		}, 300);
	}

	function changePage(newPage: number) {
		if (newPage < 1 || newPage > totalPages || newPage === pageNumber) return;
		pageNumber = newPage;
		loadWaivers();
	}

	function handlePageSizeChange(e: Event) {
		const target = e.target as HTMLSelectElement;
		pageSize = Number(target.value) || 20;
		pageNumber = 1;
		loadWaivers();
	}

	async function loadTemplates() {
		if (!venueState.selectedVenue) return;
		try {
			const res = await getApiAdminWaiversTemplatesVenueByVenueId({
				path: { venueId: venueState.selectedVenue.id }
			});
			templates = res.data || [];
			if (templates.length > 0) {
				editingTemplate = templates[0];
				templateTitle = templates[0].title;
				templateMarkdown = templates[0].bodyTextMarkdown;
			}
		} catch (e) {
			console.error('Failed to load waiver templates', e);
		}
	}

	async function handleSaveTemplate(e: SubmitEvent) {
		e.preventDefault();
		if (!editingTemplate) return;
		isSavingTemplate = true;
		templateSuccessMsg = null;
		templateErrorMsg = null;

		try {
			const res = await putApiAdminWaiversTemplatesByTemplateId({
				path: { templateId: editingTemplate.id },
				body: {
					title: templateTitle.trim(),
					bodyTextMarkdown: templateMarkdown.trim(),
					isActive: true
				}
			});
			if (res.data) {
				templateSuccessMsg = 'Legal waiver template updated successfully! New version created with computed SHA-256 hash.';
				await loadTemplates();
			} else {
				templateErrorMsg = 'Failed to update template.';
			}
		} catch (err: any) {
			templateErrorMsg = err?.message || 'Error updating template.';
		} finally {
			isSavingTemplate = false;
		}
	}

	$effect(() => {
		if (venueState.selectedVenue) {
			pageNumber = 1;
			loadWaivers();
			loadTemplates();
		}
	});

	onDestroy(() => {
		if (searchDebounceTimer) clearTimeout(searchDebounceTimer);
	});

	onMount(() => {
		loadWaivers();
		loadTemplates();
	});
</script>

<svelte:head>
	<title>Waiver Vault | VenueAxe Admin</title>
</svelte:head>

<div class="tab-header">
	<div>
		<h2 class="font-display">Digital Waiver Vault & Compliance</h2>
		<p class="tab-subtitle">Cryptographic safety release records, minor participant tracking, and legal templates</p>
	</div>
	<div style="display: flex; gap: 0.75rem; align-items: center;">
		<div class="view-toggle-group" style="display: inline-flex; background: #0f141c; border: 1px solid var(--border-color); border-radius: var(--radius-sm); padding: 2px;">
			<button
				type="button"
				class="btn-toggle font-display"
				class:active={activeSubTab === 'vault'}
				onclick={() => (activeSubTab = 'vault')}
				style="padding: 0.4rem 0.85rem; font-size: 0.85rem; background: {activeSubTab === 'vault' ? 'var(--accent-amber)' : 'transparent'}; color: {activeSubTab === 'vault' ? '#000' : 'var(--text-secondary)'}; border: none; border-radius: 4px; cursor: pointer;"
			>
				✍️ Signed Waivers ({totalCount})
			</button>
			<button
				type="button"
				class="btn-toggle font-display"
				class:active={activeSubTab === 'template'}
				onclick={() => (activeSubTab = 'template')}
				style="padding: 0.4rem 0.85rem; font-size: 0.85rem; background: {activeSubTab === 'template' ? 'var(--accent-amber)' : 'transparent'}; color: {activeSubTab === 'template' ? '#000' : 'var(--text-secondary)'}; border: none; border-radius: 4px; cursor: pointer;"
			>
				📜 Legal Template Editor
			</button>
		</div>
	</div>
</div>

{#if activeSubTab === 'vault'}
	<!-- Search & Filters -->
	<div class="glass-panel" style="padding: 1rem 1.25rem; margin-bottom: 1.5rem; display: flex; gap: 1rem; align-items: center; flex-wrap: wrap;">
		<div style="flex: 1; min-width: 280px; position: relative;">
			<input
				type="text"
				class="form-input"
				style="width: 100%;"
				placeholder="🔍 Case-insensitive search by signer name, child name, email, or phone..."
				bind:value={searchTerm}
				oninput={handleSearchInput}
			/>
			{#if searchTerm}
				<button
					type="button"
					class="btn-clear"
					onclick={() => { searchTerm = ''; pageNumber = 1; loadWaivers(); }}
					style="position: absolute; right: 10px; top: 50%; transform: translateY(-50%); color: var(--text-muted); cursor: pointer;"
					title="Clear Search"
				>
					✕
				</button>
			{/if}
		</div>
		<div style="display: flex; gap: 0.5rem; align-items: center;">
			<span style="font-size: 0.82rem; color: var(--text-muted);">Rows per page:</span>
			<select
				class="form-input"
				style="padding: 0.35rem 0.6rem; font-size: 0.85rem; width: auto;"
				value={pageSize}
				onchange={handlePageSizeChange}
			>
				<option value={10}>10</option>
				<option value={20}>20</option>
				<option value={50}>50</option>
				<option value={100}>100</option>
			</select>
			<button class="btn btn-secondary font-display btn-sm" onclick={() => { pageNumber = 1; loadWaivers(); }}>
				Refresh
			</button>
		</div>
	</div>

	{#if isLoading}
		<div style="padding: 3rem; text-align: center; color: var(--text-secondary);">
			<p class="font-display">Loading signed waivers...</p>
		</div>
	{:else if waivers.length === 0}
		<div class="glass-panel" style="padding: 3rem; text-align: center; border-radius: var(--radius-lg);">
			<h3 class="font-display" style="font-size: 1.3rem; margin-bottom: 0.5rem;">No Signed Waivers Found</h3>
			<p style="color: var(--text-secondary);">No signed safety records match the active search criteria.</p>
		</div>
	{:else}
		<div class="glass-panel" style="overflow-x: auto; border-radius: var(--radius-lg);">
			<table style="width: 100%; border-collapse: collapse; text-align: left; font-size: 0.88rem;">
				<thead>
					<tr style="border-bottom: 1px solid var(--border-color); background: rgba(10, 15, 25, 0.4);">
						<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">SIGNER NAME</th>
						<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">CONTACT INFO</th>
						<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">DATE OF BIRTH</th>
						<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">GUARDIAN STATUS</th>
						<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">MINORS COVERED</th>
						<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">SIGNED AT</th>
						<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem;">SIGNATURE</th>
						<th class="font-display" style="padding: 0.85rem 1rem; color: var(--text-muted); font-size: 0.75rem; text-align: right;">AUDIT PDF</th>
					</tr>
				</thead>
				<tbody>
					{#each waivers as w (w.id)}
						<tr style="border-bottom: 1px solid rgba(255, 255, 255, 0.05);">
							<td style="padding: 0.85rem 1rem;">
								<strong class="font-display" style="font-size: 0.95rem;">{w.signerFirstName} {w.signerLastName}</strong>
							</td>
							<td style="padding: 0.85rem 1rem;">
								<span>{w.signerEmail}</span><br />
								<small class="font-mono" style="color: var(--text-muted);">{w.signerPhone}</small>
							</td>
							<td style="padding: 0.85rem 1rem;">
								<span class="font-mono">{w.dateOfBirth}</span>
							</td>
							<td style="padding: 0.85rem 1rem;">
								{#if w.isGuardianSigning}
									<span class="badge badge-turnaround font-display">Parent / Guardian</span>
								{:else}
									<span class="badge badge-available font-display">Adult (Self)</span>
								{/if}
							</td>
							<td style="padding: 0.85rem 1rem;">
								{#if w.minorsCoveredJson}
									{@const minors = formatMinors(w.minorsCoveredJson)}
									{#if minors.length > 0}
										<div style="display: flex; flex-wrap: wrap; gap: 4px;">
											{#each minors as minor}
												<span class="badge badge-turnaround font-display" style="font-size: 0.75rem; padding: 2px 6px; text-transform: none;">
													🧒 {minor}
												</span>
											{/each}
										</div>
									{:else}
										<span style="color: var(--text-muted);">None</span>
									{/if}
								{:else}
									<span style="color: var(--text-muted);">None</span>
								{/if}
							</td>
							<td style="padding: 0.85rem 1rem;">
								<span class="font-mono" style="font-size: 0.82rem;">{new Date(w.signedAtUtc).toLocaleString()}</span>
							</td>
							<td style="padding: 0.85rem 1rem;">
								{#if w.signatureImagePngBase64}
									<button
										type="button"
										class="btn-clear"
										style="padding: 0; display: inline-block; cursor: pointer;"
										onclick={() => (selectedWaiver = w)}
										title="View Signature"
									>
										<img
											src={w.signatureImagePngBase64}
											alt="Signature"
											style="max-height: 36px; max-width: 100px; background: #0f141c; padding: 4px 6px; border-radius: 4px; border: 1px solid var(--border-color); display: block;"
										/>
									</button>
								{/if}
							</td>
							<td style="padding: 0.85rem 1rem; text-align: right;">
								<button
									type="button"
									class="btn btn-secondary btn-xs font-display"
									disabled={downloadingWaiverId === w.id}
									onclick={() => downloadWaiverPdf(w.id, `${w.signerLastName}_${w.signerFirstName}`)}
									title="Download Legal Audit PDF"
								>
									{downloadingWaiverId === w.id ? '⏳ PDF' : '📄 PDF'}
								</button>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>

			<!-- Pagination Footer -->
			<div style="display: flex; justify-content: space-between; align-items: center; padding: 0.85rem 1.25rem; border-top: 1px solid var(--border-color); background: rgba(10, 15, 25, 0.5); flex-wrap: wrap; gap: 0.75rem;">
				<div style="font-size: 0.82rem; color: var(--text-secondary);">
					Showing <strong style="color: #fff;">{totalCount > 0 ? (pageNumber - 1) * pageSize + 1 : 0}</strong> -
					<strong style="color: #fff;">{Math.min(pageNumber * pageSize, totalCount)}</strong> of
					<strong style="color: var(--accent-amber);">{totalCount}</strong> signatures
				</div>

				<div style="display: flex; gap: 0.4rem; align-items: center;">
					<button
						type="button"
						class="btn btn-secondary btn-xs font-display"
						disabled={pageNumber <= 1}
						onclick={() => changePage(1)}
						title="First Page"
					>
						«
					</button>
					<button
						type="button"
						class="btn btn-secondary btn-xs font-display"
						disabled={pageNumber <= 1}
						onclick={() => changePage(pageNumber - 1)}
					>
						‹ Prev
					</button>

					<span style="font-size: 0.82rem; color: var(--text-secondary); padding: 0 0.5rem;">
						Page <strong style="color: #fff;">{pageNumber}</strong> of <strong style="color: #fff;">{totalPages}</strong>
					</span>

					<button
						type="button"
						class="btn btn-secondary btn-xs font-display"
						disabled={pageNumber >= totalPages}
						onclick={() => changePage(pageNumber + 1)}
					>
						Next ›
					</button>
					<button
						type="button"
						class="btn btn-secondary btn-xs font-display"
						disabled={pageNumber >= totalPages}
						onclick={() => changePage(totalPages)}
						title="Last Page"
					>
						»
					</button>
				</div>
			</div>
		</div>
	{/if}
{:else}
	<!-- Legal Template Editor -->
	<div class="glass-panel" style="padding: 1.75rem; border-radius: var(--radius-lg); max-width: 900px;">
		<h3 class="font-display" style="font-size: 1.25rem; margin-bottom: 0.5rem;">Official Liability Waiver Agreement</h3>
		<p class="editor-hint" style="margin-bottom: 1.25rem;">
			Modifying the text creates an immutable new template version. All future digital signatures will reference the new version's SHA-256 fingerprint.
		</p>

		{#if templateSuccessMsg}
			<div class="alert-success" style="margin-bottom: 1rem;">
				✓ {templateSuccessMsg}
			</div>
		{/if}

		{#if templateErrorMsg}
			<div class="alert-error" style="margin-bottom: 1rem;">
				⚠️ {templateErrorMsg}
			</div>
		{/if}

		{#if editingTemplate}
			<form onsubmit={handleSaveTemplate}>
				<div class="form-group">
					<label class="form-label" for="tpl-title">Waiver Document Title</label>
					<input id="tpl-title" type="text" class="form-input" bind:value={templateTitle} required />
				</div>

				<div class="form-group" style="margin-top: 1rem;">
					<label class="form-label" for="tpl-body">Agreement Text (Markdown Supported)</label>
					<textarea
						id="tpl-body"
						class="form-input font-mono"
						style="min-height: 320px; font-size: 0.85rem; line-height: 1.6;"
						bind:value={templateMarkdown}
						required
					></textarea>
				</div>

				<div style="margin-top: 1rem; padding: 0.75rem 1rem; background: rgba(10, 15, 25, 0.6); border-radius: var(--radius-sm); border: 1px solid var(--border-color); display: flex; justify-content: space-between; align-items: center;">
					<span style="font-size: 0.75rem; color: var(--text-muted); font-family: monospace;">
						Current Version: v{editingTemplate.versionNumber} • Hash: {editingTemplate.sha256Hash?.substring(0, 16)}...
					</span>
					<span class="badge badge-available font-display">Active Template</span>
				</div>

				<div style="margin-top: 1.5rem; display: flex; justify-content: flex-end;">
					<button type="submit" class="btn btn-primary font-display" disabled={isSavingTemplate}>
						{isSavingTemplate ? 'Publishing Version...' : '💾 Publish New Waiver Version'}
					</button>
				</div>
			</form>
		{/if}
	</div>
{/if}

<!-- SIGNATURE DETAIL MODAL -->
{#if selectedWaiver}
	<div class="modal-overlay" role="button" tabindex="0" onclick={() => (selectedWaiver = null)} onkeydown={(e) => { if (e.key === 'Escape') selectedWaiver = null; }}>
		<div class="modal-card glass-panel" style="max-width: 500px;" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.stopPropagation()}>
			<div class="modal-header-row">
				<div>
					<h3 class="modal-title font-display">Signature Inspection</h3>
					<p class="editor-hint" style="margin-bottom: 0;">Signer: {selectedWaiver.signerFirstName} {selectedWaiver.signerLastName}</p>
				</div>
				<button type="button" class="btn-clear" onclick={() => (selectedWaiver = null)}>✕</button>
			</div>

			<div style="position: relative; text-align: center; padding: 1.5rem; background: #0f141c; border: 2px dashed var(--border-color); border-radius: var(--radius-md); margin-top: 1rem; overflow: hidden;">
				<img src={selectedWaiver.signatureImagePngBase64} alt="Signature Full" style="max-width: 100%; max-height: 180px; display: inline-block; position: relative; z-index: 1;" />
				<div style="position: absolute; bottom: 25px; left: 20px; right: 20px; height: 1px; background: rgba(148, 163, 184, 0.2); pointer-events: none;"></div>
			</div>

			<div style="margin-top: 1rem; font-size: 0.85rem; color: var(--text-secondary); display: flex; flex-direction: column; gap: 0.4rem;">
				<div>Signed: <strong style="color: #fff;">{new Date(selectedWaiver.signedAtUtc).toLocaleString()}</strong></div>
				<div>Signer Email: <strong style="color: #fff;">{selectedWaiver.signerEmail}</strong></div>
				<div>DOB: <strong style="color: #fff;">{selectedWaiver.dateOfBirth}</strong></div>
				{#if selectedWaiver.isGuardianSigning && selectedWaiver.minorsCoveredJson}
					{@const minors = formatMinors(selectedWaiver.minorsCoveredJson)}
					{#if minors.length > 0}
						<div>Covered Minors: <strong style="color: var(--accent-amber);">{minors.join(', ')}</strong></div>
					{/if}
				{/if}
			</div>

			<div class="modal-actions" style="margin-top: 1.5rem;">
				<button
					type="button"
					class="btn btn-primary font-display"
					disabled={downloadingWaiverId === selectedWaiver.id}
					onclick={() => downloadWaiverPdf(selectedWaiver!.id, `${selectedWaiver!.signerLastName}_${selectedWaiver!.signerFirstName}`)}
				>
					{downloadingWaiverId === selectedWaiver.id ? '⏳ Downloading PDF...' : '📄 Download Full Audit PDF'}
				</button>
				<button type="button" class="btn btn-secondary" onclick={() => (selectedWaiver = null)}>
					Close
				</button>
			</div>
		</div>
	</div>
{/if}
