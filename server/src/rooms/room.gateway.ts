import {
  ConnectedSocket,
  MessageBody,
  OnGatewayConnection,
  OnGatewayDisconnect,
  SubscribeMessage,
  WebSocketGateway,
  WebSocketServer,
} from '@nestjs/websockets';
import { Logger } from '@nestjs/common';
import { Server, WebSocket } from 'ws';
import { randomUUID } from 'crypto';
import { RoomService } from './room.service';
import { GameService } from '../games/game.service';

@WebSocketGateway({ path: '/rooms' })
export class RoomGateway implements OnGatewayConnection, OnGatewayDisconnect {
  constructor(
    private readonly roomService: RoomService,
    private readonly gameService: GameService,
  ) {}

  @WebSocketServer()
  server!: Server;

  private readonly logger = new Logger(RoomGateway.name);
  private readonly roomClients = new Map<string, Set<WebSocket>>();
  private readonly clientRoom = new Map<WebSocket, string>();
  private readonly clientUid = new Map<WebSocket, string>();

  handleConnection(_client: WebSocket) {
    this.logger.log(`Client connected — clients: ${this.server.clients.size}`);
  }

  handleDisconnect(client: WebSocket) {
    const roomId = this.clientRoom.get(client);
    const uid = this.clientUid.get(client);

    if (roomId && uid) {
      try {
        this.roomService.leaveRoom(roomId, uid);
        const room = this.roomService.getRoom(roomId);
        this.broadcast(roomId, 'roomUpdate', room);
      } catch {}
    }

    this.removeClient(client);
  }

  @SubscribeMessage('createRoom')
  handleCreateRoom(
    @MessageBody() data: { playerId: string; maxPlayer?: number },
    @ConnectedSocket() client: WebSocket,
  ) {
    try {
      const uid = randomUUID();
      const result = this.roomService.createRoom(
        data.playerId,
        data.maxPlayer,
        uid,
      );
      this.addClient(client, result.room.id, uid);
      this.broadcastAvailableRooms();
      return { event: 'createRoom', data: result };
    } catch (e) {
      return this.errorResponse(e);
    }
  }

  @SubscribeMessage('joinRoom')
  handleJoinRoom(
    @MessageBody() data: { roomId: string; playerId: string },
    @ConnectedSocket() client: WebSocket,
  ) {
    try {
      const uid = randomUUID();
      const { room } = this.roomService.joinRoom(
        data.roomId,
        data.playerId,
        uid,
      );
      this.addClient(client, room.id, uid);
      this.broadcast(data.roomId, 'roomUpdate', room);
      this.broadcastAvailableRooms();
      return { event: 'joinRoom', data: { room, uid } };
    } catch (e) {
      return this.errorResponse(e);
    }
  }

  @SubscribeMessage('leaveRoom')
  handleLeaveRoom(
    @MessageBody() data: { roomId: string; uid: string },
    @ConnectedSocket() client: WebSocket,
  ) {
    try {
      this.roomService.leaveRoom(data.roomId, data.uid);
      this.removeClient(client);
      try {
        const room = this.roomService.getRoom(data.roomId);
        this.broadcast(data.roomId, 'roomUpdate', room);
      } catch {}
      this.broadcastAvailableRooms();
      return { event: 'leaveRoom', data: {} };
    } catch (e) {
      return this.errorResponse(e);
    }
  }

  @SubscribeMessage('setReady')
  handleSetReady(@MessageBody() data: { roomId: string; uid: string }) {
    try {
      const room = this.roomService.setReady(data.roomId, data.uid);
      this.broadcast(data.roomId, 'roomUpdate', room);
      return { event: 'setReady', data: room };
    } catch (e) {
      return this.errorResponse(e);
    }
  }

  @SubscribeMessage('quickMatch')
  handleQuickMatch(
    @MessageBody() data: { playerId: string; maxPlayers?: number },
    @ConnectedSocket() client: WebSocket,
  ) {
    try {
      const maxPlayers = data.maxPlayers ?? 2;
      const uid = randomUUID();
      const availableRooms = this.roomService.getAvailableRooms(maxPlayers);

      if (availableRooms.length === 0) {
        const result = this.roomService.createRoom(
          data.playerId,
          maxPlayers,
          uid,
        );
        this.addClient(client, result.room.id, uid);
        this.broadcastAvailableRooms();
        return { event: 'quickMatch', data: result };
      }

      const { room } = this.roomService.joinRoom(
        availableRooms[0].id,
        data.playerId,
        uid,
      );
      this.addClient(client, room.id, uid);
      this.roomService.setReady(room.id, uid);
      this.broadcast(room.id, 'roomUpdate', this.roomService.getRoom(room.id));
      this.broadcastAvailableRooms();
      return { event: 'quickMatch', data: { room, uid } };
    } catch (e) {
      return this.errorResponse(e);
    }
  }

  @SubscribeMessage('startGame')
  handleStartGame(
    @MessageBody()
    {
      roomId,
      uid,
      mapName = 'JJS',
    }: {
      roomId: string;
      uid: string;
      mapName?: string;
    },
  ) {
    try {
      const sessionId = this.roomService.startGame(roomId, uid);
      const room = this.roomService.getRoom(roomId);
      this.gameService.createSession(
        sessionId,
        room.players.map((p) => p.uid),
        mapName,
      );
      const session = this.gameService.getSession(sessionId);
      const pick = ({
        uid,
        x,
        y,
        z,
        r,
      }: {
        uid: string;
        x: number;
        y: number;
        z: number;
        r: number;
      }) => ({ uid, x, y, z, r });
      const playerPoses = Array.from(session.players.values()).map(pick);
      const npcPoses = Array.from(session.npcs.values()).map(pick);
      this.broadcast(roomId, 'gameStart', { sessionId, playerPoses, npcPoses });
      this.broadcastAvailableRooms();
    } catch (e) {
      return this.errorResponse(e);
    }
  }

  @SubscribeMessage('getAvailableRooms')
  handleGetAvailableRooms(@MessageBody() data?: { maxPlayers?: number }) {
    return {
      event: 'getAvailableRooms',
      data: this.roomService.getAvailableRooms(data?.maxPlayers),
    };
  }

  @SubscribeMessage('getRoom')
  handleGetRoom(@MessageBody() data: { roomId: string }) {
    try {
      return { event: 'getRoom', data: this.roomService.getRoom(data.roomId) };
    } catch (e) {
      return this.errorResponse(e);
    }
  }

  private broadcastAvailableRooms() {
    this.broadcastAll('availableRooms', this.roomService.getAvailableRooms());
  }

  private broadcastAll(event: string, data: unknown) {
    const payload = JSON.stringify({ event, data });
    this.server.clients.forEach((c) => {
      if (c.readyState === WebSocket.OPEN) c.send(payload);
    });
  }

  private broadcast(roomId: string, event: string, data: unknown) {
    const clients = this.roomClients.get(roomId);
    if (!clients) return;
    const payload = JSON.stringify({ event, data });
    clients.forEach((c) => {
      if (c.readyState === WebSocket.OPEN) c.send(payload);
    });
  }

  private addClient(client: WebSocket, roomId: string, uid: string) {
    if (!this.roomClients.has(roomId)) this.roomClients.set(roomId, new Set());
    this.roomClients.get(roomId)!.add(client);
    this.clientRoom.set(client, roomId);
    this.clientUid.set(client, uid);
  }

  private removeClient(client: WebSocket) {
    const roomId = this.clientRoom.get(client);
    if (roomId) {
      this.roomClients.get(roomId)?.delete(client);
      if (this.roomClients.get(roomId)?.size === 0)
        this.roomClients.delete(roomId);
    }
    this.clientRoom.delete(client);
    this.clientUid.delete(client);
  }

  private errorResponse(e: unknown) {
    return { event: 'error', data: { message: (e as Error).message } };
  }
}
