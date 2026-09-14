<script lang="ts">
	import { onMount, onDestroy } from 'svelte';

	interface Props {
		onchange?: (pngBase64: string) => void;
	}

	let { onchange }: Props = $props();

	let canvas: HTMLCanvasElement | null = $state(null);
	let ctx: CanvasRenderingContext2D | null = null;
	let isDrawing = $state(false);
	let hasSignature = $state(false);
	let lastX = 0;
	let lastY = 0;
	let resizeObserver: ResizeObserver | null = null;
	let dpr = 1;

	function setupCanvas() {
		if (!canvas) return;
		const rect = canvas.getBoundingClientRect();
		if (rect.width === 0 || rect.height === 0) return;

		dpr = Math.max(window.devicePixelRatio || 1, 2);
		const prevData = hasSignature ? canvas.toDataURL('image/png') : null;

		canvas.width = Math.round(rect.width * dpr);
		canvas.height = Math.round(rect.height * dpr);

		ctx = canvas.getContext('2d');
		if (ctx) {
			ctx.scale(dpr, dpr);
			ctx.strokeStyle = '#f8fafc';
			ctx.fillStyle = '#f8fafc';
			ctx.lineWidth = 2.5;
			ctx.lineCap = 'round';
			ctx.lineJoin = 'round';

			if (prevData) {
				const img = new Image();
				img.onload = () => {
					ctx?.drawImage(img, 0, 0, rect.width, rect.height);
				};
				img.src = prevData;
			}
		}
	}

	onMount(() => {
		setupCanvas();
		if (canvas && typeof ResizeObserver !== 'undefined') {
			resizeObserver = new ResizeObserver(() => {
				if (!isDrawing && canvas) {
					const rect = canvas.getBoundingClientRect();
					if (Math.abs(canvas.width - Math.round(rect.width * dpr)) > 10) {
						setupCanvas();
					}
				}
			});
			resizeObserver.observe(canvas);
		}
	});

	onDestroy(() => {
		resizeObserver?.disconnect();
	});

	function getCoordinates(event: PointerEvent) {
		if (!canvas) return { x: 0, y: 0 };
		const rect = canvas.getBoundingClientRect();
		return {
			x: event.clientX - rect.left,
			y: event.clientY - rect.top
		};
	}

	function handlePointerDown(e: PointerEvent) {
		if (!canvas || !ctx) return;
		try {
			canvas.setPointerCapture(e.pointerId);
		} catch {}
		isDrawing = true;
		const { x, y } = getCoordinates(e);
		lastX = x;
		lastY = y;
		ctx.beginPath();
		ctx.arc(x, y, (ctx.lineWidth || 2.5) / 2, 0, Math.PI * 2);
		ctx.fill();
		hasSignature = true;
	}

	function handlePointerMove(e: PointerEvent) {
		if (!isDrawing || !ctx) return;
		const { x, y } = getCoordinates(e);
		ctx.beginPath();
		ctx.moveTo(lastX, lastY);
		ctx.lineTo(x, y);
		ctx.stroke();
		lastX = x;
		lastY = y;
		hasSignature = true;
	}

	function handlePointerUp(e: PointerEvent) {
		if (!isDrawing) return;
		isDrawing = false;
		try {
			if (canvas?.hasPointerCapture(e.pointerId)) {
				canvas.releasePointerCapture(e.pointerId);
			}
		} catch {}
		if (canvas && hasSignature) {
			const dataUrl = canvas.toDataURL('image/png');
			onchange?.(dataUrl);
		}
	}

	export function sampleSign() {
		if (!canvas || !ctx) return;
		const rect = canvas.getBoundingClientRect();
		const w = rect.width || 600;
		const h = rect.height || 180;

		ctx.clearRect(0, 0, w, h);
		ctx.beginPath();

		// Stylized cursive initial flourish
		ctx.moveTo(w * 0.16, h * 0.68);
		ctx.bezierCurveTo(w * 0.10, h * 0.55, w * 0.20, h * 0.18, w * 0.29, h * 0.22);
		ctx.bezierCurveTo(w * 0.36, h * 0.25, w * 0.14, h * 0.82, w * 0.24, h * 0.70);
		ctx.bezierCurveTo(w * 0.30, h * 0.60, w * 0.34, h * 0.44, w * 0.44, h * 0.42);
		
		// Rhythmic cursive letters
		ctx.bezierCurveTo(w * 0.48, h * 0.40, w * 0.46, h * 0.64, w * 0.52, h * 0.54);
		ctx.bezierCurveTo(w * 0.56, h * 0.46, w * 0.60, h * 0.66, w * 0.66, h * 0.50);
		
		// Second surname peak and slash
		ctx.moveTo(w * 0.70, h * 0.68);
		ctx.bezierCurveTo(w * 0.74, h * 0.26, w * 0.80, h * 0.22, w * 0.84, h * 0.38);
		ctx.bezierCurveTo(w * 0.87, h * 0.52, w * 0.74, h * 0.74, w * 0.90, h * 0.58);
		ctx.lineTo(w * 0.95, h * 0.56);

		// Dynamic sweeping underline
		ctx.moveTo(w * 0.14, h * 0.80);
		ctx.bezierCurveTo(w * 0.38, h * 0.78, w * 0.68, h * 0.81, w * 0.89, h * 0.76);
		
		ctx.stroke();

		hasSignature = true;
		const dataUrl = canvas.toDataURL('image/png');
		onchange?.(dataUrl);
	}

	export function clear() {
		if (!canvas || !ctx) return;
		const rect = canvas.getBoundingClientRect();
		ctx.clearRect(0, 0, rect.width || 600, rect.height || 180);
		hasSignature = false;
		onchange?.('');
	}
</script>

<div class="canvas-wrapper">
	<div class="canvas-header">
		<span class="label font-display">Sign Below with Finger or Stylus</span>
		<div style="display: flex; gap: 0.5rem;">
			<button type="button" class="btn-clear" onclick={sampleSign}>Quick Sign</button>
			<button type="button" class="btn-clear" onclick={clear}>Clear</button>
		</div>
	</div>
	<div class="canvas-box">
		<canvas
			bind:this={canvas}
			class="sig-canvas"
			onpointerdown={handlePointerDown}
			onpointermove={handlePointerMove}
			onpointerup={handlePointerUp}
			onpointercancel={handlePointerUp}
		></canvas>
		<div class="baseline"></div>
	</div>
</div>

<style>
	.canvas-wrapper {
		width: 100%;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.canvas-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.label {
		font-size: 0.85rem;
		color: var(--text-secondary);
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.btn-clear {
		background: transparent;
		border: 1px solid var(--border-color);
		color: var(--text-muted);
		padding: 0.25rem 0.75rem;
		border-radius: var(--radius-sm);
		font-size: 0.75rem;
		font-weight: 700;
		cursor: pointer;
		text-transform: uppercase;
	}

	.btn-clear:hover {
		color: var(--accent-crimson);
		border-color: var(--accent-crimson);
	}

	.canvas-box {
		position: relative;
		background: #0f141c;
		border: 2px dashed var(--border-color);
		border-radius: var(--radius-md);
		overflow: hidden;
		touch-action: none;
	}

	.sig-canvas {
		width: 100%;
		height: 180px;
		display: block;
		cursor: crosshair;
		touch-action: none;
	}

	.baseline {
		position: absolute;
		bottom: 30px;
		left: 20px;
		right: 20px;
		height: 1px;
		background: rgba(148, 163, 184, 0.2);
		pointer-events: none;
	}
</style>
