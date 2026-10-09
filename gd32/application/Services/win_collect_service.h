#ifndef WIN_COLLECT_SERVICE_H
#define WIN_COLLECT_SERVICE_H

#include "game_authority.h"
#include <stdint.h>

void WinCollectService_Init(void);
uint8_t WinCollectService_Start(GameAuthorityState *state, uint32_t now_tick);
uint8_t WinCollectService_Poll(GameAuthorityState *state, uint32_t now_tick);
uint8_t WinCollectService_FastFinish(GameAuthorityState *state);

#endif
