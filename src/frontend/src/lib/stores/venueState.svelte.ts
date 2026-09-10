import { getApiAdminVenues } from '$lib/api/client';
import type { VenueDto } from '$lib/api/generated/types.gen';

class VenueState {
	venues = $state<VenueDto[]>([]);
	selectedVenue = $state<VenueDto | null>(null);
	isLoading = $state<boolean>(false);
	showCreateVenueModal = $state<boolean>(false);

	async loadVenues() {
		this.isLoading = true;
		try {
			const res = await getApiAdminVenues();
			if (res.data && res.data.length > 0) {
				this.venues = res.data;
				const savedId = typeof localStorage !== 'undefined' ? localStorage.getItem('venueaxe_selected_venue_id') : null;
				if (savedId && this.venues.some((v) => v.id === savedId)) {
					this.selectedVenue = this.venues.find((v) => v.id === savedId) || this.venues[0];
				} else if (!this.selectedVenue || !this.venues.some((v) => v.id === this.selectedVenue?.id)) {
					this.selectedVenue = this.venues[0];
				}
			} else {
				this.venues = [];
				this.selectedVenue = null;
			}
		} catch (e) {
			console.error('Failed to load venues', e);
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
			if (typeof localStorage !== 'undefined') {
				localStorage.setItem('venueaxe_selected_venue_id', found.id);
			}
		}
	}
}

export const venueState = new VenueState();
