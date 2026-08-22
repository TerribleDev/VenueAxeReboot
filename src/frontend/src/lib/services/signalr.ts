import * as signalR from '@microsoft/signalr';
import type { GameStateSnapshot, LaneDto } from '$lib/api/generated/types.gen';

export class LaneSignalRService {
    private connection: signalR.HubConnection | null = null;
    private laneId: string | null = null;

    public onStateChanged: ((lane: LaneDto) => void) | null = null;
    public onThrowRecorded: ((gameState: GameStateSnapshot) => void) | null = null;
    public onClutchCalled: ((playerId: string, side: string) => void) | null = null;
    public onSafetyStopActivated: ((reason: string) => void) | null = null;
    public onSessionExtended: ((newMinutes: number) => void) | null = null;

    public async connect(laneId: string, hubUrl = 'http://localhost:5280/hubs/lane'): Promise<void> {
        this.laneId = laneId;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                withCredentials: true,
                skipNegotiation: false
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.connection.on('OnLaneStateChanged', (lane: LaneDto) => {
            this.onStateChanged?.(lane);
        });

        this.connection.on('OnThrowRecorded', (gameState: GameStateSnapshot) => {
            this.onThrowRecorded?.(gameState);
        });

        this.connection.on('OnClutchCalled', (playerId: string, side: string) => {
            this.onClutchCalled?.(playerId, side);
        });

        this.connection.on('OnSafetyStopActivated', (reason: string) => {
            this.onSafetyStopActivated?.(reason);
        });

        this.connection.on('OnSessionExtended', (mins: number) => {
            this.onSessionExtended?.(mins);
        });

        await this.connection.start();
        await this.connection.invoke('JoinLaneGroup', laneId);
    }

    public async disconnect(): Promise<void> {
        if (this.connection && this.laneId) {
            try {
                await this.connection.invoke('LeaveLaneGroup', this.laneId);
            } catch (e) {
                // Ignore disconnect errors
            }
            await this.connection.stop();
            this.connection = null;
        }
    }
}

export const laneSignalR = new LaneSignalRService();
