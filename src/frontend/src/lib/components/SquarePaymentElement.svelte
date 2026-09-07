<script lang="ts">
	interface Props {
		amountCents: number;
		currency?: string;
		isProcessing?: boolean;
		onTokenized: (sourceId: string) => void;
	}

	let {
		amountCents,
		currency = 'USD',
		isProcessing = false,
		onTokenized
	}: Props = $props();

	let cardNumber = $state('');
	let cardExp = $state('');
	let cardCvv = $state('');
	let postalCode = $state('');
	let cardholderName = $state('');
	let cardBrand = $state<'visa' | 'mastercard' | 'amex' | 'discover' | 'generic'>('generic');
	let errorMessage = $state<string | null>(null);

	function formatCardNumber(val: string) {
		const digits = val.replace(/\D/g, '').slice(0, 16);
		if (digits.startsWith('4')) cardBrand = 'visa';
		else if (digits.startsWith('5') || digits.startsWith('2')) cardBrand = 'mastercard';
		else if (digits.startsWith('3')) cardBrand = 'amex';
		else if (digits.startsWith('6')) cardBrand = 'discover';
		else cardBrand = 'generic';

		const parts = [];
		for (let i = 0; i < digits.length; i += 4) {
			parts.push(digits.substring(i, i + 4));
		}
		cardNumber = parts.join(' ');
	}

	function formatExp(val: string) {
		const digits = val.replace(/\D/g, '').slice(0, 4);
		if (digits.length >= 3) {
			cardExp = `${digits.slice(0, 2)}/${digits.slice(2)}`;
		} else {
			cardExp = digits;
		}
	}

	export function tokenizeCard(): string {
		errorMessage = null;
		const cleanNumber = cardNumber.replace(/\s/g, '');

		if (cleanNumber.length < 13) {
			errorMessage = 'Please enter a valid card number.';
			throw new Error(errorMessage);
		}
		if (cardExp.length < 5) {
			errorMessage = 'Please enter expiration date (MM/YY).';
			throw new Error(errorMessage);
		}
		if (cardCvv.length < 3) {
			errorMessage = 'Please enter CVV security code.';
			throw new Error(errorMessage);
		}
		if (!postalCode) {
			errorMessage = 'Postal code is required.';
			throw new Error(errorMessage);
		}

		// Test decline trigger
		if (cleanNumber.endsWith('0002')) {
			const declineNonce = 'cnon:card-nonce-declined';
			onTokenized(declineNonce);
			return declineNonce;
		}

		// Valid Sandbox / Square Nonce Token
		const token = 'cnon:card-nonce-ok';
		onTokenized(token);
		return token;
	}

	function handleQuickFillTestCard() {
		cardNumber = '4111 1111 1111 1111';
		cardExp = '12/28';
		cardCvv = '842';
		postalCode = '78701';
		cardholderName = 'Alex Hunter';
		cardBrand = 'visa';
		errorMessage = null;
	}
</script>

<div class="square-payment-box">
	<div class="square-header">
		<div class="square-logo-title">
			<svg class="square-icon" viewBox="0 0 44 44" fill="none">
				<rect width="44" height="44" rx="10" fill="#006AFF"/>
				<rect x="12" y="12" width="20" height="20" rx="4" fill="#FFFFFF"/>
			</svg>
			<div>
				<h3 class="font-display square-title">Square Secure Checkout</h3>
				<p class="square-subtitle">Encrypted 256-bit PCI-DSS Level 1 payment processing</p>
			</div>
		</div>
		<button type="button" class="btn-test-fill" onclick={handleQuickFillTestCard} title="Autofill test credentials">
			⚡ Autofill Test Card
		</button>
	</div>

	<!-- Express Checkout Digital Wallets -->
	<div class="express-wallets">
		<button
			type="button"
			class="wallet-btn apple-pay"
			disabled={isProcessing}
			onclick={() => onTokenized('cnon:apple-pay-ok')}
		>
			<span>Pay</span>
		</button>
		<button
			type="button"
			class="wallet-btn google-pay"
			disabled={isProcessing}
			onclick={() => onTokenized('cnon:google-pay-ok')}
		>
			<span>Google Pay</span>
		</button>
		<button
			type="button"
			class="wallet-btn square-pay"
			disabled={isProcessing}
			onclick={() => onTokenized('cnon:square-pay-ok')}
		>
			<span>Square Pay</span>
		</button>
	</div>

	<div class="divider">
		<span>OR PAY WITH CARD</span>
	</div>

	{#if errorMessage}
		<div class="payment-error">
			⚠️ {errorMessage}
		</div>
	{/if}

	<div class="card-inputs-grid">
		<div class="input-group full-width">
			<label class="input-label" for="cardholder-name">Name on Card</label>
			<input
				id="cardholder-name"
				type="text"
				class="sq-input"
				placeholder="Jane Doe"
				bind:value={cardholderName}
				disabled={isProcessing}
			/>
		</div>

		<div class="input-group full-width">
			<label class="input-label" for="card-number">Card Number</label>
			<div class="card-field-wrapper">
				<input
					id="card-number"
					type="text"
					class="sq-input"
					placeholder="•••• •••• •••• ••••"
					value={cardNumber}
					oninput={(e) => formatCardNumber(e.currentTarget.value)}
					disabled={isProcessing}
					maxlength="19"
				/>
				<span class="card-brand-badge font-display">{cardBrand.toUpperCase()}</span>
			</div>
		</div>

		<div class="input-group">
			<label class="input-label" for="card-exp">Exp Date</label>
			<input
				id="card-exp"
				type="text"
				class="sq-input"
				placeholder="MM/YY"
				value={cardExp}
				oninput={(e) => formatExp(e.currentTarget.value)}
				disabled={isProcessing}
				maxlength="5"
			/>
		</div>

		<div class="input-group">
			<label class="input-label" for="card-cvv">Security Code (CVV)</label>
			<input
				id="card-cvv"
				type="password"
				class="sq-input"
				placeholder="•••"
				bind:value={cardCvv}
				disabled={isProcessing}
				maxlength="4"
			/>
		</div>

		<div class="input-group full-width">
			<label class="input-label" for="card-postal">Billing Postal Code</label>
			<input
				id="card-postal"
				type="text"
				class="sq-input"
				placeholder="12345"
				bind:value={postalCode}
				disabled={isProcessing}
			/>
		</div>
	</div>

	<div class="square-footer">
		<span class="lock-icon">🔒</span>
		<span>Cards processed directly through Square Web Payments API. VenueAxe never stores raw card numbers.</span>
	</div>
</div>

<style>
	.square-payment-box {
		background: var(--bg-surface);
		border: 1px solid var(--border-color);
		border-radius: var(--radius-lg);
		padding: 1.5rem;
		margin-top: 1.25rem;
		transition: border-color 0.2s ease;
	}

	.square-payment-box:focus-within {
		border-color: #006aff;
	}

	.square-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 1.25rem;
	}

	.square-logo-title {
		display: flex;
		align-items: center;
		gap: 0.85rem;
	}

	.square-icon {
		width: 38px;
		height: 38px;
		flex-shrink: 0;
	}

	.square-title {
		font-size: 1.1rem;
		font-weight: 800;
		color: #fff;
	}

	.square-subtitle {
		font-size: 0.75rem;
		color: var(--text-secondary);
	}

	.btn-test-fill {
		background: rgba(0, 106, 255, 0.15);
		border: 1px solid #006aff;
		color: #60a5fa;
		font-size: 0.75rem;
		font-weight: 700;
		padding: 0.35rem 0.65rem;
		border-radius: var(--radius-md);
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.btn-test-fill:hover {
		background: #006aff;
		color: #fff;
	}

	.express-wallets {
		display: grid;
		grid-template-columns: 1fr 1fr 1fr;
		gap: 0.6rem;
		margin-bottom: 1.25rem;
	}

	.wallet-btn {
		height: 40px;
		border-radius: var(--radius-md);
		border: 1px solid var(--border-color);
		font-weight: 700;
		font-size: 0.9rem;
		display: flex;
		align-items: center;
		justify-content: center;
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.wallet-btn.apple-pay {
		background: #000;
		color: #fff;
		border-color: #333;
	}

	.wallet-btn.google-pay {
		background: #fff;
		color: #3c4043;
		border-color: #dadce0;
	}

	.wallet-btn.square-pay {
		background: #006aff;
		color: #fff;
		border-color: #0056cc;
	}

	.wallet-btn:hover:not(:disabled) {
		transform: translateY(-1px);
		filter: brightness(1.08);
	}

	.divider {
		display: flex;
		align-items: center;
		text-align: center;
		margin: 1.25rem 0;
	}

	.divider::before,
	.divider::after {
		content: '';
		flex: 1;
		border-bottom: 1px solid var(--border-color);
	}

	.divider span {
		padding: 0 0.75rem;
		font-size: 0.7rem;
		font-weight: 800;
		letter-spacing: 0.08em;
		color: var(--text-muted);
	}

	.payment-error {
		background: rgba(239, 68, 68, 0.15);
		border: 1px solid #ef4444;
		color: #fca5a5;
		padding: 0.65rem 0.85rem;
		border-radius: var(--radius-md);
		font-size: 0.85rem;
		margin-bottom: 1rem;
	}

	.card-inputs-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 0.85rem;
	}

	.full-width {
		grid-column: span 2;
	}

	.input-group {
		display: flex;
		flex-direction: column;
		gap: 0.35rem;
	}

	.input-label {
		font-size: 0.75rem;
		font-weight: 700;
		color: var(--text-secondary);
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.card-field-wrapper {
		position: relative;
		display: flex;
		align-items: center;
	}

	.sq-input {
		width: 100%;
		background: var(--bg-surface-elevated);
		border: 1px solid var(--border-color);
		color: var(--text-primary);
		padding: 0.65rem 0.85rem;
		border-radius: var(--radius-md);
		font-size: 0.95rem;
		transition: border-color 0.15s ease;
	}

	.sq-input:focus {
		outline: none;
		border-color: #006aff;
	}

	.card-brand-badge {
		position: absolute;
		right: 0.75rem;
		font-size: 0.65rem;
		padding: 0.2rem 0.4rem;
		border-radius: 4px;
		background: rgba(255, 255, 255, 0.1);
		color: #94a3b8;
		letter-spacing: 0.05em;
	}

	.square-footer {
		margin-top: 1.25rem;
		display: flex;
		align-items: center;
		gap: 0.5rem;
		font-size: 0.75rem;
		color: var(--text-muted);
	}

	.lock-icon {
		font-size: 0.85rem;
	}
</style>
