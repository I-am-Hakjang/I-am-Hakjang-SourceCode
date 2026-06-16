import { Injectable } from '@nestjs/common';
import { CharacterFactory, PlayerState } from '../game.types';


type Vec3 = { x: number; y: number; z: number };

@Injectable()
export class PlayerFactory implements CharacterFactory<PlayerState> {
  create(uid: string, walkablePositions: Vec3[]): PlayerState {
    const spawn =
      walkablePositions[Math.floor(Math.random() * walkablePositions.length)]
    return {
      uid,
      state: 0,
      x: spawn.x,
      y: 0,
      z: spawn.z,
      r: 0,
      alive: true,
      playerKillCount: 0,
      npcKillCount: 0,
    };
  }
}
