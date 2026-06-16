import { Injectable } from '@nestjs/common';
import { CharacterFactory, NpcState, Vec3 } from '../game.types';

@Injectable()
export class NpcFactory implements CharacterFactory<NpcState> {
  private readonly INITIAL_SPEED = 3;

  create(uid: string, walkablePositions: Vec3[]): NpcState {
    const spawn =
      walkablePositions[Math.floor(Math.random() * walkablePositions.length)];

    return {
      uid,
      state: 1,
      x: spawn.x,
      y: 0,
      z: spawn.z,
      r: 0,
      alive: true,
      destination: spawn,
      path: [],
      speed: this.INITIAL_SPEED,
    };
  }
}
