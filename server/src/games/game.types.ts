export interface MapData {
  cellSize: number;
  width: number;
  height: number;
  originX: number;
  originZ: number;
  grid: number[][];
}

/** 텐션 이벤트 유형 */
export enum TensionEventType {
  SMOKE = 'SMOKE',
  WHOAREYOU = 'WHOAREYOU',
  SIREN = 'SIREN',
}

/** 현재 활성화된 텐션 이벤트 상태 */
export interface ActiveTensionEvent {
  type: TensionEventType;
  startedAt: number;
  durationMs: number;
}

/** 구역별 텐션 상태 */
export interface ZoneTensionState {
  zoneId: string;
  tension: number;
  activeEvent: ActiveTensionEvent | null;
  lastEventEndedAt: number;
}

/** 사이렌 이벤트 상태 */
export interface SirenState {
  targetX: number;
  targetZ: number;
  startedAt: number;
  durationMs: number;
}

/** WHOAREYOU 이벤트 상태 */
export interface WhoAreYouState {
  startedAt: number;
  durationMs: number;
}

/** 구역별 텐션 결과 */
export interface ZoneTensionResult {
  zoneId: string;
  tension: number;
  triggeredEvent: TensionEventType | null;
}

/** 텐션 평가 결과 */
export interface TensionEvalResult {
  zones: ZoneTensionResult[];
}

/** Observer 패턴: 텐션 이벤트 구독 인터페이스 */
export interface TensionEventListener {
  onTensionEvaluated(sessionId: string, result: TensionEvalResult): void;
  onTensionEventExpired(sessionId: string, zoneId: string): void;
}

export interface CharacterCoordinate {
  uid: string;
  state: number;
  x: number;
  y: number;
  z: number;
  r: number;
  alive: boolean;
}

export interface PlayerState extends CharacterCoordinate {
  playerKillCount: number;
  npcKillCount: number;
}

export interface NpcBehaviorContext {
  sirenActive: boolean;
  whoAreYouActive: boolean;
  players: Map<string, PlayerState>;
}

export interface NpcState extends CharacterCoordinate {
  destination: { x: number; y: number; z: number };
  path: { x: number; y: number; z: number }[];
  speed: number;
}

export interface GameSession {
  sessionId: string;
  playerIds: string[];
  createdAt: Date;
  players: Map<string, PlayerState>;
  npcs: Map<string, NpcState>;
  map: MapData;
  walkablePositions: { x: number; y: number; z: number }[];
  zoneTensions: Map<string, ZoneTensionState>;
  activeSiren: SirenState | null;
  activeWhoAreYou: WhoAreYouState | null;
  tensionTickCounter: number;
}

export type Vec3 = { x: number; y: number; z: number };

export interface CharacterFactory<T extends CharacterCoordinate> {
  create(uid: string, walkablePositions: Vec3[]): T;
}