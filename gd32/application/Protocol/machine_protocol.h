#ifndef MACHINE_PROTOCOL_H
#define MACHINE_PROTOCOL_H
#include "game_authority.h"
#include <stddef.h>
typedef enum { MC_NONE, MC_SETTINGS_GET, MC_SETTINGS_APPLY, MC_ACCOUNT_GET,
    MC_ENTER, MC_EXIT, MC_AUTH, MC_REPORT, MC_CLEAR } MachineCommand;
typedef struct { MachineCommand type; uint32_t req,base,password,values[GAME_AUTHORITY_SETTINGS_COUNT]; } MachineRequest;
uint8_t MachineProtocol_Parse(const char *line,MachineRequest *request);
uint8_t MachineProtocol_Settings(char *out,size_t size,const GameAuthorityState *s,uint8_t storage);
uint8_t MachineProtocol_Account(char *out,size_t size,const GameAuthorityState *s);
#endif
