<img width="848" height="1236" alt="1781609661185" src="https://github.com/user-attachments/assets/7f52890d-106c-4398-b7c0-d7ec0d717147" />
﻿# I-am-Hakjang Unity 소스 개요 (README)

Unity 폴더 내 C# 소스 코드의 역할을 파일 단위로 간략히 정리했습니다.

## Editor
- `Unity/Editor/ManagerAutoAdder.cs` : 씬/오브젝트에 매니저 컴포넌트를 자동으로 배치하거나 연결하는 에디터 유틸리티입니다.
- `Unity/Editor/NavMeshGridExporter.cs` : 내비게이션/그리드 관련 데이터를 에디터에서 추출(Export)하는 도구 윈도우입니다.
- `Unity/Editor/RandomizeRotation.cs` : 선택 오브젝트 회전을 랜덤으로 적용하는 에디터 도구입니다.
- `Unity/Editor/Windows/DebugManagerWindow.cs` : 디버그 매니저 상태/동작을 제어하기 위한 커스텀 에디터 윈도우입니다.

## Network
- `Unity/Network/NetworkedAnimation.cs` : 네트워크 동기화를 위한 애니메이션 상태(enum 포함) 전송/적용 로직입니다.
- `Unity/Network/NetworkedTransform.cs` : 플레이어/유닛 위치와 Y축 회전을 주기적으로 송신하고 원격 오브젝트를 보간/스냅 처리합니다.

## ScriptableObject
- `Unity/ScriptableObject/BaseData.cs` : 데이터 에셋의 공통 기반이 되는 추상 ScriptableObject 타입입니다.
- `Unity/ScriptableObject/Database.cs` : 게임 데이터 에셋들을 통합 관리하고 ID 기반 접근을 제공하는 데이터베이스 에셋입니다.
- `Unity/ScriptableObject/PlayerData.cs` : 플레이어 이동/전투 관련 설정값(MovementData, CombatData)을 담는 데이터 에셋입니다.
- `Unity/ScriptableObject/PrefabList.cs` : 프리팹 참조 목록과 프리팹 ID 매핑 생성을 담당하는 에셋입니다.

## System
- `Unity/System/DataIDs.cs` : 데이터 에셋 접근에 사용하는 상수 ID 모음입니다.
- `Unity/System/DataManager.cs` : 데이터 로딩/조회/캐싱 등 런타임 데이터 접근의 중심 매니저입니다.
- `Unity/System/DataManagerBehaviour.cs` : DataManager를 씬 라이프사이클에 연결하는 Mono/Manager Behaviour 래퍼입니다.
- `Unity/System/DebugManager.cs` : 디버그 기능 상태 관리 및 디버그 관련 서비스 제공 매니저입니다.
- `Unity/System/DebugManagerBehaviour.cs` : DebugManager를 씬 오브젝트와 연결하는 Behaviour입니다.
- `Unity/System/ManagerBehaviour.cs` : 공통 매니저 Behaviour의 기반 추상 클래스입니다.
- `Unity/System/NetworkManager.cs` : 서버 연결, 이벤트 송수신, 룸/매치/게임 흐름을 관리하는 네트워크 핵심 매니저입니다.
- `Unity/System/NetworkManager.DTO.cs` : 네트워크 메시지 DTO(룸, 매치, 킬로그, 결과 등)와 직렬화용 타입 정의입니다.
- `Unity/System/NetworkManagerBehaviour.cs` : NetworkManager의 초기화/업데이트를 담당하는 Behaviour입니다.
- `Unity/System/Prefabable.cs` : 프리팹 ID/레지스트리와 연결되는 오브젝트용 공통 컴포넌트입니다.
- `Unity/System/PrefabIds.cs` : 프리팹 식별 상수(ID) 모음입니다.
- `Unity/System/ResourceManager.cs` : 리소스 로딩/보관/해제를 담당하는 매니저입니다.
- `Unity/System/ResourceManagerBehaviour.cs` : ResourceManager 라이프사이클 연결 Behaviour입니다.
- `Unity/System/Root.cs` : 전역 매니저 접근 지점(서비스 로케이터 역할)의 정적 엔트리 포인트입니다.
- `Unity/System/SystemObjectController.cs` : 시스템 오브젝트 초기화/활성 상태를 제어하는 컨트롤러입니다.
- `Unity/System/UpdateOrderGroup.cs` : 업데이트 순서 그룹을 정의하는 enum입니다.

## UI
- `Unity/UI/ConnectionController.cs` : 연결/로비/게임 진입 플로우 상태를 제어하는 상위 UI 컨트롤러입니다.
- `Unity/UI/ConnectUI.cs` : 서버 접속 화면 입력/버튼 처리 UI입니다.
- `Unity/UI/GameOverUI.cs` : 게임 종료 결과 표시 UI입니다.
- `Unity/UI/GameUI.cs` : 인게임 HUD 전반을 관리하는 메인 UI입니다.
- `Unity/UI/KillLogUI.cs` : 킬 로그 항목 생성/표시/정리를 담당하는 UI입니다.
- `Unity/UI/LobbyUI.cs` : 로비 플레이어 목록/준비 상태/시작 관련 UI입니다.

## Unit
- `Unity/Unit/AnimationParams.cs` : 애니메이터 파라미터 이름/해시를 관리하는 공용 정의입니다.
- `Unity/Unit/BaseUnit.cs` : 플레이어/NPC 공통 속성(ID, 소유권, 상태 등)과 기본 동작을 제공하는 베이스 클래스입니다.
- `Unity/Unit/RagDollController.cs` : 피격/사망 등 상황에서 랙돌 활성화/복귀를 제어합니다.
- `Unity/Unit/NPC/NPC.cs` : NPC 전용 유닛 로직 클래스입니다.
- `Unity/Unit/Player/Player.cs` : 플레이어 엔티티의 상위 도메인 로직 클래스입니다.
- `Unity/Unit/Player/PlayerAnimationDriver.cs` : 입력/이동/전투 상태를 애니메이터 파라미터로 반영합니다.
- `Unity/Unit/Player/PlayerAttackDetector.cs` : 공격 판정(히트 감지) 처리 로직입니다.
- `Unity/Unit/Player/PlayerCameraRig.cs` : 플레이어 추적 카메라 리그의 위치/회전 제어 로직입니다.
- `Unity/Unit/Player/PlayerController.cs` : 플레이어 입력, 이동, 전투, 네트워크 연계를 오케스트레이션하는 핵심 컨트롤러입니다.
- `Unity/Unit/Player/PlayerInputProvider.cs` : 플레이어 입력 수집 및 표준화 인터페이스를 제공합니다.
- `Unity/Unit/Player/PlayerMovementState.cs` : 플레이어 이동 상태 계산/전이 로직을 담당합니다.

## Utils
- `Unity/Utils/Util.cs` : 컴포넌트 획득, 공통 계산 등 프로젝트 전반에서 재사용되는 유틸리티 함수 모음입니다.

---

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
server/src/
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
