<script lang="ts">
	import QRCode from 'qrcode';

	let { text = '', size = 180 }: { text: string; size?: number } = $props();

	let svgContent = $state('');

	$effect(() => {
		if (!text) {
			svgContent = '';
			return;
		}
		QRCode.toString(text, {
			type: 'svg',
			margin: 1,
			color: {
				dark: '#000000',
				light: '#ffffff'
			}
		}).then((svg) => {
			svgContent = svg;
		}).catch((err) => {
			console.error('QR generation error:', err);
		});
	});
</script>

<div class="qr-container" style="width: {size}px; height: {size}px;">
	{#if svgContent}
		{@html svgContent}
	{:else}
		<div class="qr-placeholder">Generating QR...</div>
	{/if}
</div>

<style>
	.qr-container {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		background: #ffffff;
		padding: 8px;
		border-radius: 8px;
		box-shadow: 0 4px 16px rgba(0, 0, 0, 0.4);
		overflow: hidden;
	}

	.qr-container :global(svg) {
		width: 100% !important;
		height: 100% !important;
		display: block;
	}

	.qr-placeholder {
		font-size: 0.8rem;
		color: #666;
	}
</style>
