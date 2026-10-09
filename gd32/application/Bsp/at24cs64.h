/*!
    \file    at24cs64.h
    \brief   the header file of AT24CS64 driver

    \version 2026-02-12, V3.3.3, demo for GD32F4xx
*/

#ifndef AT24CS64_H
#define AT24CS64_H

#include "gd32f4xx.h"

#define AT24CS64_ADDR   0x50

int AT24CS64_WriteByte(uint16_t addr, uint8_t data);
int AT24CS64_ReadByte(uint16_t addr, uint8_t *data);
int AT24CS64_ReadBuffer(uint16_t addr, uint8_t *data, uint16_t length);
int AT24CS64_WriteBuffer(uint16_t addr, const uint8_t *data, uint16_t length);

void AT24CS64_test(void);

#endif /* AT24CS64 */
