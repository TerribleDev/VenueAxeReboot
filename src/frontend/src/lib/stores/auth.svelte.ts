import { getApiAdminAuthMe, postApiAdminAuthLogin, postApiAdminAuthLogout } from '$lib/api/client';
import type { UserProfileDto } from '$lib/api/generated/types.gen';
import { venueState } from './venueState.svelte';

class AuthState {
	user = $state<UserProfileDto | null>(null);
	isLoading = $state<boolean>(true);
	isAuthenticated = $derived(this.user !== null);

	async init() {
		return this.checkAuth();
	}

	async checkAuth() {
		this.isLoading = true;
		try {
			const res = await getApiAdminAuthMe();
			if (res.data) {
				this.user = res.data;
			} else {
				this.user = null;
			}
		} catch (e) {
			this.user = null;
		} finally {
			this.isLoading = false;
		}
	}

	async login(email: string, password: string): Promise<boolean> {
		try {
			venueState.reset();
			const res = await postApiAdminAuthLogin({
				body: { email, password }
			});
			if (res.data) {
				this.user = res.data;
				await venueState.loadVenues(res.data.id, res.data.venueId);
				return true;
			}
			return false;
		} catch (e) {
			return false;
		}
	}

	async register(payload: {
		organizationName: string;
		venueName: string;
		firstName: string;
		lastName: string;
		email: string;
		password: string;
		city?: string;
		timezone?: string;
	}): Promise<{ success: boolean; error?: string }> {
		try {
			const response = await fetch('/api/admin/auth/register', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				credentials: 'include',
				body: JSON.stringify(payload)
			});

			if (!response.ok) {
				const err = await response.json().catch(() => ({}));
				return { success: false, error: err.message || 'Registration failed' };
			}

			const data = await response.json();
			venueState.reset();
			this.user = data;
			await venueState.loadVenues(data.id, data.venueId);
			return { success: true };
		} catch (e: any) {
			return { success: false, error: e.message || 'Network error' };
		}
	}

	async logout(): Promise<void> {
		try {
			await postApiAdminAuthLogout();
		} catch (e) {
			// ignore
		}
		this.user = null;
		venueState.reset();
	}
}

export const auth = new AuthState();
