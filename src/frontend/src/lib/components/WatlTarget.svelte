<script lang="ts">
	interface Props {
		interactive?: boolean;
		isClutchCalled?: boolean;
		targetType?: 'watl' | 'iatf';
		lastThrow?: { x?: number | null; y?: number | null; pointsAwarded?: number; zone?: string } | null;
		onthrow?: (payload: { x: number; y: number; isClutchCalled: boolean }) => void;
	}

	let { interactive = true, isClutchCalled = false, targetType = 'watl', lastThrow = null, onthrow }: Props = $props();

	let svgElement: SVGSVGElement | null = $state(null);
	let hitMarker: { x: number; y: number; visible: boolean; points: number } = $state({
		x: 0,
		y: 0,
		visible: false,
		points: 0
	});

	// SVG ViewBox dimensions: centered at 0,0 from -500 to +500
	const SIZE = 1000;
	const HALF = 500;

	// Proportional radii mapped to SVG 500px radius
	const R_BULL = 48.5;  // 0.097 * 500
	const R_5 = 90.0;     // 0.180 * 500
	const R_4 = 132.0;    // 0.264 * 500
	const R_3 = 173.5;    // 0.347 * 500
	const R_2 = 215.0;    // 0.430 * 500
	const R_1 = 257.0;    // 0.514 * 500

	const CLUTCH_X = 190.0; // 0.380 * 500
	const CLUTCH_Y = -230.0; // 0.460 * 500 (SVG Y is inverted up/down)
	const R_CLUTCH = 36.5;  // 0.073 * 500

	function handleBoardClick(event: MouseEvent | TouchEvent) {
		if (!interactive || !svgElement) return;

		const rect = svgElement.getBoundingClientRect();
		let clientX = 0;
		let clientY = 0;

		if ('touches' in event && event.touches.length > 0) {
			clientX = event.touches[0].clientX;
			clientY = event.touches[0].clientY;
		} else if ('clientX' in event) {
			clientX = event.clientX;
			clientY = event.clientY;
		}

		// Convert screen pixels to SVG coordinate [-500, +500]
		const rawX = ((clientX - rect.left) / rect.width) * SIZE - HALF;
		const rawY = ((clientY - rect.top) / rect.height) * SIZE - HALF;

		// Normalized coordinates [-1.0, +1.0] (invert Y so top is positive)
		const normX = Math.round((rawX / HALF) * 10000) / 10000;
		const normY = Math.round((-rawY / HALF) * 10000) / 10000;

		hitMarker = {
			x: rawX,
			y: rawY,
			visible: true,
			points: 0
		};

		onthrow?.({
			x: normX,
			y: normY,
			isClutchCalled
		});
	}

	$effect(() => {
		if (lastThrow && lastThrow.x !== undefined && lastThrow.y !== undefined && lastThrow.x !== null && lastThrow.y !== null) {
			hitMarker = {
				x: lastThrow.x * HALF,
				y: -lastThrow.y * HALF,
				visible: true,
				points: lastThrow.pointsAwarded ?? 0
			};
		}
	});
</script>

<div class="target-container">
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<svg
		bind:this={svgElement}
		viewBox="-500 -500 1000 1000"
		class="watl-svg"
		class:clickable={interactive}
		onclick={handleBoardClick}
		ontouchstart={handleBoardClick}
	>
		<defs>
			<!-- Wood grain background pattern -->
			<radialGradient id="woodGrain" cx="50%" cy="50%" r="50%">
				<stop offset="0%" stop-color="#2a231d" />
				<stop offset="85%" stop-color="#1c1713" />
				<stop offset="100%" stop-color="#0f0c0a" />
			</radialGradient>

			<!-- Target glowing drop shadow -->
			<filter id="boardGlow" x="-20%" y="-20%" width="140%" height="140%">
				<feDropShadow dx="0" dy="8" stdDeviation="15" flood-color="rgba(0,0,0,0.8)" />
			</filter>

			<filter id="clutchGlow" x="-50%" y="-50%" width="200%" height="200%">
				<feDropShadow dx="0" dy="0" stdDeviation="10" flood-color="#06b6d4" />
			</filter>
		</defs>

		<!-- Target Wood Planks -->
		<circle cx="0" cy="0" r="470" fill="url(#woodGrain)" stroke="#3f3328" stroke-width="8" filter="url(#boardGlow)" />

		<!-- Vertical Timber Seams -->
		<line x1="-160" y1="-470" x2="-160" y2="470" stroke="#120e0a" stroke-width="4" stroke-dasharray="8 4" />
		<line x1="160" y1="-470" x2="160" y2="470" stroke="#120e0a" stroke-width="4" stroke-dasharray="8 4" />

		<!-- Target Rings (Outer to Inner) -->
		<!-- Ring 1 (1 pt - Black) -->
		<circle cx="0" cy="0" r={R_1} fill="#181e29" stroke="#334155" stroke-width="3" />

		<!-- Ring 2 (2 pts - Blue) -->
		<circle cx="0" cy="0" r={R_2} fill="#1d4ed8" stroke="#1e40af" stroke-width="3" />

		<!-- Ring 3 (3 pts - Red) -->
		<circle cx="0" cy="0" r={R_3} fill="#b91c1c" stroke="#991b1b" stroke-width="3" />

		<!-- Ring 4 (4 pts - Blue) -->
		<circle cx="0" cy="0" r={R_4} fill="#2563eb" stroke="#1d4ed8" stroke-width="3" />

		<!-- Ring 5 (5 pts - Red) -->
		<circle cx="0" cy="0" r={R_5} fill="#dc2626" stroke="#b91c1c" stroke-width="3" />

		<!-- Bullseye (6 pts - Black Core) -->
		<circle cx="0" cy="0" r={R_BULL} fill="#090d16" stroke="#f59e0b" stroke-width="4" />
		<circle cx="0" cy="0" r="12" fill="#f59e0b" />

		<!-- Left Corner Target (WATL Killshot 8 pts / IATF Clutch 7 pts) -->
		<g class="clutch-group" class:armed={isClutchCalled}>
			<circle
				cx={-CLUTCH_X}
				cy={CLUTCH_Y}
				r={R_CLUTCH}
				fill="#0891b2"
				stroke={isClutchCalled ? '#22d3ee' : '#155e75'}
				stroke-width="5"
				filter={isClutchCalled ? 'url(#clutchGlow)' : ''}
			/>
			<circle cx={-CLUTCH_X} cy={CLUTCH_Y} r="8" fill="#ffffff" />
			<text x={-CLUTCH_X} y={CLUTCH_Y + 50} text-anchor="middle" fill="#06b6d4" font-family="'Chakra Petch'" font-weight="700" font-size="20">
				{targetType === 'watl' ? 'KILLSHOT' : 'CLUTCH'}
			</text>
		</g>

		<!-- Right Corner Target (WATL Killshot 8 pts / IATF Clutch 7 pts) -->
		<g class="clutch-group" class:armed={isClutchCalled}>
			<circle
				cx={CLUTCH_X}
				cy={CLUTCH_Y}
				r={R_CLUTCH}
				fill="#0891b2"
				stroke={isClutchCalled ? '#22d3ee' : '#155e75'}
				stroke-width="5"
				filter={isClutchCalled ? 'url(#clutchGlow)' : ''}
			/>
			<circle cx={CLUTCH_X} cy={CLUTCH_Y} r="8" fill="#ffffff" />
			<text x={CLUTCH_X} y={CLUTCH_Y + 50} text-anchor="middle" fill="#06b6d4" font-family="'Chakra Petch'" font-weight="700" font-size="20">
				{targetType === 'watl' ? 'KILLSHOT' : 'CLUTCH'}
			</text>
		</g>

		<!-- Point Labels on Rings for clear guidance -->
		<text x="0" y={-R_1 + 25} text-anchor="middle" fill="#94a3b8" font-size="20" font-weight="700">1</text>
		<text x="0" y={-R_2 + 25} text-anchor="middle" fill="#e0e7ff" font-size="22" font-weight="700">2</text>
		<text x="0" y={-R_3 + 25} text-anchor="middle" fill="#fee2e2" font-size="24" font-weight="700">3</text>
		<text x="0" y={-R_4 + 25} text-anchor="middle" fill="#e0e7ff" font-size="26" font-weight="700">4</text>
		<text x="0" y={-R_5 + 28} text-anchor="middle" fill="#fee2e2" font-size="28" font-weight="800">5</text>
		<text x="0" y="7" text-anchor="middle" fill="#000" font-size="22" font-weight="900">6</text>

		<!-- Interactive Hit Marker Animation -->
		{#if hitMarker.visible}
			<g class="hit-marker" transform="translate({hitMarker.x}, {hitMarker.y})">
				<!-- Outer Pulse Ripple -->
				<circle cx="0" cy="0" r="30" fill="none" stroke="#f59e0b" stroke-width="3" class="pulse-ring" />
				<!-- Axe Blade Hit Icon -->
				<circle cx="0" cy="0" r="14" fill="#f59e0b" stroke="#ffffff" stroke-width="3" />
				<path d="M-6,-6 L6,6 M-6,6 L6,-6" stroke="#000" stroke-width="3" />
			</g>
		{/if}
	</svg>
</div>

<style>
	.target-container {
		width: 100%;
		max-width: 580px;
		aspect-ratio: 1 / 1;
		margin: 0 auto;
		display: flex;
		align-items: center;
		justify-content: center;
		touch-action: manipulation;
		user-select: none;
	}

	.watl-svg {
		width: 100%;
		height: 100%;
		border-radius: 50%;
	}

	.watl-svg.clickable {
		cursor: crosshair;
	}

	.clutch-group.armed circle {
		animation: pulse-clutch 1.2s infinite alternate;
	}

	@keyframes pulse-clutch {
		0% { transform: scale(1); }
		100% { transform: scale(1.06); }
	}

	.pulse-ring {
		animation: ripple 1.5s infinite ease-out;
		transform-origin: center;
	}

	@keyframes ripple {
		0% { r: 15px; opacity: 1; }
		100% { r: 45px; opacity: 0; }
	}
</style>
