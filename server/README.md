# I am Hakjang — Game Server

NestJS 기반 멀티플레이어 실시간 게임 서버. WebSocket으로 룸 매칭부터 인게임 상태 동기화, 텐션 이벤트까지 처리한다.

---

## 기술 스택

- **Runtime**: Node.js
- **Framework**: NestJS 11
- **WebSocket**: `ws` (NestJS platform-ws)
- **Language**: TypeScript 5

---

## 프로젝트 구조

```
src/
├── main.ts
├── app.module.ts
├── rooms/                  # 룸 매칭 도메인
│   ├── room.gateway.ts     # WebSocket /rooms 엔드포인트
│   ├── room.service.ts
│   └── room.types.ts
└── games/                  # 인게임 도메인
    ├── game.gateway.ts     # WebSocket /games 엔드포인트
    ├── game.service.ts     # Facade + Observer Subject
    ├── game.types.ts
    ├── tension.service.ts  # 텐션 계산 및 이벤트 판정
    ├── npc/
    │   ├── npc.service.ts
    │   ├── npc.factory.ts
    │   ├── npc.behaviors.ts  # Strategy Pattern (NPC 행동)
    │   └── path.service.ts
    └── player/
        ├── player.service.ts
        └── player.factory.ts
```

---

## WebSocket API

### `/rooms` — 룸 매칭

| 이벤트 (송신) | 페이로드 | 설명 |
|---|---|---|
| `createRoom` | `{ playerId, maxPlayer? }` | 룸 생성 |
| `joinRoom` | `{ roomId, playerId }` | 룸 참가 |
| `leaveRoom` | `{ roomId, uid }` | 룸 퇴장 |
| `setReady` | `{ roomId, uid }` | 준비 상태 토글 |
| `quickMatch` | `{ playerId, maxPlayers? }` | 빠른 매칭 |
| `startGame` | `{ roomId, uid, mapName? }` | 게임 시작 (기본 맵: `JJS`) |
| `getAvailableRooms` | `{ maxPlayers? }` | 참가 가능한 룸 목록 조회 |
| `getRoom` | `{ roomId }` | 특정 룸 정보 조회 |

| 이벤트 (수신) | 설명 |
|---|---|
| `createRoom` | 생성된 룸 정보 + 본인 uid |
| `joinRoom` | 참가한 룸 정보 + 본인 uid |
| `roomUpdate` | 룸 상태 변경 브로드캐스트 |
| `availableRooms` | 전체 참가 가능 룸 목록 |
| `gameStart` | 게임 시작 — `{ sessionId, playerPoses, npcPoses }` |

---

### `/games?sessionId=&playerUid=` — 인게임

연결 시 쿼리스트링으로 `sessionId`와 `playerUid`를 전달해야 한다.

| 이벤트 (송신) | 페이로드 | 설명 |
|---|---|---|
| `unitSync` | `{ state, x, y, z, r }` | 플레이어 좌표 업데이트 |
| `attack` | `{ target: uid }` | 공격 대상 지정 |

| 이벤트 (수신) | 설명 |
|---|---|
| `connected` | 연결 성공 |
| `unitSync` | 매 33ms — 플레이어·NPC 전체 좌표 동기화 |
| `tensionUpdate` | 매 5초 — 구역별 텐션 수치 `{ zones: [{ zoneId, tension }] }` |
| `event` | 텐션 이벤트 발생 `{ type, x, y, z, timestamp }` |
| `tensionEventEnd` | 텐션 이벤트 만료 `{ zoneId }` |
| `kill` | 캐릭터 사망 브로드캐스트 `{ killer, target }` |
| `killStat` | 공격자에게 킬 통계 반환 |
| `gameOver` | 게임 종료 — 리더보드 배열 |

---

## 텐션 시스템

맵을 4×4 = 16개 구역으로 분할하여 구역별 독립적으로 텐션을 계산한다.

### 계산 로직

플레이어 쌍(pair)마다 아래 점수를 합산하고 0~100으로 정규화한다.

| 조건 | 점수 |
|---|---|
| 거리 < 10 | +40 |
| 거리 < 25 | +20 |
| 거리 ≥ 25 | +5 |
| 서로 바라봄 (시선각 ≤ 30°) | +25 |
| 한쪽만 바라봄 | +15 |

### 이벤트 발동 조건

- 텐션 ≥ 70
- 활성 이벤트 없음
- 마지막 이벤트 종료 후 30초 경과

### 이벤트 종류

| 타입 | 지속 시간 | 효과 |
|---|---|---|
| `SMOKE` | 10초 | 연막 효과 (클라이언트 처리) |
| `SIREN` | 10초 | NPC 전체가 특정 지점으로 집결 (5초) |
| `WHOAREYOU` | 10초 | NPC 전체 동결 (5초) |

### 타이밍

| 주기 | 동작 |
|---|---|
| 33ms | 게임 루프 (좌표 동기화, 이벤트 만료 체크) |
| 5,000ms | 텐션 평가 |
| 10,000ms | 텐션 이벤트 자동 만료 |
| 30,000ms | 이벤트 쿨다운 |

---

## 디자인 패턴

| 패턴 | 적용 위치 | 설명 |
|---|---|---|
| **Observer** | `GameService` / `GameGateway` | 텐션 이벤트 발생·만료를 `TensionEventListener`로 통보 |
| **Facade** | `GameService` | `NpcService`, `PlayerService`, `TensionService`를 단일 인터페이스로 래핑 |
| **Strategy** | `npc.behaviors.ts` | NPC 행동(이동·동결·집결)을 교체 가능한 전략으로 분리 |
| **Factory** | `NpcFactory`, `PlayerFactory` | 캐릭터 초기 상태 생성 로직 캡슐화 |

---

## 실행

```bash
# 의존성 설치
yarn install

# 개발 (watch mode)
yarn start:dev

# 프로덕션 빌드 및 실행
yarn build
yarn start:prod
```

## 테스트

```bash
yarn test        # 단위 테스트
yarn test:e2e    # E2E 테스트
yarn test:cov    # 커버리지
```

## 맵 파일

`maps/` 디렉토리에 JSON 형식으로 위치한다. `startGame` 시 `mapName` 파라미터로 지정하며 기본값은 `JJS`이다.

```json
{
  "cellSize": 1,
  "width": 100,
  "height": 100,
  "originX": 0,
  "originZ": 0,
  "grid": [[...]]
}
```
