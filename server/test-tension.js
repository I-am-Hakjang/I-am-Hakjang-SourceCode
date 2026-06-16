const WebSocket = require('ws');

/**
 * 텐션 시스템 테스트 스크립트
 *
 * 1. /rooms에 접속하여 방 생성 → 2명 입장 → 게임 시작
 * 2. /games에 2명 접속
 * 3. 두 플레이어를 가까운 거리 + 서로 바라보게 배치 → 텐션 상승 확인
 * 4. tensionEvent / tensionEventEnd 패킷 수신 확인
 *
 * 사용법: node test-tension.js
 */

const SERVER_URL = 'ws://localhost:3000';
const PLAYER_1 = 'tension_tester_1';
const PLAYER_2 = 'tension_tester_2';

let roomId = null;
let uid1 = null;
let uid2 = null;

console.log('========================================');
console.log('  텐션 시스템 테스트');
console.log('========================================\n');

// ─── Step 1: 플레이어 1이 방 생성 ───
const room1 = new WebSocket(`${SERVER_URL}/rooms`);

room1.on('open', () => {
  console.log('✅ Player 1: /rooms 연결');
  room1.send(JSON.stringify({
    event: 'createRoom',
    data: { playerId: PLAYER_1, maxPlayer: 2 },
  }));
});

room1.on('message', (raw) => {
  const msg = JSON.parse(raw);

  if (msg.event === 'error') {
    console.error('❌ Server Room Error:', msg.data?.message || msg.data);
  }

  // createRoom 응답: { event: 'createRoom', data: { room, uid } }
  if (msg.event === 'createRoom' && msg.data?.room && msg.data?.uid) {
    roomId = msg.data.room.id;
    uid1 = msg.data.uid;
    console.log(`✅ Player 1: 방 생성 완료 — roomId: ${roomId}, uid: ${uid1}`);
    joinPlayer2();
  }

  if (msg.event === 'gameStart') {
    console.log(`🚀 Player 1: 게임 시작! sessionId: ${msg.data.sessionId}`);
    setTimeout(() => room1.close(), 100);
    connectBothToGame(msg.data.sessionId);
  }

  if (msg.event === 'roomUpdate') {
    const roomData = msg.data;
    if (roomData.players && roomData.players.every(p => p.isReady) && roomData.players.length >= 2) {
      console.log('✅ 모든 플레이어 준비 완료 — 게임 시작 요청');
      room1.send(JSON.stringify({
        event: 'startGame',
        data: { roomId, uid: uid1, mapName: 'JJS' },
      }));
    }
  }
});

room1.on('error', (err) => console.error('❌ Player 1 Room Error:', err.message));

// ─── Step 2: 플레이어 2 입장 ───
function joinPlayer2() {
  const room2 = new WebSocket(`${SERVER_URL}/rooms`);

  room2.on('open', () => {
    console.log('✅ Player 2: /rooms 연결');
    room2.send(JSON.stringify({
      event: 'joinRoom',
      data: { roomId, playerId: PLAYER_2 },
    }));
  });

  room2.on('message', (raw) => {
    const msg = JSON.parse(raw);

    if (msg.event === 'joinRoom' && msg.data?.uid) {
      uid2 = msg.data.uid;
      console.log(`✅ Player 2: 입장 완료 — uid: ${uid2}`);

      // 준비 완료
      room2.send(JSON.stringify({
        event: 'setReady',
        data: { roomId, uid: uid2 },
      }));
    }

    if (msg.event === 'gameStart') {
      setTimeout(() => room2.close(), 100);
    }
  });

  room2.on('error', (err) => console.error('❌ Player 2 Room Error:', err.message));
}

// ─── Step 3: 게임 접속 + 텐션 테스트 ───
function connectBothToGame(sessionId) {
  console.log('\n--- 게임 접속 및 텐션 테스트 시작 ---\n');

  const game1 = new WebSocket(`${SERVER_URL}/games?sessionId=${sessionId}&playerUid=${uid1}`);
  const game2 = new WebSocket(`${SERVER_URL}/games?sessionId=${sessionId}&playerUid=${uid2}`);

  let connected = 0;
  let tensionSamples = [];
  let tensionEventReceived = false;
  let tensionEventEndReceived = false;
  let receivedEventType = null;

  function onBothConnected() {
    console.log('✅ 두 플레이어 모두 게임 접속 완료\n');

    // ── Phase 1: 서로 다른 구역 배치 (텐션 0이어야 함) ──
    console.log('📍 Phase 1: 멀리 배치 (Player1: Zone 0,0, Player2: Zone 3,3, 텐션 0이어야 함)');
    game1.send(JSON.stringify({
      event: 'unitSync',
      data: { state: 0, x: -2, y: 0, z: -20, r: 0 },
    }));
    game2.send(JSON.stringify({
      event: 'unitSync',
      data: { state: 0, x: 12, y: 0, z: 5, r: 180 },
    }));

    // 7초 후 Phase 2: 같은 구역 (Zone 2,2) 배치 + 서로 바라보기
    setTimeout(() => {
      const phase1Max = Math.max(...tensionSamples, 0);
      console.log(`\n\n📊 Phase 1 결과: 최대 텐션 = ${phase1Max.toFixed(1)} (낮아야 정상)\n`);

      tensionSamples = []; // 리셋
      console.log('📍 Phase 2: 같은 구역 (Zone 2,2) 배치 및 밀접 조우 (거리=2, 서로 바라봄, 텐션 높아야 함)');
      // Player1: (7, 0, -3), r=180 (z- 방향) -> Zone 2,2
      game1.send(JSON.stringify({
        event: 'unitSync',
        data: { state: 0, x: 7, y: 0, z: -3, r: 180 },
      }));
      // Player2: (7, 0, -5), r=0 (z+ 방향) -> Zone 2,2
      game2.send(JSON.stringify({
        event: 'unitSync',
        data: { state: 0, x: 7, y: 0, z: -5, r: 0 },
      }));
    }, 7000);

    // 50초 후 결과 출력 및 종료 (쿨타임 30초 + 여유)
    setTimeout(() => {
      console.log('\n\n========================================');
      console.log('  텐션 테스트 결과');
      console.log('========================================');
      console.log(`  텐션 이벤트 수신:    ${tensionEventReceived ? '✅ YES (' + receivedEventType + ')' : '❌ NO'}`);
      console.log(`  이벤트 종료 수신:    ${tensionEventEndReceived ? '✅ YES' : '❌ NO (아직 안 끝남 or 미발생)'}`);

      const maxTension = Math.max(...tensionSamples, 0);
      console.log(`  Phase 2 최대 텐션:   ${maxTension.toFixed(1)}`);
      console.log(`  최근 텐션 샘플:      ${tensionSamples.slice(-5).map(t => t.toFixed(1)).join(', ')}`);

      if (maxTension >= 70) {
        console.log('\n  ✅ 텐션이 임계값(70) 이상 도달!');
      } else if (maxTension > 0) {
        console.log('\n  ⚠️ 텐션 계산됨, 임계값 미달 (확률적으로 이벤트 미발생 가능)');
      } else {
        console.log('\n  ❌ 텐션이 계산되지 않음');
      }

      if (!tensionEventReceived && maxTension >= 70) {
        console.log('  ℹ️  텐션 >= 70이지만 이벤트 미발생 — 확률(40%) 미충족 가능');
      }

      console.log('========================================\n');

      game1.close();
      game2.close();
      process.exit(0);
    }, 50000);
  }

  // Player 1 메시지 핸들러
  game1.on('open', () => {
    connected++;
    if (connected === 2) onBothConnected();
  });

  game1.on('message', (raw) => {
    const msg = JSON.parse(raw);

    if (msg.event === 'tensionUpdate' && msg.data?.zones) {
      let maxT = 0;
      let maxZone = '0,0';
      for (const z of msg.data.zones) {
        if (z.tension > maxT) {
          maxT = z.tension;
          maxZone = z.zoneId;
        }
      }
      tensionSamples.push(maxT);
      process.stdout.write(`\r  [TensionUpdate] maxTension=${maxT.toFixed(1)} (Zone ${maxZone})    `);
    }

    if (msg.event === 'SMOKE') {
      tensionEventReceived = true;
      receivedEventType = msg.event;
      console.log(`\n\n🔥 텐션 이벤트 발생!! type=${msg.event}, timestamp=${msg.data.timestamp}, center=(${msg.data.centerX.toFixed(2)}, ${msg.data.centerZ.toFixed(2)})`);
    }

    if (msg.event === 'tensionEventEnd') {
      tensionEventEndReceived = true;
      console.log(`\n✅ 텐션 이벤트 종료 수신 (zoneId=${msg.data.zoneId})`);
    }
  });

  // Player 2
  game2.on('open', () => {
    connected++;
    if (connected === 2) onBothConnected();
  });

  game2.on('message', () => { }); // Player 2는 수신만

  game1.on('error', (err) => console.error('❌ Game1 Error:', err.message));
  game2.on('error', (err) => console.error('❌ Game2 Error:', err.message));
}
