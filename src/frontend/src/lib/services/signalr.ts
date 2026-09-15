import * as signalR from '@microsoft/signalr';
import type { GameStateSnapshot, LaneDto } from '$lib/api/generated/types.gen';

export class LaneSignalRService {
    private connection: signalR.HubConnection | null = null;
    private laneId: string | null = null;

    public onStateChanged: ((laneOrId: any, status?: string) => void) | null = null;
    public onThrowRecorded: ((gameState: GameStateSnapshot) => void) | null = null;
    public onClutchCalled: ((playerId: string, side: string) => void) | null = null;
    public onSafetyStopActivated: ((reason: string) => void) | null = null;
    public onSessionExtended: ((newMinutes: number) => void) | null = null;

    public async connect(laneId: string, hubUrl = '/hubs/lane'): Promise<void> {
        this.laneId = laneId;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                withCredentials: true,
                skipNegotiation: false
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.connection.on('OnLaneStateChanged', (laneOrId: any, status?: string) => {
            this.onStateChanged?.(laneOrId, status);
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

        this.connection.onreconnected(async () => {
            if (this.laneId) {
                try {
                    await this.connection?.invoke('JoinLaneGroup', this.laneId);
                } catch (e) {
                    console.error('Failed to re-join lane group after reconnect:', e);
                }
            }
        });

        await this.connection.start();
        await this.connection.invoke('JoinLaneGroup', laneId);
    }

    public async sendHeartbeat(laneId: string, isScreen: boolean): Promise<void> {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                await this.connection.invoke('SendHeartbeat', laneId, isScreen);
            } catch (e) {
                // Ignore heartbeat network drops
            }
        }
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

export function createAdminHubConnection(hubUrl = '/hubs/lane') {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
            withCredentials: true,
            skipNegotiation: false
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    connection.onreconnected(async () => {
        try {
            await connection.invoke('JoinAdminGroup');
        } catch (e) {
            console.error('Failed to re-join admin group after reconnect:', e);
        }
    });

    return connection;
}
