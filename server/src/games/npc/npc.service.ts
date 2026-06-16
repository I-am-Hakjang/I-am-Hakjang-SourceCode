import { Injectable } from '@nestjs/common';
import {
  MapData,
  NpcBehaviorContext,
  NpcState,
} from '../game.types';
import { PathService } from './path.service';
import { NpcFactory } from './npc.factory';
import { NormalBehavior, NpcBehavior, SirenBehavior, WhoAreYouBehavior } from './npc.behaviors'

type Vec3 = { x: number; y: number; z: number };

@Injectable()
export class NpcService {
  private readonly strategies = new Map<string, NpcBehavior>();
  private readonly sirenBehavior: SirenBehavior;
  private readonly whoAreYouBehavior: WhoAreYouBehavior;

  constructor(
    private readonly pathService: PathService,
    private readonly npcFactory: NpcFactory,
    normalBehavior: NormalBehavior,
    sirenBehavior: SirenBehavior,
    whoAreYouBehavior: WhoAreYouBehavior,
  ) {
    this.sirenBehavior = sirenBehavior;
    this.whoAreYouBehavior = whoAreYouBehavior;
    this.strategies.set('NORMAL', normalBehavior);
    this.strategies.set('SIREN', sirenBehavior);
    this.strategies.set('WHOAREYOU', whoAreYouBehavior);
  }

  private readonly NPC_COUNT = 50;
  public readonly NPC_WALK_SPEED = 3;
  public readonly NPC_RUN_SPEED = 6;
  public readonly ROTATION_SPEED = 270;
  private readonly WAYPOINT_THRESHOLD = 0.1;

  // ── 공개 API ─────────────────────────────────────────────────────────────

  computeWalkablePositions(map: MapData): Vec3[] {
    const positions: Vec3[] = [];
    for (let row = 0; row < map.height; row++) {
      for (let col = 0; col < map.width; col++) {
        if (map.grid[row][col] === 0) {
          positions.push({
            x: map.originX + (col + 0.5) * map.cellSize,
            y: 0,
            z: map.originZ + (row + 0.5) * map.cellSize,
          });
        }
      }
    }
    return positions;
  }

  createNpcs(map: MapData, walkablePositions: Vec3[]): Map<string, NpcState> {
    const npcs = new Map<string, NpcState>();
    for (let i = 0; i < this.NPC_COUNT; i++) {
      const npc = this.npcFactory.create(`npc_${i}`, walkablePositions);
      this.assignNewPath(npc, map, walkablePositions);
      npcs.set(npc.uid, npc);
    }
    return npcs;
  }

  redirectAllToTarget(
    npcs: Map<string, NpcState>,
    target: { x: number; z: number },
    map: MapData,
  ): void {
    for (const npc of npcs.values()) {
      if (!npc.alive) continue;
      npc.destination = { x: target.x, y: 0, z: target.z };
      this.assignPath(npc, map);
    }
  }

  updatePositions(
    sessionId: string,
    npcs: Map<string, NpcState>,
    deltaMs: number,
    map: MapData,
    walkablePositions: Vec3[],
    context: NpcBehaviorContext,
  ): void {
    const delta = deltaMs / 1000;
    for (const npc of npcs.values()) {
      if (!npc.alive) continue;

      let strategyKey = 'NORMAL';
      if (context.whoAreYouActive) {
        strategyKey = 'WHOAREYOU';
      } else if (context.sirenActive) {
        strategyKey = 'SIREN';
      }

      const strategy = this.strategies.get(strategyKey);
      if (strategy) {
        strategy.execute(
          npc,
          delta,
          context,
          map,
          walkablePositions,
          this,
          sessionId,
        );
      }
    }
  }

  // ── 경로 상태 관리 ────────────────────────────────────────────────────────

  private assignDestination(npc: NpcState, walkablePositions: Vec3[]): void {
    npc.destination =
      walkablePositions[Math.floor(Math.random() * walkablePositions.length)];
  }

  private assignPath(npc: NpcState, map: MapData): void {
    const current: Vec3 = { x: npc.x, y: npc.y, z: npc.z };
    npc.path = this.pathService.smoothPath(
      map,
      current,
      this.pathService.computePath(map, current, npc.destination),
    );
  }

  public assignNewPath(
    npc: NpcState,
    map: MapData,
    walkablePositions: Vec3[],
  ): void {
    this.assignDestination(npc, walkablePositions);
    this.assignPath(npc, map);
  }

  /** 현재 웨이포인트에 도달했으면 path에서 제거하고 true 반환, 아직 멀면 false 반환 */
  public advancePath(npc: NpcState): boolean {
    if (npc.path.length === 0) return false;
    const wp = npc.path[0];
    const dx = wp.x - npc.x;
    const dz = wp.z - npc.z;
    if (Math.sqrt(dx * dx + dz * dz) < this.WAYPOINT_THRESHOLD) {
      npc.path.shift();
      return true;
    }
    return false;
  }
}
