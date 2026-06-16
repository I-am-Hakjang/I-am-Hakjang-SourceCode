import { Module } from '@nestjs/common';
import { AppController } from './app.controller';
import { AppService } from './app.service';
import { GameModule } from './games/game.module';
import { RoomModule } from './rooms/room.module';

@Module({
  imports: [RoomModule, GameModule],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}
