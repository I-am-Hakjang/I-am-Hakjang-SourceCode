import {
  ConflictException,
  Injectable,
  Logger,
  NotFoundException,
} from '@nestjs/common';
import * as fs from 'fs';
import * as path from 'path';
import {
  CharacterCoordinate,
  GameSession,
  MapData,
  TensionEventListener,
} from './game.types';
import { NpcService } from './npc/npc.service';
import { PlayerService } from './player/player.service';
import { TensionService } from './tension.service';

@Injectable()
export class GameService {
  constructor(
    private readonly npcService: NpcService,
    private readonly playerService: PlayerService,
    private readonly tensionService: TensionService,
  ) {}

  private readonly logger = new Logger(GameService.name);
  private readonly sessions = new Map<string, GameSession>();
  private readonly tensionListeners: TensionEventListener[] = [];

  addTensionEventListener(listener: TensionEventListener): void {
    this.tensionListeners.push(listener);
  }

  removeTensionEventListener(listener: TensionEventListener): void {
    const idx = this.tensionListeners.indexOf(listener);
    if (idx !== -1) this.tensionListeners.splice(idx, 1);
  }

  createSession(sessionId: string, playerIds: string[], mapName: string): void {
    if (this.sessions.has(sessionId))
      throw new ConflictException(`Session already exists: ${sessionId}`);

    const mapPath = path.join(process.cwd(), 'maps', `${mapName}.json`);
    const map: MapData = JSON.parse(fs.readFileSync(mapPath, 'utf-8'));
    const walkablePositions = this.npcService.computeWalkablePositions(map);

    const npcs = this.npcService.createNpcs(map, walkablePositions);

    const players = this.playerService.createPlayers(playerIds, walkablePositions);

    this.sessions.set(sessionId, {
      sessionId,
      playerIds,
      createdAt: new Date(),
      players,
      npcs,
      map,
      walkablePositions,
      zoneTensions: this.tensionService.createZoneStates(),
      activeSiren: null,
      activeWhoAreYou: null,
      tensionTickCounter: 0,
    });

    this.logger.log(
      `Session created — sessionId: ${sessionId}, players: ${playerIds.join(', ')}`,
    );
  }

  getAllSessions(): GameSession[] {
    return Array.from(this.sessions.values());
  }

  validateSession(sessionId: string): boolean {
    return this.sessions.has(sessionId);
  }

  validatePlayer(sessionId: string, playerId: string): boolean {
    return this.sessions.get(sessionId)?.playerIds.includes(playerId) ?? false;
  }

  updatePlayerCoordinate(
    sessionId: string,
    coordinate: CharacterCoordinate,
  ): void {
    const session = this.sessions.get(sessionId);
    if (session) this.playerService.updatePositions(session, coordinate);
  }

  killCharacter(
    sessionId: string,
    killerUid: string,
    targetUid: string,
  ): { playerKillCount: number; npcKillCount: number } | null {
    const session = this.sessions.get(sessionId);
    if (!session) return null;
    return this.playerService.killCharacter(session, killerUid, targetUid);
  }

  updateNpcPositions(deltaMs: number): void {
    for (const session of this.sessions.values()) {
      this.npcService.updatePositions(
        session.sessionId,
        session.npcs,
        deltaMs,
        session.map,
        session.walkablePositions,
        {
          sirenActive: session.activeSiren !== null,
          whoAreYouActive: session.activeWhoAreYou !== null,
          players: session.players,
        },
      );
    }
  }

  getSession(sessionId: string): GameSession {
    const session = this.sessions.get(sessionId);
    if (!session)
      throw new NotFoundException(`Session not found: ${sessionId}`);
    return session;
  }

  deleteSession(sessionId: string): void {
    this.sessions.delete(sessionId);
    this.logger.log(`Session deleted — sessionId: ${sessionId}`);
  }

  activateSiren(sessionId: string): void {
    const session = this.getSession(sessionId);
    const target =
      session.walkablePositions[
        Math.floor(Math.random() * session.walkablePositions.length)
      ];
    session.activeSiren = {
      targetX: target.x,
      targetZ: target.z,
      startedAt: Date.now(),
      durationMs: 5_000,
    };
    this.npcService.redirectAllToTarget(
      session.npcs,
      { x: target.x, z: target.z },
      session.map,
    );
    this.logger.log(
      `[${sessionId}] SIREN activated — target: (${target.x.toFixed(1)}, ${target.z.toFixed(1)})`,
    );
  }

  checkSirenExpiry(sessionId: string): boolean {
    const session = this.getSession(sessionId);
    if (!session.activeSiren) return false;

    const elapsed = Date.now() - session.activeSiren.startedAt;
    if (elapsed < session.activeSiren.durationMs) return false;

    session.activeSiren = null;
    this.logger.log(
      `[${sessionId}] SIREN expired — NPCs resume normal behavior`,
    );
    return true;
  }

  activateWhoAreYou(sessionId: string): void {
    const session = this.getSession(sessionId);
    session.activeWhoAreYou = {
      startedAt: Date.now(),
      durationMs: 5_000,
    };
    this.logger.log(`[${sessionId}] WHOAREYOU activated — all NPCs frozen`);
  }

  checkWhoAreYouExpiry(sessionId: string): boolean {
    const session = this.getSession(sessionId);
    if (!session.activeWhoAreYou) return false;

    const elapsed = Date.now() - session.activeWhoAreYou.startedAt;
    if (elapsed < session.activeWhoAreYou.durationMs) return false;

    session.activeWhoAreYou = null;
    this.logger.log(
      `[${sessionId}] WHOAREYOU expired — NPCs resume normal behavior`,
    );
    return true;
  }

  getZoneCenter(sessionId: string, zoneId: string): { x: number; z: number } {
    const session = this.getSession(sessionId);
    return this.tensionService.getZoneCenter(zoneId, session.map);
  }

  tickTension(sessionId: string, deltaMs: number): void {
    const session = this.getSession(sessionId);

    const expiredZones = this.tensionService.checkEventExpiry(session);
    for (const zoneId of expiredZones) {
      this.tensionListeners.forEach((l) => l.onTensionEventExpired(sessionId, zoneId));
    }

    session.tensionTickCounter += deltaMs;
    if (session.tensionTickCounter < this.tensionService.TENSION_EVAL_INTERVAL_MS) return;

    session.tensionTickCounter = 0;
    const result = this.tensionService.evaluateTension(session);
    this.tensionListeners.forEach((l) => l.onTensionEvaluated(sessionId, result));
  }

  checkGameOver(sessionId: string):
    | {
        playerUid: string;
        rank: number;
        playerKillCount: number;
        npcKillCount: number;
      }[]
    | null {
    const session = this.sessions.get(sessionId);
    if (!session) return null;

    const alivePlayers = Array.from(session.players.values()).filter(
      (p) => p.alive,
    );
    if (
      alivePlayers.length === 1 &&
      alivePlayers.length < session.playerIds.length
    ) {
      const leaderBoard = Array.from(session.players.values())
        .sort((a, b) => a.npcKillCount - b.npcKillCount)
        .sort((a, b) => b.playerKillCount - a.playerKillCount)
        .map((p, i) => ({
          playerUid: p.uid,
          rank: i + 1,
          playerKillCount: p.playerKillCount,
          npcKillCount: p.npcKillCount,
        }));
      return leaderBoard;
    }

    return null;
  }
}
