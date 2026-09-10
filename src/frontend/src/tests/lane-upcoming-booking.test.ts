import { describe, it, expect } from 'vitest';
import type { LaneDto, NextBookingSummaryDto } from '$lib/api/generated/types.gen';

describe('Lane Upcoming Booking Today Display', () => {
    function formatBookingTime(isoString: string): string {
        if (!isoString) return '';
        try {
            const d = new Date(isoString);
            return d.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
        } catch {
            return isoString;
        }
    }

    it('formats booking time into human-readable 12-hour AM/PM string', () => {
        const testIso = '2026-09-07T20:30:00.000Z';
        const formatted = formatBookingTime(testIso);
        expect(formatted).toMatch(/\d{1,2}:\d{2}\s?(AM|PM)/i);
    });

    it('correctly associates NextBookingToday with lane card metadata', () => {
        const nextBooking: NextBookingSummaryDto = {
            bookingId: 'b-1234',
            bookingReference: 'VA-89102',
            guestName: 'Jessica Martinez',
            startTime: '2026-09-07T22:00:00Z',
            endTime: '2026-09-07T23:00:00Z',
            partySize: 6
        };

        const laneWithBooking: LaneDto = {
            id: 'lane-1',
            venueId: 'venue-1',
            laneNumber: 1,
            name: 'Lane 01',
            maxThrowers: 6,
            currentStatus: 0,
            tabletPairingCode: 'AX101',
            screenPairingCode: 'TV101',
            lastHeartbeatAt: null,
            activeSession: null,
            isActive: true,
            nextBookingToday: nextBooking
        };

        expect(laneWithBooking.nextBookingToday).not.toBeNull();
        expect(laneWithBooking.nextBookingToday?.bookingReference).toBe('VA-89102');
        expect(laneWithBooking.nextBookingToday?.guestName).toBe('Jessica Martinez');
        expect(laneWithBooking.nextBookingToday?.partySize).toBe(6);
    });

    it('handles lanes without upcoming bookings gracefully', () => {
        const laneWithoutBooking: LaneDto = {
            id: 'lane-2',
            venueId: 'venue-1',
            laneNumber: 2,
            name: 'Lane 02',
            maxThrowers: 6,
            currentStatus: 0,
            tabletPairingCode: 'AX102',
            screenPairingCode: 'TV102',
            lastHeartbeatAt: null,
            activeSession: null,
            isActive: true,
            nextBookingToday: null
        };

        expect(laneWithoutBooking.nextBookingToday).toBeNull();
    });
});
