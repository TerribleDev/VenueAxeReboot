<script lang="ts">
	import { getGameRules } from '$lib/constants/gameRules';

	let {
		gameTypeId = 'watl_standard',
		isOpen = false,
		onClose
	}: {
		gameTypeId?: string;
		isOpen: boolean;
		onClose: () => void;
	} = $props();

	let rules = $derived(getGameRules(gameTypeId));

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape' && isOpen) {
			onClose();
		}
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
	<div class="modal-backdrop" onclick={onClose} role="presentation">
		<!-- svelte-ignore a11y_click_events_have_key_events -->
		<!-- svelte-ignore a11y_no_static_element_interactions -->
		<div
			class="rules-modal glass-panel"
			onclick={(e) => e.stopPropagation()}
			role="dialog"
			aria-modal="true"
			aria-labelledby="rules-modal-title"
			tabindex="-1"
		>
			<div class="rules-header">
				<div class="rules-title-group">
					<span class="rules-icon">{rules.icon}</span>
					<div>
						<h2 id="rules-modal-title" class="rules-title font-display">{rules.displayName}</h2>
						<span class="rules-badge font-display">{rules.badge}</span>
					</div>
				</div>
				<button type="button" class="btn-close" onclick={onClose} aria-label="Close Game Rules">✕</button>
			</div>

			<div class="rules-body">
				<!-- Objective -->
				<div class="rules-card objective-card">
					<h3 class="font-display rules-section-title">🎯 Objective</h3>
					<p class="rules-text">{rules.objective}</p>
				</div>

				<!-- Scoring -->
				<div class="rules-card">
					<h3 class="font-display rules-section-title">📊 Scoring & Zone Values</h3>
					<ul class="rules-list">
						{#each rules.scoringRules as item}
							<li>{item}</li>
						{/each}
					</ul>
				</div>

				<!-- Special Rules & Mechanics -->
				<div class="rules-card">
					<h3 class="font-display rules-section-title">⚡ Special Rules & Match Mechanics</h3>
					<ul class="rules-list">
						{#each rules.specialRules as item}
							<li>{item}</li>
						{/each}
					</ul>
				</div>

				<!-- Coach Tips -->
				{#if rules.tips && rules.tips.length > 0}
					<div class="rules-card tips-card">
						<h3 class="font-display rules-section-title">💡 Pro Coach Tips</h3>
						<ul class="rules-list">
							{#each rules.tips as tip}
								<li>{tip}</li>
						{/each}
						</ul>
					</div>
				{/if}
			</div>

			<div class="rules-footer">
				<button type="button" class="btn btn-primary font-display btn-block" onclick={onClose}>
					Got It — Back to Game
				</button>
			</div>
		</div>
	</div>
{/if}

<style>
	.modal-backdrop {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.78);
		backdrop-filter: blur(8px);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 9999;
		padding: 1rem;
	}

	.rules-modal {
		background: #0f172a;
		border: 1px solid rgba(245, 158, 11, 0.4);
		box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.8), 0 0 24px rgba(245, 158, 11, 0.15);
		border-radius: var(--radius-lg, 16px);
		width: 100%;
		max-width: 620px;
		max-height: 90vh;
		display: flex;
		flex-direction: column;
		animation: modalPop 0.2s cubic-bezier(0.16, 1, 0.3, 1);
		overflow: hidden;
	}

	@keyframes modalPop {
		from {
			opacity: 0;
			transform: scale(0.95);
		}
		to {
			opacity: 1;
			transform: scale(1);
		}
	}

	.rules-header {
		padding: 1.25rem 1.5rem;
		display: flex;
		align-items: center;
		justify-content: space-between;
		border-bottom: 1px solid rgba(255, 255, 255, 0.08);
		background: rgba(15, 23, 42, 0.95);
	}

	.rules-title-group {
		display: flex;
		align-items: center;
		gap: 0.85rem;
	}

	.rules-icon {
		font-size: 2rem;
		filter: drop-shadow(0 2px 8px rgba(0, 0, 0, 0.4));
	}

	.rules-title {
		font-size: 1.35rem;
		color: #f8fafc;
		margin: 0;
		line-height: 1.2;
	}

	.rules-badge {
		display: inline-block;
		margin-top: 0.25rem;
		background: rgba(245, 158, 11, 0.15);
		color: #f59e0b;
		border: 1px solid rgba(245, 158, 11, 0.3);
		padding: 2px 8px;
		border-radius: 9999px;
		font-size: 0.75rem;
		font-weight: 700;
	}

	.btn-close {
		background: rgba(255, 255, 255, 0.06);
		border: 1px solid rgba(255, 255, 255, 0.1);
		color: #94a3b8;
		width: 36px;
		height: 36px;
		border-radius: 50%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 1.1rem;
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.btn-close:hover {
		background: rgba(239, 68, 68, 0.2);
		color: #ef4444;
		border-color: rgba(239, 68, 68, 0.4);
	}

	.rules-body {
		padding: 1.25rem 1.5rem;
		overflow-y: auto;
		display: flex;
		flex-direction: column;
		gap: 1rem;
	}

	.rules-card {
		background: rgba(30, 41, 59, 0.6);
		border: 1px solid rgba(255, 255, 255, 0.06);
		border-radius: var(--radius-md, 10px);
		padding: 1rem 1.25rem;
	}

	.objective-card {
		background: rgba(245, 158, 11, 0.08);
		border-color: rgba(245, 158, 11, 0.25);
	}

	.tips-card {
		background: rgba(6, 182, 212, 0.08);
		border-color: rgba(6, 182, 212, 0.25);
	}

	.rules-section-title {
		font-size: 0.95rem;
		color: #f8fafc;
		margin: 0 0 0.5rem 0;
		text-transform: uppercase;
		letter-spacing: 0.04em;
	}

	.rules-text {
		color: #cbd5e1;
		font-size: 0.92rem;
		line-height: 1.5;
		margin: 0;
	}

	.rules-list {
		margin: 0;
		padding-left: 1.25rem;
		color: #cbd5e1;
		font-size: 0.9rem;
		line-height: 1.5;
		display: flex;
		flex-direction: column;
		gap: 0.35rem;
	}

	.rules-list li::marker {
		color: #f59e0b;
	}

	.rules-footer {
		padding: 1rem 1.5rem;
		border-top: 1px solid rgba(255, 255, 255, 0.08);
		background: rgba(15, 23, 42, 0.95);
	}

	.btn-block {
		width: 100%;
		padding: 0.75rem 1rem;
		font-size: 1rem;
	}
</style>
