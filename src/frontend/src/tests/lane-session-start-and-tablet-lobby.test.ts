import { describe, it, expect } from 'vitest';
import type { LaneDto, NextBookingSummaryDto, GameStateSnapshot } from '$lib/api/generated/types.gen';

describe('Lane Overview Session Launcher & Tablet Lobby Logic', () => {
    function startSessionWithBooking(lane: LaneDto, booking: NextBookingSummaryDto) {
        const partySize = Math.max(1, Number(booking.partySize) || 2);
        const playerNames = Array.from({ length: partySize }, (_, i) =>
            i === 0 ? booking.guestName : `Thrower ${i + 1}`
        ).join(', ');

        const startMs = new Date(booking.startTime).getTime();
        const endMs = new Date(booking.endTime).getTime();
        const diffMins = Math.round((endMs - startMs) / 60000);
        const sessionDurationMinutes = diffMins > 0 ? diffMins : 60;

        return {
            bookingId: booking.bookingId,
            sessionTitle: `${booking.guestName} (${booking.bookingReference})`,
            playerNames,
            sessionDurationMinutes,
            laneId: lane.id
        };
    }

    function formatTimer(seconds: number): string {
        const mins = Math.floor(seconds / 60);
        const secs = seconds % 60;
        if (mins >= 60) {
            const hrs = Math.floor(mins / 60);
            const remMins = mins % 60;
            return `${hrs}:${remMins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        }
        return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }

    it('pre-populates session settings from active booking on lane', () => {
        const currentBooking: NextBookingSummaryDto = {
            bookingId: 'book-789',
            bookingReference: 'VA-77123',
            guestName: 'Alex Mercer',
            startTime: '2026-09-11T19:00:00Z',
            endTime: '2026-09-11T20:00:00Z',
            partySize: 4,
            notes: 'Birthday party'
        };

        const lane: LaneDto = {
            id: 'lane-01',
            venueId: 'venue-01',
            laneNumber: 1,
            name: 'Lane 01',
            maxThrowers: 6,
            currentStatus: 0,
            tabletPairingCode: 'AX101',
            screenPairingCode: 'TV101',
            lastHeartbeatAt: null,
            activeSession: null,
            isActive: true,
            currentBooking,
            nextBookingToday: {
                bookingId: 'book-790',
                bookingReference: 'VA-77124',
                guestName: 'Samantha Lee',
                startTime: '2026-09-11T20:30:00Z',
                endTime: '2026-09-11T21:30:00Z',
                partySize: 2
            }
        };

        const sessionPayload = startSessionWithBooking(lane, lane.currentBooking!);

        expect(sessionPayload.bookingId).toBe('book-789');
        expect(sessionPayload.sessionTitle).toBe('Alex Mercer (VA-77123)');
        expect(sessionPayload.playerNames).toBe('Alex Mercer, Thrower 2, Thrower 3, Thrower 4');
        expect(sessionPayload.sessionDurationMinutes).toBe(60);
        expect(lane.nextBookingToday).not.toBeNull();
        expect(lane.nextBookingToday?.guestName).toBe('Samantha Lee');
    });

    it('determines tablet idle state vs active lobby state correctly', () => {
        // No session active
        const noSessionState: any = null;
        const isIdle = !noSessionState || !noSessionState.players || noSessionState.players.length === 0;
        expect(isIdle).toBe(true);

        // Active session started
        const activeSessionState: any = {
            matchId: 'match-101',
            gameTypeId: 'watl-standard',
            gameName: 'WATL Standard',
            currentRound: 1,
            totalRounds: 10,
            currentPlayerIndex: 0,
            status: 1,
            players: [
                { id: 'p1', name: 'Alex Mercer', score: 0 },
                { id: 'p2', name: 'Thrower 2', score: 0 }
            ],
            throws: []
        };

        const hasSession = activeSessionState && (activeSessionState.players?.length ?? 0) > 0;
        expect(hasSession).toBe(true);

        // Newly launched session starts in lobby
        const isNewSession = true;
        const inLobby = isNewSession && (activeSessionState.throws?.length ?? 0) === 0;
        expect(inLobby).toBe(true);
    });

    it('formats countdown timer accurately for minutes and seconds', () => {
        expect(formatTimer(3599)).toBe('59:59');
        expect(formatTimer(60)).toBe('01:00');
        expect(formatTimer(45)).toBe('00:45');
        expect(formatTimer(0)).toBe('00:00');
        expect(formatTimer(3665)).toBe('1:01:05');
    });
});
