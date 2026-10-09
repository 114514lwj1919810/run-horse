#ifndef INPUT_SERVICE_H
#define INPUT_SERVICE_H

#include "board_io.h"
#include <stdint.h>

void InputService_Init(void);
void InputService_Poll(void);
uint8_t InputService_GetKeyPress(BoardKey *key);

#endif
