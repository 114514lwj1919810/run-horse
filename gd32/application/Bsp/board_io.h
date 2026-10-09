#ifndef BOARD_IO_H
#define BOARD_IO_H

#include <stdint.h>

typedef enum
{
    BOARD_KEY_LEFT = 0,
    BOARD_KEY_RIGHT,
    BOARD_KEY_UP,
    BOARD_KEY_DOWN,
    BOARD_KEY_SETTING,
    BOARD_KEY_GAME,
    BOARD_KEY_EXCHANGE,
    BOARD_KEY_SERVICE_RESET,
    BOARD_KEY_COUNT
} BoardKey;

void BoardIO_Init(void);
void BoardIO_Poll(void);
uint8_t BoardIO_ReadInput(uint8_t index);
void BoardIO_WriteOutput(uint8_t index, uint8_t active);
uint8_t BoardIO_GetKeyPress(BoardKey *key);
void BoardIO_StartAndroidLedBlink(void);
void BoardIO_NotifyCommunicationActivity(void);
void BoardIO_ConfigureCounters(uint8_t coin_input, uint8_t payout_input, uint8_t normally_closed);
uint32_t BoardIO_TakeCoinCount(void);
uint32_t BoardIO_TakePayoutCount(void);

#endif
