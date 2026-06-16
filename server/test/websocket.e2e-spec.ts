import { INestApplication } from '@nestjs/common';
import { Test, TestingModule } from '@nestjs/testing';
import { WsAdapter } from '@nestjs/platform-ws';
import { WebSocket } from 'ws';
import { AppModule } from '../src/app.module';
import { GameGateway } from '../src/games/game.gateway';

const WS_PORT = 3001;
const WS_URL = `ws://localhost:${WS_PORT}/ws`;

interface ConnectedClient {
  ws: WebSocket;
  firstMessage: Promise<string>;
}

// Set up the message listener BEFORE resolving open, so we never miss the
// 'connected' greeting that the server sends immediately on handshake.
function connect(): Promise<ConnectedClient> {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(WS_URL);
    const firstMessage = nextMessage(ws);
    ws.once('open', () => resolve({ ws, firstMessage }));
    ws.once('error', reject);
  });
}

function nextMessage(ws: WebSocket): Promise<string> {
  return new Promise((resolve, reject) => {
    ws.once('message', (data) => resolve(data.toString()));
    ws.once('error', reject);
  });
}

function close(ws: WebSocket): Promise<void> {
  return new Promise((resolve) => {
    if (ws.readyState === WebSocket.CLOSED) return resolve();
    ws.once('close', () => resolve());
    ws.close();
  });
}

describe('GameGateway (e2e)', () => {
  let app: INestApplication;
  let gateway: GameGateway;

  beforeAll(async () => {
    const module: TestingModule = await Test.createTestingModule({
      imports: [AppModule],
    }).compile();

    app = module.createNestApplication();
    app.useWebSocketAdapter(new WsAdapter(app));
    await app.listen(WS_PORT);

    gateway = module.get(GameGateway);
  }, 10000);

  afterAll(async () => {
    await app.close();
  });

  describe('connection', () => {
    it('receives connected event on connect', async () => {
      const { ws, firstMessage } = await connect();
      const msg = JSON.parse(await firstMessage);

      expect(msg).toEqual({ event: 'connected' });

      await close(ws);
    });
  });

  describe('disconnection', () => {
    it('server client list shrinks after disconnect', async () => {
      const { ws, firstMessage } = await connect();
      await firstMessage; // consume 'connected'

      const countBefore = gateway.server.clients.size;
      await close(ws);

      // give the server a tick to process the close
      await new Promise((r) => setTimeout(r, 50));

      expect(gateway.server.clients.size).toBe(countBefore - 1);
    });
  });

  describe('broadcast', () => {
    it('all connected clients receive the same coordinates payload', async () => {
      const clients = await Promise.all([connect(), connect(), connect()]);
      await Promise.all(clients.map((c) => c.firstMessage)); // consume greetings

      const coordinates = Array.from({ length: 50 }, (_, i) => ({
        id: i,
        x: parseFloat(Math.random().toFixed(4)),
        y: 0,
        z: parseFloat(Math.random().toFixed(4)),
      }));

      const pending = Promise.all(clients.map((c) => nextMessage(c.ws)));
      gateway.broadcastCoordinates(coordinates);
      const messages = await pending;

      const parsed = messages.map((m) => JSON.parse(m));
      parsed.forEach((msg) => {
        expect(msg.event).toBe('coordinates');
        expect(msg.data).toHaveLength(50);
        expect(msg.data).toEqual(coordinates);
      });

      await Promise.all(clients.map((c) => close(c.ws)));
    });
  });
});
