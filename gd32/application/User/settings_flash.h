#ifndef _SETTINGS_FLASH_H
#define _SETTINGS_FLASH_H

#include "game_authority.h"
#include <stdint.h>

uint8_t SettingsFlash_Load(GameSettings *settings);
uint8_t SettingsFlash_Save(const GameSettings *settings);
uint8_t RuntimeStorage_Load(GameAuthorityState *state);
uint8_t RuntimeStorage_Save(const GameAuthorityState *state);

#endif
