export interface Player {
  uid: string;
  id: string;
  isReady: boolean;
}

export interface Room {
  id: string;
  hostUid: string;
  players: Player[];
  maxPlayer: number;
  status: 'waiting' | 'playing';
}

export interface RoomSummary {
  id: string;
  hostUid: string;
  playerCount: number;
  maxPlayer: number;
}
