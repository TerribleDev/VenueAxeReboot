<script lang="ts">
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';

	onMount(() => {
		goto('/admin/lanes', { replaceState: true });
	});
</script>

<div class="admin-redirecting" style="padding: 2rem; text-align: center;">
	<p class="font-display text-secondary">Loading Lanes Overview...</p>
</div>
