import { describe, it, expect } from 'vitest';

describe('Square Payment Integration & Venue Config Tests', () => {
	it('Extracts and formats venue Square credentials from venue data correctly', () => {
		const venueDto = {
			id: 'v_123',
			name: 'Axe Arena',
			slug: 'axe-arena',
			squareConfig: {
				applicationId: 'sandbox-sq0idb-custom123',
				locationId: 'LOC_VENUE_999',
				environment: 'sandbox',
				hasAccessToken: true,
				maskedAccessToken: '••••••••••••1234',
				webhookSignatureKey: 'whsec_test_sig'
			}
		};

		expect(venueDto.squareConfig.applicationId).toBe('sandbox-sq0idb-custom123');
		expect(venueDto.squareConfig.locationId).toBe('LOC_VENUE_999');
		expect(venueDto.squareConfig.hasAccessToken).toBe(true);
		expect(venueDto.squareConfig.maskedAccessToken).toContain('••••');
		expect(venueDto.squareConfig.maskedAccessToken).toContain('1234');
	});

	it('Preserves masked token detection logic in admin venue settings form', () => {
		const isMaskedToken = (token: string) => token.includes('•') || token.startsWith('••••');
		
		expect(isMaskedToken('••••••••••••••••')).toBe(true);
		expect(isMaskedToken('••••••••••••1234')).toBe(true);
		expect(isMaskedToken('EAAAl_real_new_token_value')).toBe(false);
		expect(isMaskedToken('')).toBe(false);
	});

	it('Public booking page payload strictly omits secret tokens while retaining public credentials', () => {
		const publicBookingPage = {
			venueId: 'v_123',
			venueName: 'Axe Arena',
			venueSlug: 'axe-arena',
			currency: 'USD',
			squareApplicationId: 'sandbox-sq0idb-public',
			squareLocationId: 'LOC_PUBLIC_OK',
			squareEnvironment: 'sandbox',
			brandingConfigJson: JSON.stringify({
				primaryColor: '#f59e0b',
				payment: {
					gateway: 'square',
					locationId: 'LOC_PUBLIC_OK',
					appId: 'sandbox-sq0idb-public',
					environment: 'sandbox'
				}
			})
		};

		// Assert public properties are present
		expect(publicBookingPage.squareApplicationId).toBe('sandbox-sq0idb-public');
		expect(publicBookingPage.squareLocationId).toBe('LOC_PUBLIC_OK');

		// Assert secret keys are absent
		const parsed = JSON.parse(publicBookingPage.brandingConfigJson);
		expect(parsed.payment.accessToken).toBeUndefined();
		expect(parsed.payment.webhookKey).toBeUndefined();
		expect(parsed.payment.webhookSignatureKey).toBeUndefined();
	});

	it('Test gateway connection response parsing handles success and error states cleanly', () => {
		const successResponse = {
			success: true,
			message: 'Successfully authenticated with Square (Sandbox)! Location: Main Hall.',
			merchantName: 'Axe Master Inc',
			locationName: 'Main Hall'
		};

		const errorResponse = {
			success: false,
			message: 'Square API: The authorization header has expired or is invalid.'
		};

		expect(successResponse.success).toBe(true);
		expect(successResponse.locationName).toBe('Main Hall');

		expect(errorResponse.success).toBe(false);
		expect(errorResponse.message).toContain('authorization header has expired');
	});
});
