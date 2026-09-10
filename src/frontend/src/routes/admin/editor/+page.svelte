<script lang="ts">
	import { onMount } from 'svelte';
	import { venueState } from '$lib/stores/venueState.svelte';
	import {
		getApiAdminBookingConfigVenueByVenueId,
		putApiAdminBookingConfigVenueByVenueId
	} from '$lib/api/client';
	import type { BookingConfigDto } from '$lib/api/generated/types.gen';

	interface PackageItem {
		id: string;
		name: string;
		description: string;
		pricePerPersonCents: number;
		isDefault: boolean;
	}

	interface DiscountRuleItem {
		id: string;
		name: string;
		type: 'group_size' | 'promo_code';
		minPartySize?: number;
		code?: string;
		discountPercent: number;
		discountAmountCents: number;
		autoApply: boolean;
	}

	interface BookingTypeItem {
		id: string;
		name: string;
		description: string;
		allowAfterHoursBooking: boolean;
		allowOffDaysBooking: boolean;
		minPartySize: number;
	}

	interface AddonItem {
		id: string;
		name: string;
		description: string;
		priceCents: number;
		priceType: 'flat' | 'per_person';
	}

	interface CustomFieldItem {
		id: string;
		label: string;
		type: 'text' | 'select' | 'checkbox';
		options?: string[];
		required: boolean;
	}

	let bookingConfig = $state<BookingConfigDto | null>(null);
	let isLoading = $state(true);
	let isSaving = $state(false);
	let successMessage = $state<string | null>(null);
	let errorMessage = $state<string | null>(null);
	let copiedEmbedCode = $state(false);

	// View mode: Visual Builder vs Advanced JSON
	let viewMode = $state<'visual' | 'json'>('visual');
	let activeBuilderTab = $state<'packages' | 'discounts' | 'bookingTypes' | 'addons' | 'customFields'>('packages');

	// Parsed Visual Data Models
	let packages = $state<PackageItem[]>([]);
	let discountRules = $state<DiscountRuleItem[]>([]);
	let bookingTypes = $state<BookingTypeItem[]>([]);
	let addons = $state<AddonItem[]>([]);
	let customFields = $state<CustomFieldItem[]>([]);

	// New Item Draft Forms
	let showNewPackageModal = $state(false);
	let newPkgName = $state('');
	let newPkgDesc = $state('');
	let newPkgPriceDollars = $state(35);
	let newPkgIsDefault = $state(false);

	let showNewDiscountModal = $state(false);
	let newDiscName = $state('');
	let newDiscType = $state<'group_size' | 'promo_code'>('promo_code');
	let newDiscCode = $state('');
	let newDiscMinParty = $state(8);
	let newDiscPercent = $state(10);
	let newDiscAutoApply = $state(false);

	let showNewBookingTypeModal = $state(false);
	let newBtName = $state('');
	let newBtDesc = $state('');
	let newBtMinParty = $state(2);
	let newBtAllowAfterHours = $state(false);
	let newBtAllowOffDays = $state(false);

	let showNewAddonModal = $state(false);
	let newAddonName = $state('');
	let newAddonDesc = $state('');
	let newAddonPriceDollars = $state(25);
	let newAddonPriceType = $state<'flat' | 'per_person'>('flat');

	let showNewCustomFieldModal = $state(false);
	let newFieldLabel = $state('');
	let newFieldType = $state<'text' | 'select' | 'checkbox'>('text');
	let newFieldOptionsRaw = $state('First Time, Intermediate, League Member');
	let newFieldRequired = $state(false);

	function parseAllJson(cfg: BookingConfigDto) {
		try {
			packages = cfg.packagesJson ? JSON.parse(cfg.packagesJson) : [];
		} catch {
			packages = [];
		}
		try {
			discountRules = cfg.discountRulesJson ? JSON.parse(cfg.discountRulesJson) : [];
		} catch {
			discountRules = [];
		}
		try {
			bookingTypes = cfg.bookingTypesJson ? JSON.parse(cfg.bookingTypesJson) : [];
		} catch {
			bookingTypes = [];
		}
		try {
			addons = cfg.addonsJson ? JSON.parse(cfg.addonsJson) : [];
		} catch {
			addons = [];
		}
		try {
			customFields = cfg.customFieldsJson ? JSON.parse(cfg.customFieldsJson) : [];
		} catch {
			customFields = [];
		}
	}

	function syncJsonFromVisual() {
		if (!bookingConfig) return;
		bookingConfig.packagesJson = JSON.stringify(packages, null, 2);
		bookingConfig.discountRulesJson = JSON.stringify(discountRules, null, 2);
		bookingConfig.bookingTypesJson = JSON.stringify(bookingTypes, null, 2);
		bookingConfig.addonsJson = JSON.stringify(addons, null, 2);
		bookingConfig.customFieldsJson = JSON.stringify(customFields, null, 2);
	}

	async function loadConfig() {
		if (!venueState.selectedVenue) return;
		isLoading = true;
		try {
			const res = await getApiAdminBookingConfigVenueByVenueId({
				path: { venueId: venueState.selectedVenue.id }
			});
			bookingConfig = res.data || null;
			if (bookingConfig) {
				parseAllJson(bookingConfig);
			}
		} catch (e) {
			console.error('Failed to load booking config', e);
		} finally {
			isLoading = false;
		}
	}

	async function handleSaveConfig(e?: SubmitEvent) {
		if (e) e.preventDefault();
		if (!venueState.selectedVenue || !bookingConfig) return;

		// If in visual mode, sync JSON prior to save
		if (viewMode === 'visual') {
			syncJsonFromVisual();
		} else {
			// In raw JSON mode, parse back to update visual lists
			parseAllJson(bookingConfig);
		}

		isSaving = true;
		successMessage = null;
		errorMessage = null;

		try {
			const res = await putApiAdminBookingConfigVenueByVenueId({
				path: { venueId: venueState.selectedVenue.id },
				body: {
					minPartySize: Number(bookingConfig.minPartySize),
					maxPartySize: Number(bookingConfig.maxPartySize),
					slotDurationsMinutes: bookingConfig.slotDurationsMinutes,
					turnaroundBufferMinutes: Number(bookingConfig.turnaroundBufferMinutes),
					pricingModel: bookingConfig.pricingModel,
					basePriceCents: Number(bookingConfig.basePriceCents),
					peakPriceCents: Number(bookingConfig.peakPriceCents),
					depositType: bookingConfig.depositType,
					depositAmountCents: Number(bookingConfig.depositAmountCents),
					editorThemeJson: bookingConfig.editorThemeJson,
					customFieldsJson: bookingConfig.customFieldsJson,
					packagesJson: bookingConfig.packagesJson,
					discountRulesJson: bookingConfig.discountRulesJson,
					bookingTypesJson: bookingConfig.bookingTypesJson,
					addonsJson: bookingConfig.addonsJson,
					cancellationPolicy: bookingConfig.cancellationPolicy
				}
			});

			if (res.data) {
				successMessage = 'Booking engine configuration saved successfully!';
				bookingConfig = res.data;
				parseAllJson(bookingConfig);
			} else {
				errorMessage = 'Failed to save configuration.';
			}
		} catch (err: any) {
			errorMessage = err?.message || 'Error saving configuration.';
		} finally {
			isSaving = false;
		}
	}

	// Package visual CRUD
	function addPackage() {
		if (!newPkgName.trim()) return;
		const id = 'pkg_' + newPkgName.toLowerCase().replace(/[^a-z0-9]/g, '_');
		if (newPkgIsDefault) {
			packages.forEach((p) => (p.isDefault = false));
		}
		packages = [
			...packages,
			{
				id,
				name: newPkgName.trim(),
				description: newPkgDesc.trim() || 'Exciting target throwing experience',
				pricePerPersonCents: Math.round(Number(newPkgPriceDollars) * 100),
				isDefault: newPkgIsDefault
			}
		];
		syncJsonFromVisual();
		showNewPackageModal = false;
		newPkgName = '';
		newPkgDesc = '';
		newPkgPriceDollars = 35;
		newPkgIsDefault = false;
	}

	function deletePackage(id: string) {
		packages = packages.filter((p) => p.id !== id);
		syncJsonFromVisual();
	}

	function setDefaultPackage(id: string) {
		packages = packages.map((p) => ({
			...p,
			isDefault: p.id === id
		}));
		syncJsonFromVisual();
	}

	// Discount visual CRUD
	function addDiscount() {
		if (!newDiscName.trim()) return;
		const id = 'disc_' + newDiscName.toLowerCase().replace(/[^a-z0-9]/g, '_');
		discountRules = [
			...discountRules,
			{
				id,
				name: newDiscName.trim(),
				type: newDiscType,
				code: newDiscType === 'promo_code' ? newDiscCode.trim().toUpperCase() : undefined,
				minPartySize: newDiscType === 'group_size' ? Number(newDiscMinParty) : undefined,
				discountPercent: Number(newDiscPercent),
				discountAmountCents: 0,
				autoApply: newDiscAutoApply
			}
		];
		syncJsonFromVisual();
		showNewDiscountModal = false;
		newDiscName = '';
		newDiscCode = '';
		newDiscPercent = 10;
	}

	function deleteDiscount(id: string) {
		discountRules = discountRules.filter((d) => d.id !== id);
		syncJsonFromVisual();
	}

	// Booking Type visual CRUD
	function addBookingType() {
		if (!newBtName.trim()) return;
		const id = 'bt_' + newBtName.toLowerCase().replace(/[^a-z0-9]/g, '_');
		bookingTypes = [
			...bookingTypes,
			{
				id,
				name: newBtName.trim(),
				description: newBtDesc.trim() || 'Custom booking format',
				allowAfterHoursBooking: newBtAllowAfterHours,
				allowOffDaysBooking: newBtAllowOffDays,
				minPartySize: Number(newBtMinParty)
			}
		];
		syncJsonFromVisual();
		showNewBookingTypeModal = false;
		newBtName = '';
		newBtDesc = '';
	}

	function deleteBookingType(id: string) {
		bookingTypes = bookingTypes.filter((b) => b.id !== id);
		syncJsonFromVisual();
	}

	// Addon visual CRUD
	function addAddon() {
		if (!newAddonName.trim()) return;
		const id = 'addon_' + newAddonName.toLowerCase().replace(/[^a-z0-9]/g, '_');
		addons = [
			...addons,
			{
				id,
				name: newAddonName.trim(),
				description: newAddonDesc.trim() || 'Optional amenity upgrade',
				priceCents: Math.round(Number(newAddonPriceDollars) * 100),
				priceType: newAddonPriceType
			}
		];
		syncJsonFromVisual();
		showNewAddonModal = false;
		newAddonName = '';
		newAddonDesc = '';
		newAddonPriceDollars = 25;
	}

	function deleteAddon(id: string) {
		addons = addons.filter((a) => a.id !== id);
		syncJsonFromVisual();
	}

	// Custom field visual CRUD
	function addCustomField() {
		if (!newFieldLabel.trim()) return;
		const id = 'field_' + newFieldLabel.toLowerCase().replace(/[^a-z0-9]/g, '_');
		const opts = newFieldType === 'select'
			? newFieldOptionsRaw.split(',').map((s) => s.trim()).filter(Boolean)
			: undefined;

		customFields = [
			...customFields,
			{
				id,
				label: newFieldLabel.trim(),
				type: newFieldType,
				options: opts,
				required: newFieldRequired
			}
		];
		syncJsonFromVisual();
		showNewCustomFieldModal = false;
		newFieldLabel = '';
	}

	function deleteCustomField(id: string) {
		customFields = customFields.filter((f) => f.id !== id);
		syncJsonFromVisual();
	}

	function copyEmbedCode() {
		const slug = venueState.selectedVenue?.slug ?? 'downtown';
		const origin = typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173';
		const snippet = `<iframe id="venueaxe-booking" data-venueaxe-widget src="${origin}/book/${slug}?embed=true" width="100%" frameborder="0" scrolling="no"></iframe>\n<script src="${origin}/venueaxe-widget.js" async><\/script>`;
		navigator.clipboard.writeText(snippet);
		copiedEmbedCode = true;
		setTimeout(() => (copiedEmbedCode = false), 3000);
	}

	$effect(() => {
		if (venueState.selectedVenue) {
			loadConfig();
		}
	});

	onMount(() => {
		loadConfig();
	});
</script>

<svelte:head>
	<title>Visual Booking Page Editor | VenueAxe Admin</title>
</svelte:head>

<div class="tab-header">
	<div>
		<h2 class="font-display">Visual Booking Page Editor & Widget</h2>
		<p class="tab-subtitle">Configure packages, pricing rules, promo discounts, custom questions, and embed snippets</p>
	</div>
	<div style="display: flex; gap: 0.75rem; align-items: center;">
		<!-- View Mode Toggle -->
		<div class="view-mode-toggle" style="background: rgba(255, 255, 255, 0.06); padding: 3px; border-radius: var(--radius-sm); display: inline-flex;">
			<button
				type="button"
				class="mode-btn"
				class:active={viewMode === 'visual'}
				onclick={() => {
					if (bookingConfig && viewMode === 'json') parseAllJson(bookingConfig);
					viewMode = 'visual';
				}}
			>
				✨ Visual Builder
			</button>
			<button
				type="button"
				class="mode-btn"
				class:active={viewMode === 'json'}
				onclick={() => {
					if (bookingConfig && viewMode === 'visual') syncJsonFromVisual();
					viewMode = 'json';
				}}
			>
				🛠️ Advanced JSON
			</button>
		</div>

		<button
			type="button"
			class="btn btn-primary font-display"
			disabled={isSaving || !bookingConfig}
			onclick={() => handleSaveConfig()}
		>
			{isSaving ? 'Saving...' : '💾 Save Changes'}
		</button>
	</div>
</div>

{#if successMessage}
	<div class="alert-success" style="margin-bottom: 1.25rem;">
		✓ {successMessage}
	</div>
{/if}

{#if errorMessage}
	<div class="alert-error" style="margin-bottom: 1.25rem;">
		⚠️ {errorMessage}
	</div>
{/if}

{#if isLoading}
	<div style="padding: 3rem; text-align: center; color: var(--text-secondary);">
		<p class="font-display">Loading booking configuration...</p>
	</div>
{:else if bookingConfig}
	<form id="config-form" onsubmit={handleSaveConfig} style="display: flex; flex-direction: column; gap: 1.5rem;">
		<!-- Capacity & Rules -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<h3 class="font-display" style="font-size: 1.2rem; margin-bottom: 1rem; color: var(--accent-amber);">
				🏟️ Capacity & Duration Rules
			</h3>
			<div class="form-row-3">
				<div class="form-group">
					<label class="form-label" for="cfg-min">Min Party Size</label>
					<input id="cfg-min" type="number" min="1" class="form-input" bind:value={bookingConfig.minPartySize} required />
				</div>
				<div class="form-group">
					<label class="form-label" for="cfg-max">Max Party Size</label>
					<input id="cfg-max" type="number" min="1" max="100" class="form-input" bind:value={bookingConfig.maxPartySize} required />
				</div>
				<div class="form-group">
					<label class="form-label" for="cfg-buffer">Turnaround Buffer (Minutes)</label>
					<input id="cfg-buffer" type="number" min="0" max="60" step="5" class="form-input" bind:value={bookingConfig.turnaroundBufferMinutes} required />
				</div>
			</div>
		</div>

		<!-- Pricing & Deposit -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<h3 class="font-display" style="font-size: 1.2rem; margin-bottom: 1rem; color: var(--accent-amber);">
				💵 Pricing & Deposit Structure
			</h3>
			<div class="form-row-2">
				<div class="form-group">
					<label class="form-label" for="cfg-base">Standard Hourly Base Price ($ / Thrower)</label>
					<input
						id="cfg-base"
						type="number"
						step="0.01"
						min="0"
						class="form-input"
						value={Number(bookingConfig.basePriceCents) / 100}
						oninput={(e) => (bookingConfig!.basePriceCents = Math.round(Number(e.currentTarget.value) * 100))}
						required
					/>
				</div>
				<div class="form-group">
					<label class="form-label" for="cfg-peak">Peak Weekend Price ($ / Thrower)</label>
					<input
						id="cfg-peak"
						type="number"
						step="0.01"
						min="0"
						class="form-input"
						value={Number(bookingConfig.peakPriceCents) / 100}
						oninput={(e) => (bookingConfig!.peakPriceCents = Math.round(Number(e.currentTarget.value) * 100))}
						required
					/>
				</div>
			</div>
		</div>

		{#if viewMode === 'visual'}
			<!-- Visual Builder Section with Subtabs -->
			<div class="glass-panel" style="padding: 1.75rem;">
				<div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border-color); padding-bottom: 1rem; margin-bottom: 1.5rem; flex-wrap: wrap; gap: 1rem;">
					<!-- Builder Navigation Tabs -->
					<div class="subtabs-bar" style="display: flex; gap: 0.5rem; flex-wrap: wrap;">
						<button
							type="button"
							class="subtab-btn font-display"
							class:active={activeBuilderTab === 'packages'}
							onclick={() => (activeBuilderTab = 'packages')}
						>
							🎁 Packages ({packages.length})
						</button>
						<button
							type="button"
							class="subtab-btn font-display"
							class:active={activeBuilderTab === 'discounts'}
							onclick={() => (activeBuilderTab = 'discounts')}
						>
							🏷️ Discounts & Promo ({discountRules.length})
						</button>
						<button
							type="button"
							class="subtab-btn font-display"
							class:active={activeBuilderTab === 'bookingTypes'}
							onclick={() => (activeBuilderTab = 'bookingTypes')}
						>
							🎯 Booking Formats ({bookingTypes.length})
						</button>
						<button
							type="button"
							class="subtab-btn font-display"
							class:active={activeBuilderTab === 'addons'}
							onclick={() => (activeBuilderTab = 'addons')}
						>
							🍕 Add-Ons ({addons.length})
						</button>
						<button
							type="button"
							class="subtab-btn font-display"
							class:active={activeBuilderTab === 'customFields'}
							onclick={() => (activeBuilderTab = 'customFields')}
						>
							❓ Intake Questions ({customFields.length})
						</button>
					</div>

					<!-- Contextual Action Button -->
					{#if activeBuilderTab === 'packages'}
						<button type="button" class="btn btn-secondary btn-sm font-display" onclick={() => (showNewPackageModal = true)}>
							➕ Add Package
						</button>
					{:else if activeBuilderTab === 'discounts'}
						<button type="button" class="btn btn-secondary btn-sm font-display" onclick={() => (showNewDiscountModal = true)}>
							➕ Add Discount Rule
						</button>
					{:else if activeBuilderTab === 'bookingTypes'}
						<button type="button" class="btn btn-secondary btn-sm font-display" onclick={() => (showNewBookingTypeModal = true)}>
							➕ Add Booking Format
						</button>
					{:else if activeBuilderTab === 'addons'}
						<button type="button" class="btn btn-secondary btn-sm font-display" onclick={() => (showNewAddonModal = true)}>
							➕ Add Upgrades / Add-On
						</button>
					{:else if activeBuilderTab === 'customFields'}
						<button type="button" class="btn btn-secondary btn-sm font-display" onclick={() => (showNewCustomFieldModal = true)}>
							➕ Add Intake Question
						</button>
					{/if}
				</div>

				<!-- 1. Packages Tab -->
				{#if activeBuilderTab === 'packages'}
					<div class="cards-grid" style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1.25rem;">
						{#each packages as pkg (pkg.id)}
							<div class="builder-card" style="background: rgba(15, 23, 42, 0.7); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1.25rem; display: flex; flex-direction: column; justify-content: space-between;">
								<div>
									<div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 0.5rem; margin-bottom: 0.5rem;">
										<h4 class="font-display" style="font-size: 1.1rem; color: #f8fafc; margin: 0;">{pkg.name}</h4>
										{#if pkg.isDefault}
											<span style="background: rgba(245, 158, 11, 0.2); border: 1px solid var(--accent-amber); color: var(--accent-amber); font-size: 0.7rem; padding: 2px 6px; border-radius: 4px; font-weight: 700;">DEFAULT</span>
										{/if}
									</div>
									<p style="color: var(--text-secondary); font-size: 0.85rem; line-height: 1.4; margin-bottom: 1rem;">{pkg.description}</p>
								</div>
								<div style="display: flex; justify-content: space-between; align-items: center; border-top: 1px solid rgba(255, 255, 255, 0.06); padding-top: 0.75rem;">
									<div style="font-size: 1.2rem; font-weight: 700; color: var(--accent-cyan);">
										${(pkg.pricePerPersonCents / 100).toFixed(2)} <span style="font-size: 0.75rem; color: var(--text-secondary); font-weight: normal;">/ person</span>
									</div>
									<div style="display: flex; gap: 0.5rem;">
										{#if !pkg.isDefault}
											<button type="button" class="btn-clear" style="font-size: 0.8rem; color: var(--accent-amber);" onclick={() => setDefaultPackage(pkg.id)}>Set Default</button>
										{/if}
										<button type="button" class="btn-clear" style="font-size: 0.8rem; color: #ef4444;" onclick={() => deletePackage(pkg.id)}>✕ Delete</button>
									</div>
								</div>
							</div>
						{/each}
					</div>
				{/if}

				<!-- 2. Discounts Tab -->
				{#if activeBuilderTab === 'discounts'}
					<div class="cards-grid" style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1.25rem;">
						{#each discountRules as rule (rule.id)}
							<div class="builder-card" style="background: rgba(15, 23, 42, 0.7); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1.25rem; display: flex; flex-direction: column; justify-content: space-between;">
								<div>
									<div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 0.5rem; margin-bottom: 0.5rem;">
										<h4 class="font-display" style="font-size: 1.05rem; color: #f8fafc; margin: 0;">{rule.name}</h4>
										<span style="background: rgba(6, 182, 212, 0.2); border: 1px solid var(--accent-cyan); color: var(--accent-cyan); font-size: 0.7rem; padding: 2px 6px; border-radius: 4px; font-weight: 700;">
											{rule.type === 'promo_code' ? 'PROMO' : 'GROUP SIZE'}
										</span>
									</div>
									<div style="font-size: 0.85rem; color: var(--text-secondary); margin-bottom: 1rem;">
										{#if rule.type === 'promo_code'}
											Promo Code: <strong class="font-mono" style="color: #fff;">{rule.code}</strong>
										{:else}
											Applies automatically for <strong style="color: #fff;">{rule.minPartySize}+ Throwers</strong>
										{/if}
									</div>
								</div>
								<div style="display: flex; justify-content: space-between; align-items: center; border-top: 1px solid rgba(255, 255, 255, 0.06); padding-top: 0.75rem;">
									<div style="font-size: 1.2rem; font-weight: 700; color: #10b981;">
										{rule.discountPercent}% OFF
									</div>
									<button type="button" class="btn-clear" style="font-size: 0.8rem; color: #ef4444;" onclick={() => deleteDiscount(rule.id)}>✕ Delete</button>
								</div>
							</div>
						{/each}
					</div>
				{/if}

				<!-- 3. Booking Formats Tab -->
				{#if activeBuilderTab === 'bookingTypes'}
					<div class="cards-grid" style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1.25rem;">
						{#each bookingTypes as bt (bt.id)}
							<div class="builder-card" style="background: rgba(15, 23, 42, 0.7); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1.25rem; display: flex; flex-direction: column; justify-content: space-between;">
								<div>
									<h4 class="font-display" style="font-size: 1.05rem; color: #f8fafc; margin-bottom: 0.5rem;">{bt.name}</h4>
									<p style="color: var(--text-secondary); font-size: 0.85rem; line-height: 1.4; margin-bottom: 1rem;">{bt.description}</p>
									<div style="font-size: 0.78rem; display: flex; flex-direction: column; gap: 0.25rem; color: var(--text-secondary);">
										<div>Min Party: <strong style="color: #fff;">{bt.minPartySize}</strong></div>
										<div>After-Hours: <strong style="color: {bt.allowAfterHoursBooking ? '#10b981' : '#94a3b8'};">{bt.allowAfterHoursBooking ? 'Yes' : 'No'}</strong></div>
									</div>
								</div>
								<div style="display: flex; justify-content: flex-end; border-top: 1px solid rgba(255, 255, 255, 0.06); padding-top: 0.75rem; margin-top: 1rem;">
									<button type="button" class="btn-clear" style="font-size: 0.8rem; color: #ef4444;" onclick={() => deleteBookingType(bt.id)}>✕ Delete</button>
								</div>
							</div>
						{/each}
					</div>
				{/if}

				<!-- 4. Add-Ons Tab -->
				{#if activeBuilderTab === 'addons'}
					<div class="cards-grid" style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1.25rem;">
						{#each addons as item (item.id)}
							<div class="builder-card" style="background: rgba(15, 23, 42, 0.7); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1.25rem; display: flex; flex-direction: column; justify-content: space-between;">
								<div>
									<h4 class="font-display" style="font-size: 1.05rem; color: #f8fafc; margin-bottom: 0.5rem;">{item.name}</h4>
									<p style="color: var(--text-secondary); font-size: 0.85rem; line-height: 1.4; margin-bottom: 1rem;">{item.description}</p>
								</div>
								<div style="display: flex; justify-content: space-between; align-items: center; border-top: 1px solid rgba(255, 255, 255, 0.06); padding-top: 0.75rem;">
									<div style="font-size: 1.15rem; font-weight: 700; color: var(--accent-amber);">
										${(item.priceCents / 100).toFixed(2)} <span style="font-size: 0.75rem; color: var(--text-secondary); font-weight: normal;">({item.priceType})</span>
									</div>
									<button type="button" class="btn-clear" style="font-size: 0.8rem; color: #ef4444;" onclick={() => deleteAddon(item.id)}>✕ Delete</button>
								</div>
							</div>
						{/each}
					</div>
				{/if}

				<!-- 5. Custom Fields Tab -->
				{#if activeBuilderTab === 'customFields'}
					<div class="cards-grid" style="display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 1.25rem;">
						{#each customFields as field (field.id)}
							<div class="builder-card" style="background: rgba(15, 23, 42, 0.7); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1.25rem; display: flex; flex-direction: column; justify-content: space-between;">
								<div>
									<div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 0.5rem; margin-bottom: 0.5rem;">
										<h4 class="font-display" style="font-size: 1rem; color: #f8fafc; margin: 0;">{field.label}</h4>
										<span style="font-size: 0.7rem; background: rgba(255, 255, 255, 0.08); padding: 2px 6px; border-radius: 4px; text-transform: uppercase;">{field.type}</span>
									</div>
									{#if field.options && field.options.length > 0}
										<div style="font-size: 0.8rem; color: var(--text-secondary); margin-bottom: 0.75rem;">
											Options: {field.options.join(', ')}
										</div>
									{/if}
								</div>
								<div style="display: flex; justify-content: space-between; align-items: center; border-top: 1px solid rgba(255, 255, 255, 0.06); padding-top: 0.75rem;">
									<span style="font-size: 0.75rem; color: {field.required ? '#ef4444' : 'var(--text-secondary)'};">
										{field.required ? 'Required' : 'Optional'}
									</span>
									<button type="button" class="btn-clear" style="font-size: 0.8rem; color: #ef4444;" onclick={() => deleteCustomField(field.id)}>✕ Delete</button>
								</div>
							</div>
						{/each}
					</div>
				{/if}
			</div>
		{:else}
			<!-- Advanced JSON Mode -->
			<div class="form-row-2">
				<div class="glass-panel" style="padding: 1.75rem;">
					<h3 class="font-display" style="font-size: 1.1rem; margin-bottom: 0.25rem;">🎁 Pre-Built Packages (JSON)</h3>
					<p class="editor-hint" style="margin-bottom: 0.75rem;">Tiered packages combining lanes, duration, and perks.</p>
					<textarea class="form-input font-mono" rows="8" style="font-size: 0.8rem;" bind:value={bookingConfig.packagesJson}></textarea>
				</div>
				<div class="glass-panel" style="padding: 1.75rem;">
					<h3 class="font-display" style="font-size: 1.1rem; margin-bottom: 0.25rem;">🏷️ Discount & Promo Rules (JSON)</h3>
					<p class="editor-hint" style="margin-bottom: 0.75rem;">Volume discount tiers and promo codes.</p>
					<textarea class="form-input font-mono" rows="8" style="font-size: 0.8rem;" bind:value={bookingConfig.discountRulesJson}></textarea>
				</div>
			</div>

			<div class="form-row-2">
				<div class="glass-panel" style="padding: 1.75rem;">
					<h3 class="font-display" style="font-size: 1.1rem; margin-bottom: 0.25rem;">🎯 Booking Types & Overrides (JSON)</h3>
					<p class="editor-hint" style="margin-bottom: 0.75rem;">Special event formats with custom duration.</p>
					<textarea class="form-input font-mono" rows="8" style="font-size: 0.8rem;" bind:value={bookingConfig.bookingTypesJson}></textarea>
				</div>
				<div class="glass-panel" style="padding: 1.75rem;">
					<h3 class="font-display" style="font-size: 1.1rem; margin-bottom: 0.25rem;">🍕 Add-On Upgrades Catalog (JSON)</h3>
					<p class="editor-hint" style="margin-bottom: 0.75rem;">Extra food, beverage, coaching, and swag.</p>
					<textarea class="form-input font-mono" rows="8" style="font-size: 0.8rem;" bind:value={bookingConfig.addonsJson}></textarea>
				</div>
			</div>

			<div class="glass-panel" style="padding: 1.75rem;">
				<h3 class="font-display" style="font-size: 1.1rem; margin-bottom: 0.25rem;">❓ Custom Intake Questions (JSON)</h3>
				<p class="editor-hint" style="margin-bottom: 0.75rem;">Custom guest questions asked during booking checkout.</p>
				<textarea class="form-input font-mono" rows="6" style="font-size: 0.8rem;" bind:value={bookingConfig.customFieldsJson}></textarea>
			</div>
		{/if}

		<!-- Embed Snippet Box -->
		<div class="glass-panel" style="padding: 1.75rem;">
			<div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; margin-bottom: 1rem; flex-wrap: wrap;">
				<div>
					<h3 class="font-display" style="font-size: 1.2rem; margin-bottom: 0.25rem; color: var(--accent-cyan);">
						🌐 Embeddable Booking Widget SDK
					</h3>
					<p class="editor-hint" style="margin-bottom: 0;">
						Embed this responsive booking widget on any external website (WordPress, Webflow, Squarespace, Shopify).
					</p>
				</div>
				<button type="button" class="btn btn-secondary font-display" onclick={copyEmbedCode}>
					{copiedEmbedCode ? '✓ Copied to Clipboard!' : '📋 Copy Embed Snippet'}
				</button>
			</div>

			<pre style="background: rgba(10, 15, 25, 0.9); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1rem; font-size: 0.82rem; color: #38bdf8; overflow-x: auto; white-space: pre-wrap; font-family: monospace;"><code>&lt;iframe id="venueaxe-booking" data-venueaxe-widget src="{typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173'}/book/{venueState.selectedVenue?.slug ?? 'downtown'}?embed=true" width="100%" frameborder="0" scrolling="no"&gt;&lt;/iframe&gt;
&lt;script src="{typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173'}/venueaxe-widget.js" async&gt;&lt;/script&gt;</code></pre>
		</div>

		<div style="display: flex; justify-content: flex-end;">
			<button type="submit" class="btn btn-primary font-display" style="padding: 0.85rem 2rem; font-size: 1rem;" disabled={isSaving}>
				{isSaving ? 'Saving Changes...' : '💾 Save Configuration Changes'}
			</button>
		</div>
	</form>
{/if}

<!-- Add Package Modal -->
{#if showNewPackageModal}
	<div class="modal-overlay" role="button" tabindex="0" onclick={() => (showNewPackageModal = false)} onkeydown={(e) => { if (e.key === 'Escape') showNewPackageModal = false; }}>
		<div class="modal-card glass-panel" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.stopPropagation()}>
			<div class="modal-header-row">
				<h3 class="modal-title font-display">Add Throwing Package</h3>
				<button type="button" class="btn-clear" onclick={() => (showNewPackageModal = false)}>✕</button>
			</div>
			<div class="form-group" style="margin-top: 1rem;">
				<label class="form-label" for="new-pkg-name">Package Name *</label>
				<input id="new-pkg-name" type="text" class="form-input" bind:value={newPkgName} placeholder="e.g. Cosmic Glow Axe" required />
			</div>
			<div class="form-group" style="margin-top: 0.75rem;">
				<label class="form-label" for="new-pkg-desc">Description</label>
				<input id="new-pkg-desc" type="text" class="form-input" bind:value={newPkgDesc} placeholder="Blacklight UV throwing with glow axes" />
			</div>
			<div class="form-group" style="margin-top: 0.75rem;">
				<label class="form-label" for="new-pkg-price">Price Per Thrower ($)</label>
				<input id="new-pkg-price" type="number" step="0.50" min="0" class="form-input" bind:value={newPkgPriceDollars} required />
			</div>
			<div style="margin-top: 0.75rem; display: flex; align-items: center; gap: 0.5rem;">
				<input id="new-pkg-def" type="checkbox" bind:checked={newPkgIsDefault} />
				<label for="new-pkg-def" style="font-size: 0.85rem; color: #fff; cursor: pointer;">Set as default selected package</label>
			</div>
			<div class="modal-actions" style="margin-top: 1.5rem;">
				<button type="button" class="btn btn-secondary" onclick={() => (showNewPackageModal = false)}>Cancel</button>
				<button type="button" class="btn btn-primary font-display" onclick={addPackage}>+ Add Package</button>
			</div>
		</div>
	</div>
{/if}

<!-- Add Discount Modal -->
{#if showNewDiscountModal}
	<div class="modal-overlay" role="button" tabindex="0" onclick={() => (showNewDiscountModal = false)} onkeydown={(e) => { if (e.key === 'Escape') showNewDiscountModal = false; }}>
		<div class="modal-card glass-panel" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.stopPropagation()}>
			<div class="modal-header-row">
				<h3 class="modal-title font-display">Add Discount / Promo Rule</h3>
				<button type="button" class="btn-clear" onclick={() => (showNewDiscountModal = false)}>✕</button>
			</div>
			<div class="form-group" style="margin-top: 1rem;">
				<label class="form-label" for="new-disc-name">Rule Name *</label>
				<input id="new-disc-name" type="text" class="form-input" bind:value={newDiscName} placeholder="e.g. Hero Military Discount" required />
			</div>
			<div class="form-row-2" style="margin-top: 0.75rem;">
				<div class="form-group">
					<label class="form-label" for="new-disc-type">Rule Type</label>
					<select id="new-disc-type" class="form-input" bind:value={newDiscType}>
						<option value="promo_code">Promo Code</option>
						<option value="group_size">Group Size Tier</option>
					</select>
				</div>
				<div class="form-group">
					<label class="form-label" for="new-disc-pct">Discount Percent (%)</label>
					<input id="new-disc-pct" type="number" min="1" max="100" class="form-input" bind:value={newDiscPercent} required />
				</div>
			</div>
			{#if newDiscType === 'promo_code'}
				<div class="form-group" style="margin-top: 0.75rem;">
					<label class="form-label" for="new-disc-code">Promo Code *</label>
					<input id="new-disc-code" type="text" class="form-input font-mono" bind:value={newDiscCode} placeholder="HERO15" />
				</div>
			{:else}
				<div class="form-group" style="margin-top: 0.75rem;">
					<label class="form-label" for="new-disc-min">Min Throwers Required</label>
					<input id="new-disc-min" type="number" min="2" class="form-input" bind:value={newDiscMinParty} />
				</div>
			{/if}
			<div style="margin-top: 0.75rem; display: flex; align-items: center; gap: 0.5rem;">
				<input id="new-disc-auto" type="checkbox" bind:checked={newDiscAutoApply} />
				<label for="new-disc-auto" style="font-size: 0.85rem; color: #fff; cursor: pointer;">Auto-apply during checkout</label>
			</div>
			<div class="modal-actions" style="margin-top: 1.5rem;">
				<button type="button" class="btn btn-secondary" onclick={() => (showNewDiscountModal = false)}>Cancel</button>
				<button type="button" class="btn btn-primary font-display" onclick={addDiscount}>+ Add Discount</button>
			</div>
		</div>
	</div>
{/if}

<!-- Add Addon Modal -->
{#if showNewAddonModal}
	<div class="modal-overlay" role="button" tabindex="0" onclick={() => (showNewAddonModal = false)} onkeydown={(e) => { if (e.key === 'Escape') showNewAddonModal = false; }}>
		<div class="modal-card glass-panel" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.stopPropagation()}>
			<div class="modal-header-row">
				<h3 class="modal-title font-display">Add Upgrades & Perks</h3>
				<button type="button" class="btn-clear" onclick={() => (showNewAddonModal = false)}>✕</button>
			</div>
			<div class="form-group" style="margin-top: 1rem;">
				<label class="form-label" for="new-addon-name">Addon Item Name *</label>
				<input id="new-addon-name" type="text" class="form-input" bind:value={newAddonName} placeholder="e.g. Local Craft Beer Pitcher" required />
			</div>
			<div class="form-group" style="margin-top: 0.75rem;">
				<label class="form-label" for="new-addon-desc">Description</label>
				<input id="new-addon-desc" type="text" class="form-input" bind:value={newAddonDesc} placeholder="Fresh draft pitcher for the bay" />
			</div>
			<div class="form-row-2" style="margin-top: 0.75rem;">
				<div class="form-group">
					<label class="form-label" for="new-addon-price">Price ($)</label>
					<input id="new-addon-price" type="number" step="0.50" min="0" class="form-input" bind:value={newAddonPriceDollars} required />
				</div>
				<div class="form-group">
					<label class="form-label" for="new-addon-type">Pricing Type</label>
					<select id="new-addon-type" class="form-input" bind:value={newAddonPriceType}>
						<option value="flat">Flat per Booking</option>
						<option value="per_person">Per Thrower</option>
					</select>
				</div>
			</div>
			<div class="modal-actions" style="margin-top: 1.5rem;">
				<button type="button" class="btn btn-secondary" onclick={() => (showNewAddonModal = false)}>Cancel</button>
				<button type="button" class="btn btn-primary font-display" onclick={addAddon}>+ Add Upgrades</button>
			</div>
		</div>
	</div>
{/if}

<!-- Add Intake Question Modal -->
{#if showNewCustomFieldModal}
	<div class="modal-overlay" role="button" tabindex="0" onclick={() => (showNewCustomFieldModal = false)} onkeydown={(e) => { if (e.key === 'Escape') showNewCustomFieldModal = false; }}>
		<div class="modal-card glass-panel" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()} onkeydown={(e) => e.stopPropagation()}>
			<div class="modal-header-row">
				<h3 class="modal-title font-display">Add Intake Question</h3>
				<button type="button" class="btn-clear" onclick={() => (showNewCustomFieldModal = false)}>✕</button>
			</div>
			<div class="form-group" style="margin-top: 1rem;">
				<label class="form-label" for="new-field-label">Question Label *</label>
				<input id="new-field-label" type="text" class="form-input" bind:value={newFieldLabel} placeholder="e.g. What occasion are you celebrating?" required />
			</div>
			<div class="form-group" style="margin-top: 0.75rem;">
				<label class="form-label" for="new-field-type">Question Type</label>
				<select id="new-field-type" class="form-input" bind:value={newFieldType}>
					<option value="text">Text Response</option>
					<option value="select">Dropdown Choices</option>
					<option value="checkbox">Yes / No Agreement</option>
				</select>
			</div>
			{#if newFieldType === 'select'}
				<div class="form-group" style="margin-top: 0.75rem;">
					<label class="form-label" for="new-field-opts">Choices (Comma Separated)</label>
					<input id="new-field-opts" type="text" class="form-input" bind:value={newFieldOptionsRaw} placeholder="Birthday, Bachelor, Corporate Team" />
				</div>
			{/if}
			<div style="margin-top: 0.75rem; display: flex; align-items: center; gap: 0.5rem;">
				<input id="new-field-req" type="checkbox" bind:checked={newFieldRequired} />
				<label for="new-field-req" style="font-size: 0.85rem; color: #fff; cursor: pointer;">Require answer before checkout</label>
			</div>
			<div class="modal-actions" style="margin-top: 1.5rem;">
				<button type="button" class="btn btn-secondary" onclick={() => (showNewCustomFieldModal = false)}>Cancel</button>
				<button type="button" class="btn btn-primary font-display" onclick={addCustomField}>+ Add Question</button>
			</div>
		</div>
	</div>
{/if}

<style>
	.mode-btn {
		background: transparent;
		border: none;
		color: var(--text-secondary);
		padding: 0.4rem 0.85rem;
		font-size: 0.82rem;
		font-weight: 600;
		border-radius: calc(var(--radius-sm) - 2px);
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.mode-btn.active {
		background: var(--accent-amber);
		color: #000;
		font-weight: 700;
	}

	.subtab-btn {
		background: rgba(255, 255, 255, 0.04);
		border: 1px solid var(--border-color);
		color: var(--text-secondary);
		padding: 0.5rem 1rem;
		border-radius: var(--radius-sm);
		font-size: 0.85rem;
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.subtab-btn.active {
		background: rgba(245, 158, 11, 0.15);
		border-color: var(--accent-amber);
		color: var(--accent-amber);
		font-weight: 700;
	}

	.btn-clear {
		background: transparent;
		border: none;
		cursor: pointer;
		padding: 0;
	}

	.modal-overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.75);
		backdrop-filter: blur(4px);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 1000;
		padding: 1.5rem;
	}

	.modal-card {
		width: 100%;
		max-width: 500px;
		background: #0f172a;
		border: 1px solid var(--border-color);
		border-radius: var(--radius-lg);
		padding: 1.75rem;
		box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.8);
	}

	.modal-header-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.modal-title {
		font-size: 1.2rem;
		margin: 0;
		color: #f8fafc;
	}

	.modal-actions {
		display: flex;
		justify-content: flex-end;
		gap: 0.75rem;
	}
</style>
