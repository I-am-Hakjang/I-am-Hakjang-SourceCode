import { Module } from '@nestjs/common';
import { RoomGateway } from './room.gateway';
import { RoomService } from './room.service';
import { GameModule } from '../games/game.module';

@Module({
  imports: [GameModule],
  providers: [RoomGateway, RoomService],
  exports: [RoomService],
})
export class RoomModule {}
