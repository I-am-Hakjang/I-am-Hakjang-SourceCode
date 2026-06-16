import { Injectable } from '@nestjs/common';
import { MapData, NpcBehaviorContext, NpcState, PlayerState } from '../game.types';
import { NpcService } from './npc.service';

type Vec3 = { x: number; y: number; z: number };
type IdleState = { timeRemaining: number; targetRotation: number };

export interface NpcBehavior {
  execute(
    npc: NpcState,
    delta: number,
    context: NpcBehaviorContext,
    map: MapData,
    walkablePositions: { x: number; y: number; z: number }[],
    npcService: NpcService,
    sessionId: string,
  ): void;
}

@Injectable()
export class NormalBehavior implements NpcBehavior {
  private readonly idleMap = new Map<string, IdleState>();
  private readonly IDLE_ROTATION_RATIO = 0.3;
  private readonly IDLE_MIN_SEC = 1;
  private readonly IDLE_MAX_SEC = 4;

  execute(
    npc: NpcState,
    delta: number,
    context: NpcBehaviorContext,
    map: MapData,
    walkablePositions: Vec3[],
    npcService: NpcService,
    sessionId: string,
  ): void {
    npc.speed = npcService.NPC_WALK_SPEED;

    if (npc.path.length === 0) {
      this.idleBehavior(npc, `${sessionId}:${npc.uid}`, delta, map, walkablePositions, npcService);
      return;
    }

    if (npcService.advancePath(npc)) return;

    const waypoint = npc.path[0];
    const dx = waypoint.x - npc.x;
    const dz = waypoint.z - npc.z;
    const distance = Math.sqrt(dx * dx + dz * dz);

    npc.state = 1;
    npc.r = lerpAngle(
      npc.r,
      ((Math.atan2(dx, dz) * 180) / Math.PI + 360) % 360,
      npcService.ROTATION_SPEED * delta,
    );
    npc.x += (dx / distance) * npc.speed * delta;
    npc.z += (dz / distance) * npc.speed * delta;
  }

  private idleBehavior(
    npc: NpcState,
    idleKey: string,
    delta: number,
    map: MapData,
    walkablePositions: Vec3[],
    npcService: NpcService,
  ): void {
    if (!this.idleMap.has(idleKey)) {
      this.idleMap.set(idleKey, this.createIdleState(npc.r));
    }

    const idle = this.idleMap.get(idleKey)!;
    idle.timeRemaining -= delta;

    if (idle.timeRemaining > 0) {
      npc.state = 0;
      npc.r = lerpAngle(
        npc.r,
        idle.targetRotation,
        npcService.ROTATION_SPEED * this.IDLE_ROTATION_RATIO * delta,
      );
    } else {
      this.idleMap.delete(idleKey);
      npcService.assignNewPath(npc, map, walkablePositions);
      npc.state = 1;
    }
  }

  private createIdleState(currentR: number): IdleState {
    const offset = (Math.random() - 0.5) * 180;
    return {
      timeRemaining:
        this.IDLE_MIN_SEC +
        Math.random() * (this.IDLE_MAX_SEC - this.IDLE_MIN_SEC),
      targetRotation: (currentR + offset + 360) % 360,
    };
  }
}


@Injectable()
export class SirenBehavior implements NpcBehavior {
  execute(
    npc: NpcState,
    delta: number,
    context: NpcBehaviorContext,
    map: MapData,
    walkablePositions: { x: number; y: number; z: number }[],
    npcService: NpcService,
    sessionId: string,
  ): void {
    void sessionId;
    npc.speed = npcService.NPC_RUN_SPEED;
    npc.state = 2; // Run

    if (npc.path.length === 0) {
      // 목적지 도착 후 제자리 회전
      npc.r = (npc.r + 720 * delta) % 360;
      return;
    }

    if (npcService.advancePath(npc)) return;

    const waypoint = npc.path[0];
    const dx = waypoint.x - npc.x;
    const dz = waypoint.z - npc.z;
    const distance = Math.sqrt(dx * dx + dz * dz);

    npc.r = lerpAngle(
      npc.r,
      ((Math.atan2(dx, dz) * 180) / Math.PI + 360) % 360,
      npcService.ROTATION_SPEED * delta,
    );
    npc.x += (dx / distance) * npc.speed * delta;
    npc.z += (dz / distance) * npc.speed * delta;
  }
}

@Injectable()
export class WhoAreYouBehavior implements NpcBehavior {
  execute(
    npc: NpcState,
    delta: number,
    context: NpcBehaviorContext,
    map: MapData,
    walkablePositions: { x: number; y: number; z: number }[],
    npcService: NpcService,
    sessionId: string,
  ): void {
    void sessionId;
    npc.state = 0; // Frozen / Idle
    npc.path = []; // 움직이지 않음

    const nearest = this.findNearestAlivePlayer(npc, context.players);
    if (nearest) {
      const dx = nearest.x - npc.x;
      const dz = nearest.z - npc.z;
      npc.r = ((Math.atan2(dx, dz) * 180) / Math.PI + 360) % 360;
    }
  }

  findNearestAlivePlayer(
    npc: NpcState,
    players: Map<string, PlayerState>,
  ): PlayerState | null {
    let nearest: PlayerState | null = null;
    let minDist = Infinity;
    for (const p of players.values()) {
      if (!p.alive) continue;
      const dx = p.x - npc.x;
      const dz = p.z - npc.z;
      const dist = dx * dx + dz * dz;
      if (dist < minDist) {
        minDist = dist;
        nearest = p;
      }
    }
    return nearest;
  }
}

  /**
   * 각도를 한 번에 꺾지 않고 매 틱마다 maxDelta만큼씩 점진적으로 회전시킨다.
   *
   * - current  : 현재 각도 (0~360°)
   * - target   : 목표 각도 (0~360°)
   * - maxDelta : 이번 틱에 회전할 수 있는 최대 각도 (ROTATION_SPEED × delta)
   *
   * 360° 경계(예: 350° → 10°)를 넘어갈 때도 짧은 쪽으로 자연스럽게 돌아간다.
   * diff를 -180~180 범위로 정규화한 뒤, 그 방향으로 maxDelta 이하만큼 이동한다.
   */
function lerpAngle(current: number, target: number, maxDelta: number): number {
  const diff = ((target - current + 540) % 360) - 180;
  return (
    (current + Math.sign(diff) * Math.min(Math.abs(diff), maxDelta) + 360) %
    360
  );
}