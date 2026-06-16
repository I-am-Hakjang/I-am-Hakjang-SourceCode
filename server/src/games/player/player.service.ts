import { Injectable } from '@nestjs/common';
import { CharacterCoordinate, GameSession, PlayerState, Vec3 } from '../game.types';
import { PlayerFactory } from './player.factory';

@Injectable()
export class PlayerService {
  constructor(private readonly playerFactory: PlayerFactory) {}

  createPlayers(
    playerIds: string[],
    walkablePositions: Vec3[],
  ): Map<string, PlayerState> {
    const players = new Map<string, PlayerState>();
    for (const uid of playerIds) {
      players.set(uid, this.playerFactory.create(uid, walkablePositions));
    }
    return players;
  }
  updatePositions(
    session: GameSession,
    coordinate: CharacterCoordinate,
  ): void {
    const existing = session.players.get(coordinate.uid);
    session.players.set(coordinate.uid, {
      playerKillCount: existing?.playerKillCount ?? 0,
      npcKillCount: existing?.npcKillCount ?? 0,
      ...coordinate,
      alive: existing?.alive ?? true,
    });
  }

  killCharacter(
    session: GameSession,
    killerUid: string,
    targetUid: string,
  ): { playerKillCount: number; npcKillCount: number } | null {
    const killer = session.players.get(killerUid);
    if (!killer) return null;

    const targetType = this.getCharacterType(session, targetUid);

    if (targetType === 'player') {
      const target = session.players.get(targetUid)!;
      if (!target.alive) return null;
      target.alive = false;
      killer.playerKillCount += 1;
      return {
        playerKillCount: killer.playerKillCount,
        npcKillCount: killer.npcKillCount,
      };
    }

    if (targetType === 'npc') {
      const target = session.npcs.get(targetUid)!;
      if (!target.alive) return null;
      target.alive = false;
      killer.npcKillCount += 1;
      return {
        playerKillCount: killer.playerKillCount,
        npcKillCount: killer.npcKillCount,
      };
    }

    return null;
  }

  private getCharacterType(
    session: GameSession,
    uid: string,
  ): 'player' | 'npc' | null {
    if (session.players.has(uid)) return 'player';
    if (session.npcs.has(uid)) return 'npc';
    return null;
  }
}
