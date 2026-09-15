import { getApiAdminVenues } from '$lib/api/client';
import type { VenueDto } from '$lib/api/generated/types.gen';

class VenueState {
	venues = $state<VenueDto[]>([]);
	selectedVenue = $state<VenueDto | null>(null);
	isLoading = $state<boolean>(false);
	showCreateVenueModal = $state<boolean>(false);
	currentUserId = $state<string | null>(null);

	reset() {
		this.venues = [];
		this.selectedVenue = null;
		this.isLoading = false;
		this.showCreateVenueModal = false;
		this.currentUserId = null;
		if (typeof localStorage !== 'undefined') {
			const keysToRemove: string[] = [];
			for (let i = 0; i < localStorage.length; i++) {
				const k = localStorage.key(i);
				if (k && k.startsWith('venueaxe_selected_venue')) {
					keysToRemove.push(k);
				}
			}
			keysToRemove.forEach((k) => localStorage.removeItem(k));
		}
	}

	async loadVenues(userId?: string | null, lockedVenueId?: string | null) {
		this.isLoading = true;
		this.currentUserId = userId ?? null;
		try {
			const res = await getApiAdminVenues();
			if (res.data && res.data.length > 0) {
				this.venues = res.data;

				// If user is assigned to a specific venue, always lock to it
				if (lockedVenueId) {
					const matched = this.venues.find((v) => v.id === lockedVenueId);
					if (matched) {
						this.selectedVenue = matched;
						return;
					}
				}

				// Check per-user namespaced saved venue strictly
				let savedId: string | null = null;
				if (typeof localStorage !== 'undefined' && userId) {
					savedId = localStorage.getItem(`venueaxe_selected_venue_${userId}`);
				}

				if (savedId && this.venues.some((v) => v.id === savedId)) {
					this.selectedVenue = this.venues.find((v) => v.id === savedId) || this.venues[0];
				} else {
					this.selectedVenue = this.venues[0];
				}
			} else {
				this.venues = [];
				this.selectedVenue = null;
			}
		} catch (e) {
			console.error('Failed to load venues', e);
			this.venues = [];
			this.selectedVenue = null;
		} finally {
			this.isLoading = false;
		}
	}

	setSelectedVenueId(venueId: string) {
		if (venueId === '__new__') {
			this.showCreateVenueModal = true;
			return;
		}
		const found = this.venues.find((v) => v.id === venueId);
		if (found) {
			this.selectedVenue = found;
			if (typeof localStorage !== 'undefined' && this.currentUserId) {
				localStorage.setItem(`venueaxe_selected_venue_${this.currentUserId}`, found.id);
			}
		}
	}
}

export const venueState = new VenueState();
