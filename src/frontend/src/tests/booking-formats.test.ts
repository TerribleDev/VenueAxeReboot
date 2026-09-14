import { describe, it, expect } from 'vitest';

export interface BookingTypeItem {
	id: string;
	name: string;
	description: string;
	minPartySize?: number;
	allowAfterHoursBooking?: boolean;
	allowOffDaysBooking?: boolean;
}

export function createBookingFormat(
	existingFormats: BookingTypeItem[],
	draft: {
		name: string;
		description?: string;
		minPartySize?: number;
		allowAfterHours?: boolean;
		allowOffDays?: boolean;
	}
): { updatedFormats: BookingTypeItem[]; createdFormat: BookingTypeItem | null } {
	if (!draft.name || !draft.name.trim()) {
		return { updatedFormats: existingFormats, createdFormat: null };
	}

	const id = 'bt_' + draft.name.trim().toLowerCase().replace(/[^a-z0-9]/g, '_');
	const createdFormat: BookingTypeItem = {
		id,
		name: draft.name.trim(),
		description: draft.description?.trim() || 'Custom booking format',
		minPartySize: Number(draft.minPartySize) || 2,
		allowAfterHoursBooking: !!draft.allowAfterHours,
		allowOffDaysBooking: !!draft.allowOffDays
	};

	return {
		updatedFormats: [...existingFormats, createdFormat],
		createdFormat
	};
}

export function deleteBookingFormat(
	existingFormats: BookingTypeItem[],
	idToDelete: string
): BookingTypeItem[] {
	return existingFormats.filter((b) => b.id !== idToDelete);
}

export function serializeBookingFormatsJson(formats: BookingTypeItem[]): string {
	return JSON.stringify(formats, null, 2);
}

describe('Booking Formats Visual Management', () => {
	it('creates a new booking format with sanitized ID and default description', () => {
		const initial: BookingTypeItem[] = [
			{ id: 'bt_standard', name: 'Standard Open Throw', description: 'Standard reservation', minPartySize: 2 }
		];

		const { updatedFormats, createdFormat } = createBookingFormat(initial, {
			name: 'Private Corporate Buyout',
			minPartySize: 10,
			allowAfterHours: true,
			allowOffDays: true
		});

		expect(createdFormat).not.toBeNull();
		expect(createdFormat?.id).toBe('bt_private_corporate_buyout');
		expect(createdFormat?.name).toBe('Private Corporate Buyout');
		expect(createdFormat?.description).toBe('Custom booking format');
		expect(createdFormat?.minPartySize).toBe(10);
		expect(createdFormat?.allowAfterHoursBooking).toBe(true);
		expect(createdFormat?.allowOffDaysBooking).toBe(true);
		expect(updatedFormats).toHaveLength(2);
	});

	it('rejects creating a booking format with empty name', () => {
		const initial: BookingTypeItem[] = [];
		const { updatedFormats, createdFormat } = createBookingFormat(initial, {
			name: '   '
		});

		expect(createdFormat).toBeNull();
		expect(updatedFormats).toHaveLength(0);
	});

	it('deletes an existing booking format by ID', () => {
		const formats: BookingTypeItem[] = [
			{ id: 'bt_standard', name: 'Standard', description: '', minPartySize: 2 },
			{ id: 'bt_glow', name: 'Glow Axe', description: '', minPartySize: 4 }
		];

		const afterDelete = deleteBookingFormat(formats, 'bt_glow');
		expect(afterDelete).toHaveLength(1);
		expect(afterDelete[0].id).toBe('bt_standard');
	});

	it('serializes and deserializes booking formats JSON accurately', () => {
		const formats: BookingTypeItem[] = [
			{
				id: 'bt_league',
				name: 'League Practice',
				description: 'Dedicated lane practice for league members',
				minPartySize: 1,
				allowAfterHoursBooking: true,
				allowOffDaysBooking: false
			}
		];

		const json = serializeBookingFormatsJson(formats);
		const parsed = JSON.parse(json);

		expect(parsed).toHaveLength(1);
		expect(parsed[0].id).toBe('bt_league');
		expect(parsed[0].allowAfterHoursBooking).toBe(true);
		expect(parsed[0].allowOffDaysBooking).toBe(false);
	});
});
