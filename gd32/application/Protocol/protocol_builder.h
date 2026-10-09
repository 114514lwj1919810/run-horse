#ifndef PROTOCOL_BUILDER_H
#define PROTOCOL_BUILDER_H

#include <stdint.h>
#include <stddef.h>
#include "game_authority.h"

uint8_t ProtocolBuilder_Hello(char *buffer, size_t size);
uint8_t ProtocolBuilder_Empty(char *buffer, size_t size);
uint8_t ProtocolBuilder_FullState(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence);
uint8_t ProtocolBuilder_ShowIdle(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence);
uint8_t ProtocolBuilder_ShowBetting(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence);
uint8_t ProtocolBuilder_ShowSetting(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence);
uint8_t ProtocolBuilder_ShowPayout(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence);
uint8_t ProtocolBuilder_ShowPayoutDone(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence, uint32_t coins);
uint8_t ProtocolBuilder_ShowRace(char *buffer, size_t size, const GameAuthorityState *state);
uint8_t ProtocolBuilder_ShowResult(char *buffer, size_t size, const GameAuthorityState *state);
uint8_t ProtocolBuilder_ShowDemoRace(char *buffer, size_t size, const GameAuthorityState *state,
    uint32_t sequence, uint32_t round, uint8_t winner, uint32_t duration_ms);
uint8_t ProtocolBuilder_ShowDemoResult(char *buffer, size_t size, const GameAuthorityState *state,
    uint32_t sequence, uint32_t round, uint8_t winner, uint32_t duration_ms);
uint8_t ProtocolBuilder_ShowWinReady(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence);
uint8_t ProtocolBuilder_WinTransferUpdate(char *buffer, size_t size, const GameAuthorityState *state);
uint8_t ProtocolBuilder_WinTransferDone(char *buffer, size_t size, const GameAuthorityState *state, uint8_t fast);

#endif
