#ifndef COIN_SERVICE_H
#define COIN_SERVICE_H

#include "game_authority.h"
#include <stdint.h>

void CoinService_Init(void);
void CoinService_Poll(void);
uint8_t CoinService_HandlePulse(GameAuthorityState *state);

#endif
