import {
  BadRequestException,
  ForbiddenException,
  Injectable,
  Logger,
  NotFoundException,
} from '@nestjs/common';
import { randomUUID } from 'crypto';
import { Player, Room, RoomSummary } from './room.types';

@Injectable()
export class RoomService {
  private readonly logger = new Logger(RoomService.name);
  private readonly rooms = new Map<string, Room>();

  createRoom(
    playerId: string,
    maxPlayer: number = 2,
    uid: string = randomUUID(),
  ): { room: Room; uid: string } {
    if (!playerId) throw new BadRequestException('playerId is required');
    const room: Room = {
      id: randomUUID(),
      hostUid: uid,
      players: [{ uid, id: playerId, isReady: true }],
      maxPlayer,
      status: 'waiting',
    };
    this.rooms.set(room.id, room);
    this.logger.log(`Created — roomId: ${room.id}, host uid: ${uid}`);
    return { room, uid };
  }

  joinRoom(
    roomId: string,
    playerId: string,
    uid: string = randomUUID(),
  ): { room: Room; uid: string } {
    if (!playerId) throw new BadRequestException('playerId is required');
    const room = this.findRoom(roomId);
    if (room.status === 'playing')
      throw new BadRequestException('Game already started');
    if (room.players.length >= room.maxPlayer)
      throw new BadRequestException('This room is full');
    if (room.players.some((p) => p.id === playerId))
      throw new BadRequestException('Already in room');

    room.players.push({ uid, id: playerId, isReady: false });
    this.logger.log(`${playerId} joined — roomId: ${roomId}, uid: ${uid}`);
    return { room, uid };
  }

  leaveRoom(roomId: string, uid: string): void {
    const room = this.findRoom(roomId);
    if (room.status === 'playing')
      throw new BadRequestException('Cannot leave a game in progress');

    const index = room.players.findIndex((p) => p.uid === uid);
    if (index === -1) throw new NotFoundException('Player not in room');

    room.players.splice(index, 1);

    if (room.players.length === 0) {
      this.rooms.delete(roomId);
      this.logger.log(`Deleted — roomId: ${roomId} (no players left)`);
      return;
    }

    if (room.hostUid === uid) {
      room.hostUid = room.players[0].uid;
      this.logger.log(
        `Host transferred — roomId: ${roomId}, new hostUid: ${room.hostUid}`,
      );
    }

    this.logger.log(
      `uid: ${uid} left — roomId: ${roomId}, players: ${room.players.length}`,
    );
  }

  setReady(roomId: string, uid: string): Room {
    const room = this.findRoom(roomId);
    const player = this.findPlayer(room, uid);
    player.isReady = !player.isReady;
    this.logger.log(
      `uid: ${uid} is ${player.isReady ? 'ready' : 'not ready'} — roomId: ${roomId}`,
    );
    return room;
  }

  startGame(roomId: string, uid: string): string {
    const room = this.findRoom(roomId);
    if (room.hostUid !== uid)
      throw new ForbiddenException('Only the host can start the game');
    if (room.status === 'playing')
      throw new BadRequestException('Game already started');
    if (room.players.length < 2)
      throw new BadRequestException('At least 2 players are required');

    const notReady = room.players.filter((p) => !p.isReady);
    if (notReady.length > 0) {
      throw new BadRequestException(
        `Not all players are ready: ${notReady.map((p) => p.uid).join(', ')}`,
      );
    }

    room.status = 'playing';
    this.logger.log(`Game started — roomId: ${roomId}`);
    return roomId;
  }

  getAvailableRooms(maxPlayers?: number): RoomSummary[] {
    return Array.from(this.rooms.values())
      .filter(
        (r) =>
          r.status === 'waiting' &&
          (maxPlayers === undefined || r.maxPlayer === maxPlayers),
      )
      .map((r) => ({
        id: r.id,
        hostUid: r.hostUid,
        playerCount: r.players.length,
        maxPlayer: r.maxPlayer,
      }));
  }

  validateSession(roomId: string): boolean {
    return this.rooms.get(roomId)?.status === 'playing';
  }

  getRoom(roomId: string): Room {
    return this.findRoom(roomId);
  }

  private findRoom(roomId: string): Room {
    const room = this.rooms.get(roomId);
    if (!room) throw new NotFoundException('Room not found');
    return room;
  }

  private findPlayer(room: Room, uid: string): Player {
    const player = room.players.find((p) => p.uid === uid);
    if (!player) throw new NotFoundException('Player not in room');
    return player;
  }
}
