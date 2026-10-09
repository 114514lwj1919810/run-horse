#ifndef APP_MAIN_H
#define APP_MAIN_H

#include <stdint.h>

typedef enum
{
    STANDBY_ANIMATION_OFF = 0,
    STANDBY_ANIMATION_RACE,
    STANDBY_ANIMATION_RESULT
} StandbyAnimationState;

void AppMain_Init(void);
void AppMain_Poll(void);
StandbyAnimationState AppMain_GetStandbyAnimationState(void);
uint8_t AppMain_GetStandbyWinner(void);

#endif
