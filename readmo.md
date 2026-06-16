# I-am-Hakjang Unity 소스 개요 (readmo)

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
