import { Module } from '@nestjs/common';
import { GameGateway } from './game.gateway';
import { GameService } from './game.service';
import { NpcService } from './npc/npc.service';
import { PathService } from './npc/path.service';
import { PlayerService } from './player/player.service';
import { TensionService } from './tension.service';
import { NpcFactory } from './npc/npc.factory';
import { PlayerFactory } from './player/player.factory';
import { NormalBehavior, SirenBehavior, WhoAreYouBehavior } from './npc/npc.behaviors';

@Module({
  providers: [
    GameGateway,
    GameService,
    NpcService,
    NpcFactory,
    PlayerFactory,
    PathService,
    PlayerService,
    TensionService,
    NormalBehavior,
    SirenBehavior,
    WhoAreYouBehavior,
  ],
  exports: [GameService],
})
export class GameModule {}
