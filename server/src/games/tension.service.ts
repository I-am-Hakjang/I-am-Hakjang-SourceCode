import { Injectable, Logger } from '@nestjs/common';
import {
  CharacterCoordinate,
  GameSession,
  MapData,
  PlayerState,
  TensionEvalResult,
  TensionEventType,
  ZoneTensionResult,
  ZoneTensionState,
} from './game.types';

@Injectable()
export class TensionService {
  private readonly logger = new Logger(TensionService.name);

  /* ── 상수 ── */
  readonly GRID_SIZE = 4;
  readonly TENSION_EVAL_INTERVAL_MS = 5_000;
  readonly EVENT_DURATION_MS = 10_000;

  private readonly TENSION_THRESHOLD = 70;
  private readonly EVENT_TRIGGER_PROBABILITY = 1;
  private readonly EVENT_COOLDOWN_MS = 30_000;

  private readonly CLOSE_DISTANCE = 10;
  private readonly MEDIUM_DISTANCE = 25;
  private readonly GAZE_ANGLE_THRESHOLD_DEG = 30;

  private readonly EVENT_TYPES = Object.values(TensionEventType);

  /* ────────────────────────────────────────────
   * 구역 ID 계산: 좌표와 MapData 정보 이용
   * ──────────────────────────────────────────── */
  getZoneId(x: number, z: number, map: MapData): string {
    const worldWidth = map.width * map.cellSize;
    const worldHeight = map.height * map.cellSize;

    const zoneWidth = worldWidth / this.GRID_SIZE;
    const zoneHeight = worldHeight / this.GRID_SIZE;

    const relativeX = x - map.originX;
    const relativeZ = z - map.originZ;

    const zx = Math.min(
      this.GRID_SIZE - 1,
      Math.max(0, Math.floor(relativeX / zoneWidth)),
    );
    const zz = Math.min(
      this.GRID_SIZE - 1,
      Math.max(0, Math.floor(relativeZ / zoneHeight)),
    );

    return `${zx},${zz}`;
  }

  /* ────────────────────────────────────────────
   * 구역 중심 월드 좌표 계산
   * ──────────────────────────────────────────── */
  getZoneCenter(zoneId: string, map: MapData): { x: number; z: number } {
    const [zx, zz] = zoneId.split(',').map(Number);

    const zoneWidth = (map.width * map.cellSize) / this.GRID_SIZE;
    const zoneHeight = (map.height * map.cellSize) / this.GRID_SIZE;

    return {
      x: map.originX + (zx + 0.5) * zoneWidth,
      z: map.originZ + (zz + 0.5) * zoneHeight,
    };
  }

  /* ────────────────────────────────────────────
   * 구역별 상태 초기 맵 생성
   * ──────────────────────────────────────────── */
  createZoneStates(): Map<string, ZoneTensionState> {
    const zoneStates = new Map<string, ZoneTensionState>();
    for (let x = 0; x < this.GRID_SIZE; x++) {
      for (let z = 0; z < this.GRID_SIZE; z++) {
        const zoneId = `${x},${z}`;
        zoneStates.set(zoneId, {
          zoneId,
          tension: 0,
          activeEvent: null,
          lastEventEndedAt: 0,
        });
      }
    }
    return zoneStates;
  }

  /* ────────────────────────────────────────────
   * 메인: 구역별 텐션 평가 + 이벤트 판정
   * ──────────────────────────────────────────── */
  evaluateTension(session: GameSession): TensionEvalResult {
    const zonesResult: ZoneTensionResult[] = [];

    // 생존 플레이어들을 구역별로 그룹화
    const alivePlayers = Array.from(session.players.values()).filter(
      (p) => p.alive,
    );
    const playersByZone = new Map<string, PlayerState[]>();

    for (const p of alivePlayers) {
      const zoneId = this.getZoneId(p.x, p.z, session.map);
      if (!playersByZone.has(zoneId)) {
        playersByZone.set(zoneId, []);
      }
      playersByZone.get(zoneId)!.push(p);
    }

    // 16개 구역에 대해 독립적으로 평가
    for (const [zoneId, zoneState] of session.zoneTensions.entries()) {
      const zonePlayers = playersByZone.get(zoneId) ?? [];
      const tension = this.calculateTensionForPlayers(zonePlayers);
      zoneState.tension = tension;

      const triggeredEvent = this.checkEventTrigger(zoneState, tension);

      if (triggeredEvent) {
        zoneState.activeEvent = {
          type: triggeredEvent,
          startedAt: Date.now(),
          durationMs: this.EVENT_DURATION_MS,
        };
        this.logger.log(
          `[${session.sessionId}][Zone ${zoneId}] 텐션 이벤트 발생: ${triggeredEvent} (tension=${tension.toFixed(1)})`,
        );
      }

      zonesResult.push({
        zoneId,
        tension,
        triggeredEvent,
      });
    }

    return { zones: zonesResult };
  }

  /* ────────────────────────────────────────────
   * 구역별 이벤트 만료 체크 (매 tick 호출)
   * ──────────────────────────────────────────── */
  checkEventExpiry(session: GameSession): string[] {
    const expiredZoneIds: string[] = [];

    for (const [zoneId, zoneState] of session.zoneTensions.entries()) {
      if (!zoneState.activeEvent) continue;

      const elapsed = Date.now() - zoneState.activeEvent.startedAt;
      if (elapsed >= zoneState.activeEvent.durationMs) {
        this.logger.log(
          `[${session.sessionId}][Zone ${zoneId}] 텐션 이벤트 종료: ${zoneState.activeEvent.type}`,
        );
        zoneState.activeEvent = null;
        zoneState.lastEventEndedAt = Date.now();
        expiredZoneIds.push(zoneId);
      }
    }

    return expiredZoneIds;
  }

  /* ────────────────────────────────────────────
   * 구역 내 플레이어 간 텐션 수치 계산
   * ──────────────────────────────────────────── */
  private calculateTensionForPlayers(players: PlayerState[]): number {
    // 생존 플레이어가 2명 미만이면 텐션 0
    if (players.length < 2) return 0;

    let totalScore = 0;
    let pairCount = 0;

    for (let i = 0; i < players.length; i++) {
      for (let j = i + 1; j < players.length; j++) {
        const a = players[i];
        const b = players[j];

        // 유클리드 거리 (xz 평면)
        const dx = a.x - b.x;
        const dz = a.z - b.z;
        const distance = Math.sqrt(dx * dx + dz * dz);

        // 거리 기반 점수
        let pairScore: number;
        if (distance < this.CLOSE_DISTANCE) {
          pairScore = 40;
        } else if (distance < this.MEDIUM_DISTANCE) {
          pairScore = 20;
        } else {
          pairScore = 5;
        }

        // 시선 분석 — r 값(y축 rotation, degrees) 기반
        const aLooksAtB = this.isLookingAt(a, b);
        const bLooksAtA = this.isLookingAt(b, a);

        if (aLooksAtB && bLooksAtA) {
          pairScore += 25; // 서로 바라보고 있음
        } else if (aLooksAtB || bLooksAtA) {
          pairScore += 15; // 한 쪽이 상대를 바라봄
        }

        totalScore += pairScore;
        pairCount++;
      }
    }

    // 정규화: 쌍당 최대 점수(40+25=65) 기준으로 0~100 스케일
    const maxScorePerPair = 65;
    const normalized =
      (totalScore / (Math.max(1, pairCount) * maxScorePerPair)) * 100;

    return Math.min(100, Math.max(0, normalized));
  }

  /* ────────────────────────────────────────────
   * 시선 판정: A가 B를 바라보고 있는가?
   * ──────────────────────────────────────────── */
  private isLookingAt(
    observer: CharacterCoordinate,
    target: CharacterCoordinate,
  ): boolean {
    const dx = target.x - observer.x;
    const dz = target.z - observer.z;
    const distSq = dx * dx + dz * dz;

    if (distSq < 0.01) return false;

    const radians = (observer.r * Math.PI) / 180;
    const forwardX = Math.sin(radians);
    const forwardZ = Math.cos(radians);

    const dist = Math.sqrt(distSq);
    const dirX = dx / dist;
    const dirZ = dz / dist;

    const dot = forwardX * dirX + forwardZ * dirZ;
    const clampedDot = Math.min(1, Math.max(-1, dot));
    const angleDeg = (Math.acos(clampedDot) * 180) / Math.PI;

    return angleDeg <= this.GAZE_ANGLE_THRESHOLD_DEG;
  }

  /* ────────────────────────────────────────────
   * 구역별 이벤트 발동 판정
   * ──────────────────────────────────────────── */
  private checkEventTrigger(
    zone: ZoneTensionState,
    tension: number,
  ): TensionEventType | null {
    if (zone.activeEvent) return null;

    const now = Date.now();
    if (now - zone.lastEventEndedAt < this.EVENT_COOLDOWN_MS) return null;

    if (tension < this.TENSION_THRESHOLD) return null;

    if (Math.random() > this.EVENT_TRIGGER_PROBABILITY) return null;

    return this.selectEventType();
  }

  /* ────────────────────────────────────────────
   * 이벤트 유형 랜덤 선택
   * ──────────────────────────────────────────── */
  private selectEventType(): TensionEventType {
    const index = Math.floor(Math.random() * this.EVENT_TYPES.length);
    return this.EVENT_TYPES[index % this.EVENT_TYPES.length];
  }
}
