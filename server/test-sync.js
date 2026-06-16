const WebSocket = require('ws');

/**
 * 이 스크립트는 서버의 위치 동기화 기능을 테스트하기 위한 도구입니다.
 * 1. /room에 접속하여 방을 생성하고 게임을 시작합니다.
 * 2. 받은 sessionId를 이용해 /game에 접속합니다.
 * 3. 서버에서 주기적으로 보내주는 worldState(NPC 및 유저 좌표)를 확인합니다.
 */

const SERVER_URL = 'ws://localhost:3000';

// 1. /room 접속 (방 생성 및 게임 시작용)
const roomWs = new WebSocket(`${SERVER_URL}/room`);

roomWs.on('open', () => {
  console.log('✅ Connected to /room');
  
  // 방 생성 요청
  console.log('--- Sending createRoom request ---');
  roomWs.send(JSON.stringify({
    event: 'createRoom',
    data: { playerId: 'player_test_1', maxPlayer: 2 }
  }));
});

let gameStarted = false;

roomWs.on('message', (data) => {
  let response;
  try {
    response = JSON.parse(data);
  } catch (e) {
    console.log('[Raw Message Received]:', data.toString());
    return;
  }
  
  console.log('[Room Received]:', JSON.stringify(response, null, 2));

  // 1. 방 생성 성공 확인 (서버가 { room, playerId } 형태의 raw 객체를 반환할 경우)
  const roomData = response.room || (response.event === 'roomUpdate' ? response.data : null);
  
  if (roomData && roomData.id && !gameStarted) {
    const roomId = roomData.id;
    console.log(`✅ Room identified. ID: ${roomId}`);
    
    // 방장이면 게임 시작 요청
    if (roomData.hostId === 'player_test_1') {
      console.log('--- Sending startGame request ---');
      roomWs.send(JSON.stringify({
        event: 'startGame',
        data: { roomId: roomId, playerId: 'player_test_1' }
      }));
      gameStarted = true; // 중복 요청 방지
    }
  }

  // 2. 게임 시작 성공 확인 (sessionId 추출)
  // 서버가 리턴값으로 { sessionId }를 주거나, 'gameStart' 이벤트를 브로드캐스트함
  const sessionId = response.sessionId || (response.event === 'gameStart' ? response.data.sessionId : null);
  
  if (sessionId) {
    console.log(`🚀 Game started! Session ID: ${sessionId}`);
    connectToGame(sessionId);
    roomWs.close(); // 세션 시작 후 룸 소켓 종료
  }

  if (response.event === 'error') {
    console.error('❌ Room Error:', response.data.message);
  }
});

roomWs.on('error', (err) => {
  console.error('❌ Connection Error (/room):', err.message);
});

function connectToGame(sessionId) {
  // 2. /game 접속 (위치 동기화 테스트용)
  const gameWs = new WebSocket(`${SERVER_URL}/game?sessionId=${sessionId}&playerId=player_test_1`);

  gameWs.on('open', () => {
    console.log('✅ Connected to /game');
    console.log('Monitoring worldState updates (NPC movement)...');
    
    // 2초 뒤에 내 위치 업데이트 테스트
    setTimeout(() => {
      console.log('\n--- Updating player position to (10, 0, 20) ---');
      gameWs.send(JSON.stringify({
        event: 'updateCoordinate',
        data: { x: 10, y: 0, z: 20 }
      }));
    }, 2000);
  });

  gameWs.on('message', (data) => {
    const response = JSON.parse(data);
    
    // 주기적으로 들어오는 worldState 확인
    if (response.event === 'worldState') {
      process.stdout.write(`\r[WorldState] Players: ${response.data.players.length} | NPCs: ${response.data.npcs.length} | Time: ${new Date(response.data.timestamp).toLocaleTimeString()}`);
      
      // 플레이어 좌표가 내가 보낸 값으로 업데이트 되었는지 확인
      const me = response.data.players.find(p => p.id === 'player_test_1');
      if (me && me.x === 10) {
        console.log('\n✅ Player position sync verified!');
      }
    } else if (response.event === 'connected') {
      console.log('✅ Game session handshake successful');
    }
  });

  gameWs.on('error', (err) => {
    console.error('❌ Connection Error (/game):', err.message);
  });
}
