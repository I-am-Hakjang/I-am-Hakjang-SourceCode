import {
  ConnectedSocket,
  MessageBody,
  OnGatewayConnection,
  OnGatewayDisconnect,
  SubscribeMessage,
  WebSocketGateway,
  WebSocketServer,
} from '@nestjs/websockets';
import { Logger, OnModuleInit } from '@nestjs/common';
import { Server, WebSocket } from 'ws';
import { IncomingMessage } from 'http';
import { CharacterCoordinate, TensionEvalResult, TensionEventListener } from './game.types';
import { GameService } from './game.service';

@WebSocketGateway({ path: '/games' })
export class GameGateway
  implements OnGatewayConnection, OnGatewayDisconnect, OnModuleInit, TensionEventListener {
  constructor(private readonly gameService: GameService) { }

  @WebSocketServer()
  server!: Server;

  private readonly logger = new Logger(GameGateway.name);
  private readonly sessionClients = new Map<string, Set<WebSocket>>();
  private readonly clientSession = new Map<WebSocket, string>();
  private readonly clientPlayerUid = new Map<WebSocket, string>();

  private readonly TICK_RATE = 33;
  private lastTickTime = Date.now();

  onModuleInit() {
    this.gameService.addTensionEventListener(this);
    setInterval(() => this.gameLoop(), this.TICK_RATE);
  }

  // ── Observer: TensionEventListener 구현 ──────────────────────────────────

  onTensionEvaluated(sessionId: string, result: TensionEvalResult): void {
    const clients = this.sessionClients.get(sessionId);
    if (!clients) return;

    this.broadcastToSession(clients, 'tensionUpdate', {
      zones: result.zones.map((z) => ({ zoneId: z.zoneId, tension: z.tension })),
    });

    for (const zone of result.zones) {
      if (!zone.triggeredEvent) continue;
      const center = this.gameService.getZoneCenter(sessionId, zone.zoneId);
      this.broadcastToSession(clients, 'event', {
        type: zone.triggeredEvent,
        x: center.x,
        y: 0,
        z: center.z,
        timestamp: Date.now(),
      });
      if (zone.triggeredEvent === 'SIREN') {
        this.gameService.activateSiren(sessionId);
      } else if (zone.triggeredEvent === 'WHOAREYOU') {
        this.gameService.activateWhoAreYou(sessionId);
      }
    }
  }

  onTensionEventExpired(sessionId: string, zoneId: string): void {
    const clients = this.sessionClients.get(sessionId);
    if (!clients) return;
    this.broadcastToSession(clients, 'tensionEventEnd', { zoneId });
  }

  // ── 게임 루프 ─────────────────────────────────────────────────────────────

  private gameLoop() {
    const now = Date.now();
    const deltaMs = now - this.lastTickTime;
    this.lastTickTime = now;

    // 1. NPC 위치 갱신
    this.gameService.updateNpcPositions(deltaMs);

    for (const session of this.gameService.getAllSessions()) {
      const clients = this.sessionClients.get(session.sessionId);
      if (!clients || clients.size === 0) continue;

      // 2. 텐션 틱 — 이벤트 만료·주기적 평가·리스너 알림 포함
      this.gameService.tickTension(session.sessionId, deltaMs);

      // 2-1. 사이렌 만료 체크
      this.gameService.checkSirenExpiry(session.sessionId);

      // 2-2. WHOAREYOU 만료 체크
      this.gameService.checkWhoAreYouExpiry(session.sessionId);

      // 4. unitSync 브로드캐스트
      const toSync = ({
        uid,
        state,
        x,
        y,
        z,
        r,
      }: {
        uid: string;
        state: number;
        x: number;
        y: number;
        z: number;
        r: number;
      }) => ({ uid, state, x, y, z, r });

      const payload = JSON.stringify({
        event: 'unitSync',
        data: [
          ...Array.from(session.players.values())
            .filter((p) => p.alive)
            .map(toSync),
          ...Array.from(session.npcs.values())
            .filter((n) => n.alive)
            .map(toSync),
        ],
      });

      clients.forEach((client) => {
        if (client.readyState === WebSocket.OPEN) client.send(payload);
      });
    }
  }

  private broadcastToSession(
    clients: Set<WebSocket>,
    event: string,
    data: unknown,
  ) {
    const payload = JSON.stringify({ event, data });
    clients.forEach((client) => {
      if (client.readyState === WebSocket.OPEN) client.send(payload);
    });
  }

  handleConnection(client: WebSocket, request: IncomingMessage) {
    const url = new URL(request.url!, 'http://localhost');
    const sessionId = url.searchParams.get('sessionId');
    const playerUid = url.searchParams.get('playerUid');

    if (!sessionId || !this.gameService.validateSession(sessionId)) {
      client.close(1008, 'Invalid session');
      return;
    }
    if (!playerUid || !this.gameService.validatePlayer(sessionId, playerUid)) {
      client.close(1008, 'Invalid player');
      return;
    }

    this.registerClient(client, sessionId, playerUid);

    const ip = request.socket.remoteAddress ?? 'unknown';
    this.logger.log(
      `Client connected — ip: ${ip}, session: ${sessionId}, playerUid: ${playerUid}`,
    );
    client.send(JSON.stringify({ event: 'connected' }));
  }

  handleDisconnect(client: WebSocket) {
    const sessionId = this.clientSession.get(client);
    this.deregisterClient(client);
    if (sessionId) {
      this.logger.log(`Client disconnected — session: ${sessionId}`);
    }
  }

  @SubscribeMessage('unitSync')
  handleUpdateCoordinate(
    @MessageBody()
    data: { state: number; x: number; y: number; z: number; r: number },
    @ConnectedSocket() client: WebSocket,
  ) {
    const sessionId = this.clientSession.get(client);
    const playerUid = this.clientPlayerUid.get(client);
    if (!sessionId || !playerUid) return;

    const coordinate: CharacterCoordinate = {
      uid: playerUid,
      ...data,
      alive: true,
    };
    this.gameService.updatePlayerCoordinate(sessionId, coordinate);
  }

  @SubscribeMessage('attack')
  handleAttack(
    @MessageBody()
    data: { target: string },
    @ConnectedSocket() client: WebSocket,
  ) {
    const sessionId = this.clientSession.get(client);
    const playerUid = this.clientPlayerUid.get(client);
    if (!sessionId || !playerUid) return;
    this.logger.debug(
      `attack — session: ${sessionId}, player: ${playerUid}, target: ${data.target}`,
    );
    const killResult = this.gameService.killCharacter(
      sessionId,
      playerUid,
      data.target,
    );
    if (killResult) {
      const clients = this.sessionClients.get(sessionId);
      if (clients) {
        this.broadcastToSession(clients, 'kill', {
          killer: playerUid,
          target: data.target,
        });

        const leaderBoard = this.gameService.checkGameOver(sessionId);
        if (leaderBoard) {
          this.broadcastToSession(clients, 'gameOver', leaderBoard);
          this.logger.debug('gameOver', leaderBoard);
        }
      }
      return {
        event: 'killStat',
        data: killResult,
      };
    }
  }
  private registerClient(
    client: WebSocket,
    sessionId: string,
    playerUid: string,
  ) {
    if (!this.sessionClients.has(sessionId))
      this.sessionClients.set(sessionId, new Set());
    this.sessionClients.get(sessionId)!.add(client);
    this.clientSession.set(client, sessionId);
    this.clientPlayerUid.set(client, playerUid);
  }

  private deregisterClient(client: WebSocket) {
    const sessionId = this.clientSession.get(client);
    if (sessionId) {
      const clients = this.sessionClients.get(sessionId);
      clients?.delete(client);
      if (clients?.size === 0) this.sessionClients.delete(sessionId);
    }
    this.clientSession.delete(client);
    this.clientPlayerUid.delete(client);
  }
}
