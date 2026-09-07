<script lang="ts">
	import { onMount } from 'svelte';

	interface Props {
		onchange?: (pngBase64: string) => void;
	}

	let { onchange }: Props = $props();

	let canvas: HTMLCanvasElement | null = $state(null);
	let ctx: CanvasRenderingContext2D | null = null;
	let isDrawing = $state(false);
	let hasSignature = $state(false);

	onMount(() => {
		if (canvas) {
			ctx = canvas.getContext('2d');
			if (ctx) {
				ctx.strokeStyle = '#f8fafc';
				ctx.lineWidth = 3;
				ctx.lineCap = 'round';
				ctx.lineJoin = 'round';
			}
		}
	});

	function getCoordinates(event: MouseEvent | TouchEvent) {
		if (!canvas) return { x: 0, y: 0 };
		const rect = canvas.getBoundingClientRect();
		const scaleX = canvas.width / rect.width;
		const scaleY = canvas.height / rect.height;

		if ('touches' in event && event.touches.length > 0) {
			return {
				x: (event.touches[0].clientX - rect.left) * scaleX,
				y: (event.touches[0].clientY - rect.top) * scaleY
			};
		}
		if ('clientX' in event) {
			return {
				x: (event.clientX - rect.left) * scaleX,
				y: (event.clientY - rect.top) * scaleY
			};
		}
		return { x: 0, y: 0 };
	}

	function startDrawing(e: MouseEvent | TouchEvent) {
		e.preventDefault();
		isDrawing = true;
		const { x, y } = getCoordinates(e);
		ctx?.beginPath();
		ctx?.moveTo(x, y);
	}

	function draw(e: MouseEvent | TouchEvent) {
		if (!isDrawing || !ctx) return;
		e.preventDefault();
		const { x, y } = getCoordinates(e);
		ctx.lineTo(x, y);
		ctx.stroke();
		hasSignature = true;
	}

	function stopDrawing() {
		if (!isDrawing) return;
		isDrawing = false;
		if (canvas && hasSignature) {
			const dataUrl = canvas.toDataURL('image/png');
			onchange?.(dataUrl);
		}
	}

	export function sampleSign() {
		if (!canvas || !ctx) return;
		ctx.beginPath();
		ctx.moveTo(50, 100);
		ctx.bezierCurveTo(150, 40, 200, 160, 350, 80);
		ctx.bezierCurveTo(400, 50, 450, 120, 520, 90);
		ctx.stroke();
		hasSignature = true;
		const dataUrl = canvas.toDataURL('image/png');
		onchange?.(dataUrl);
	}

	export function clear() {
		if (!canvas || !ctx) return;
		ctx.clearRect(0, 0, canvas.width, canvas.height);
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
			width="600"
			height="200"
			class="sig-canvas"
			onmousedown={startDrawing}
			onmousemove={draw}
			onmouseup={stopDrawing}
			onmouseleave={stopDrawing}
			ontouchstart={startDrawing}
			ontouchmove={draw}
			ontouchend={stopDrawing}
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
