<script lang="ts">
	interface Props {
		interactive?: boolean;
		isClutchCalled?: boolean;
		overlayMode?: 'standard' | 'tic_tac_toe';
		tttGrid?: Array<string | null>; // 9 elements: 'X', 'O', or null
		lastThrow?: { x?: number | null; y?: number | null; pointsAwarded?: number; zone?: string } | null;
		scatterThrows?: Array<{ x?: number | null; y?: number | null; pointsAwarded?: number; playerName?: string; color?: string }> | null;
		onthrow?: (payload: { x: number; y: number; isClutchCalled: boolean; manualZone?: number; isLineBreak?: boolean }) => void;
	}

	let {
		interactive = true,
		isClutchCalled = false,
		overlayMode = 'standard',
		tttGrid = Array(9).fill(null),
		lastThrow = null,
		scatterThrows = null,
		onthrow
	}: Props = $props();

	let svgElement: SVGSVGElement | null = $state(null);
	let hitMarker: { x: number; y: number; visible: boolean; points: number } = $state({
		x: 0,
		y: 0,
		visible: false,
		points: 0
	});

	// Boundary line touch detection prompt
	let pendingLineBreak: {
		x: number;
		y: number;
		higherZone: string;
		higherPoints: number;
		higherManualZone: number;
	} | null = $state(null);

	// SVG ViewBox dimensions: centered at 0,0 from -500 to +500
	const SIZE = 1000;
	const HALF = 500;

	// Official WATL Radii (proportional to 500px radius)
	const WATL_R_BULL = 48.5;  // 0.097 * 500 (Bullseye - 6 pts)
	const WATL_R_5 = 90.0;     // 0.180 * 500 (Ring 5 - 5 pts)
	const WATL_R_4 = 132.0;    // 0.264 * 500 (Ring 4 - 4 pts)
	const WATL_R_3 = 173.5;    // 0.347 * 500 (Ring 3 - 3 pts)
	const WATL_R_2 = 215.0;    // 0.430 * 500 (Ring 2 - 2 pts)
	const WATL_R_1 = 257.0;    // 0.514 * 500 (Ring 1 - 1 pt)

	// Official WATL Killshots: 8 pts when called
	const KILL_X = 190.0;     // 0.380 * 500
	const KILL_Y = -230.0;    // 0.460 * 500 (Inverted SVG Y)
	const R_KILL = 36.5;      // 0.073 * 500

	function checkLineBreak(distanceFromCenter: number) {
		const boundaries = [
			{ r: WATL_R_BULL, higherZone: 'Bullseye', higherPoints: 6, manualZone: 6 },
			{ r: WATL_R_5, higherZone: '5 Ring', higherPoints: 5, manualZone: 5 },
			{ r: WATL_R_4, higherZone: '4 Ring', higherPoints: 4, manualZone: 4 },
			{ r: WATL_R_3, higherZone: '3 Ring', higherPoints: 3, manualZone: 3 },
			{ r: WATL_R_2, higherZone: '2 Ring', higherPoints: 2, manualZone: 2 },
			{ r: WATL_R_1, higherZone: '1 Ring', higherPoints: 1, manualZone: 1 }
		];

		const TOLERANCE = 7.0; // +/- 7 SVG pixels (~0.014 normalized)
		for (const b of boundaries) {
			if (Math.abs(distanceFromCenter - b.r) <= TOLERANCE) {
				return { isNearLine: true, higherZone: b.higherZone, higherPoints: b.higherPoints, manualZone: b.manualZone };
			}
		}
		return { isNearLine: false, higherZone: '', higherPoints: 0, manualZone: 0 };
	}

	function handleBoardClick(event: MouseEvent | TouchEvent) {
		if (!interactive || !svgElement) return;

		const rect = svgElement.getBoundingClientRect();
		let clientX = 0;
		let clientY = 0;

		if ('touches' in event && event.touches.length > 0) {
			clientX = event.touches[0].clientX;
			clientY = event.touches[0].clientY;
		} else if ('clientX' in event) {
			clientX = (event as MouseEvent).clientX;
			clientY = (event as MouseEvent).clientY;
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

		// Check line breaking boundary touch
		const dist = Math.sqrt(rawX * rawX + rawY * rawY);
		const lineCheck = checkLineBreak(dist);

		if (lineCheck.isNearLine) {
			pendingLineBreak = {
				x: normX,
				y: normY,
				higherZone: lineCheck.higherZone,
				higherPoints: lineCheck.higherPoints,
				higherManualZone: lineCheck.manualZone
			};
		} else {
			pendingLineBreak = null;
			onthrow?.({
				x: normX,
				y: normY,
				isClutchCalled
			});
		}
	}

	function confirmLineBreak(awardedHigher: boolean) {
		if (!pendingLineBreak) return;
		const { x, y, higherManualZone } = pendingLineBreak;
		pendingLineBreak = null;
		onthrow?.({
			x,
			y,
			isClutchCalled,
			manualZone: awardedHigher ? higherManualZone : undefined,
			isLineBreak: awardedHigher
		});
	}

	$effect(() => {
		if (lastThrow && lastThrow.x != null && lastThrow.y != null) {
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
	{#if pendingLineBreak}
		<div class="line-break-modal">
			<div class="line-break-badge">LINE BREAK DETECTED</div>
			<p class="line-break-text">
				Blade touched the boundary line for <strong>{pendingLineBreak.higherZone}</strong> ({pendingLineBreak.higherPoints} pts).
			</p>
			<div class="line-break-actions">
				<button type="button" class="btn-award-higher" onclick={() => confirmLineBreak(true)}>
					Award Higher ({pendingLineBreak.higherPoints} pts)
				</button>
				<button type="button" class="btn-lower" onclick={() => confirmLineBreak(false)}>
					Award Lower
				</button>
			</div>
		</div>
	{/if}

	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<svg
		bind:this={svgElement}
		viewBox="-500 -500 1000 1000"
		class="watl-svg"
		class:clickable={interactive}
		onclick={handleBoardClick}
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

			<filter id="killGlow" x="-50%" y="-50%" width="200%" height="200%">
				<feDropShadow dx="0" dy="0" stdDeviation="12" flood-color="#06b6d4" />
			</filter>
		</defs>

		<!-- Target Wood Planks -->
		<circle cx="0" cy="0" r="470" fill="url(#woodGrain)" stroke="#3f3328" stroke-width="8" filter="url(#boardGlow)" />

		<!-- Vertical Timber Seams -->
		<line x1="-160" y1="-470" x2="-160" y2="470" stroke="#120e0a" stroke-width="4" stroke-dasharray="8 4" />
		<line x1="160" y1="-470" x2="160" y2="470" stroke="#120e0a" stroke-width="4" stroke-dasharray="8 4" />

		<!-- WATL Standard Target Rings (Strictly 6 Concentric Rings) -->
		<!-- Ring 1 (1 pt - Black Ring) -->
		<circle cx="0" cy="0" r={WATL_R_1} fill="#181e29" stroke="#334155" stroke-width="3" />

		<!-- Ring 2 (2 pts - Blue Ring) -->
		<circle cx="0" cy="0" r={WATL_R_2} fill="#1d4ed8" stroke="#1e40af" stroke-width="3" />

		<!-- Ring 3 (3 pts - Red Ring) -->
		<circle cx="0" cy="0" r={WATL_R_3} fill="#b91c1c" stroke="#991b1b" stroke-width="3" />

		<!-- Ring 4 (4 pts - Blue Ring) -->
		<circle cx="0" cy="0" r={WATL_R_4} fill="#2563eb" stroke="#1d4ed8" stroke-width="3" />

		<!-- Ring 5 (5 pts - Red Ring) -->
		<circle cx="0" cy="0" r={WATL_R_5} fill="#dc2626" stroke="#b91c1c" stroke-width="3" />

		<!-- Bullseye (6 pts - Black Core) -->
		<circle cx="0" cy="0" r={WATL_R_BULL} fill="#090d16" stroke="#f59e0b" stroke-width="4" />
		<circle cx="0" cy="0" r="12" fill="#f59e0b" />

		<!-- Point Labels for Official WATL Rings -->
		<text x="0" y={-WATL_R_1 + 25} text-anchor="middle" fill="#94a3b8" font-size="20" font-weight="700">1</text>
		<text x="0" y={-WATL_R_2 + 25} text-anchor="middle" fill="#e0e7ff" font-size="22" font-weight="700">2</text>
		<text x="0" y={-WATL_R_3 + 25} text-anchor="middle" fill="#fee2e2" font-size="24" font-weight="700">3</text>
		<text x="0" y={-WATL_R_4 + 25} text-anchor="middle" fill="#e0e7ff" font-size="26" font-weight="700">4</text>
		<text x="0" y={-WATL_R_5 + 28} text-anchor="middle" fill="#fee2e2" font-size="28" font-weight="800">5</text>
		<text x="0" y="7" text-anchor="middle" fill="#f59e0b" font-size="22" font-weight="900">6</text>

		<!-- Official WATL Killshots: 8 pts when called (Left & Right) -->
		<!-- Left Killshot -->
		<g class="kill-group" class:armed={isClutchCalled}>
			<circle
				cx={-KILL_X}
				cy={KILL_Y}
				r={R_KILL}
				fill="#0891b2"
				stroke={isClutchCalled ? '#22d3ee' : '#155e75'}
				stroke-width="5"
				filter={isClutchCalled ? 'url(#killGlow)' : ''}
			/>
			<circle cx={-KILL_X} cy={KILL_Y} r="8" fill="#ffffff" />
			<text x={-KILL_X} y={KILL_Y + 52} text-anchor="middle" fill="#06b6d4" font-family="'Chakra Petch', sans-serif" font-weight="800" font-size="16">
				KILL (8)
			</text>
		</g>

		<!-- Right Killshot -->
		<g class="kill-group" class:armed={isClutchCalled}>
			<circle
				cx={KILL_X}
				cy={KILL_Y}
				r={R_KILL}
				fill="#0891b2"
				stroke={isClutchCalled ? '#22d3ee' : '#155e75'}
				stroke-width="5"
				filter={isClutchCalled ? 'url(#killGlow)' : ''}
			/>
			<circle cx={KILL_X} cy={KILL_Y} r="8" fill="#ffffff" />
			<text x={KILL_X} y={KILL_Y + 52} text-anchor="middle" fill="#06b6d4" font-family="'Chakra Petch', sans-serif" font-weight="800" font-size="16">
				KILL (8)
			</text>
		</g>

		<!-- Optional Arcade Mode: 3x3 Tic-Tac-Toe Grid Overlay -->
		{#if overlayMode === 'tic_tac_toe'}
			<g class="ttt-grid-overlay">
				<!-- Grid lines -->
				<line x1="-150" y1="-225" x2="-150" y2="225" stroke="#f59e0b" stroke-width="4" opacity="0.6" />
				<line x1="150" y1="-225" x2="150" y2="225" stroke="#f59e0b" stroke-width="4" opacity="0.6" />
				<line x1="-225" y1="-75" x2="225" y2="-75" stroke="#f59e0b" stroke-width="4" opacity="0.6" />
				<line x1="-225" y1="75" x2="225" y2="75" stroke="#f59e0b" stroke-width="4" opacity="0.6" />

				<!-- Grid cell markings -->
				{#each tttGrid as mark, idx}
					{@const col = idx % 3}
					{@const row = Math.floor(idx / 3)}
					{@const cx = (col - 1) * 150}
					{@const cy = (row - 1) * 150}
					{#if mark}
						<text
							x={cx}
							y={cy + 18}
							text-anchor="middle"
							fill={mark === 'X' ? '#06b6d4' : '#ef4444'}
							font-size="52"
							font-weight="900"
							font-family="'Chakra Petch', sans-serif"
						>
							{mark}
						</text>
					{/if}
				{/each}
			</g>
		{/if}

		<!-- Scatter Throw Heatmap Markers -->
		{#if scatterThrows && scatterThrows.length > 0}
			<g class="scatter-heatmap-group">
				{#each scatterThrows as throwPin, i (i)}
					{#if throwPin.x != null && throwPin.y != null}
						{@const pinX = throwPin.x * HALF}
						{@const pinY = -throwPin.y * HALF}
						<g class="scatter-pin" transform="translate({pinX}, {pinY})">
							<circle cx="0" cy="0" r="13" fill={throwPin.color || '#f59e0b'} stroke="#ffffff" stroke-width="2.5" opacity="0.9" />
							<text x="0" y="4.5" text-anchor="middle" font-size="11" font-weight="900" fill="#000000" font-family="'Chakra Petch', sans-serif">
								{throwPin.pointsAwarded ?? ''}
							</text>
						</g>
					{/if}
				{/each}
			</g>
		{/if}

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
		position: relative;
	}

	.line-break-modal {
		position: absolute;
		top: 12px;
		left: 50%;
		transform: translateX(-50%);
		z-index: 50;
		background: rgba(15, 23, 42, 0.95);
		backdrop-filter: blur(12px);
		border: 1px solid rgba(245, 158, 11, 0.4);
		border-radius: 12px;
		padding: 12px 18px;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 8px;
		box-shadow: 0 16px 32px rgba(0, 0, 0, 0.6);
		width: 90%;
		max-width: 380px;
	}

	.line-break-badge {
		font-size: 11px;
		font-weight: 900;
		letter-spacing: 0.08em;
		color: #f59e0b;
		background: rgba(245, 158, 11, 0.12);
		padding: 2px 8px;
		border-radius: 4px;
	}

	.line-break-text {
		font-size: 13px;
		color: #e2e8f0;
		text-align: center;
		margin: 0;
	}

	.line-break-actions {
		display: flex;
		gap: 8px;
		width: 100%;
	}

	.btn-award-higher {
		flex: 1;
		background: #f59e0b;
		color: #0f172a;
		border: none;
		border-radius: 6px;
		font-size: 12px;
		font-weight: 800;
		padding: 8px 12px;
		cursor: pointer;
	}

	.btn-lower {
		background: rgba(255, 255, 255, 0.1);
		color: #94a3b8;
		border: 1px solid rgba(255, 255, 255, 0.15);
		border-radius: 6px;
		font-size: 12px;
		padding: 8px 12px;
		cursor: pointer;
	}

	.watl-svg {
		width: 100%;
		height: 100%;
		border-radius: 50%;
	}

	.watl-svg.clickable {
		cursor: crosshair;
	}

	.kill-group.armed circle {
		animation: pulse-kill 1.2s infinite alternate;
	}

	@keyframes pulse-kill {
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
