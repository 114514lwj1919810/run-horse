#ifndef BOARD_TIMER_H
#define BOARD_TIMER_H

#include <stdint.h>

void BoardTimer_Init(void);
void BoardTimer_OnTim6Irq(void);
uint32_t BoardTimer_Millis(void);
uint8_t BoardTimer_Consume10msFlag(void);
uint8_t BoardTimer_Consume1sFlag(void);

#endif
