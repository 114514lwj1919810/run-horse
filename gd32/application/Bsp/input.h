/*!
    \file    input.h
    \brief   the header file of input pin of users.

    \version 2026-08-02, V3.3.3, demo for GD32F4xx
*/

#ifndef INPUT_H
#define INPUT_H

#include <stdint.h>

/* IN-Kx Socket  */
void IN_init(void);
uint32_t IN_bit_get(void);
void IN_test(void);

/* SW1 Ports  */
void SW_init(void);
uint8_t SW_bit_get(void);
void SW_test(void);

#endif /* INPUT_H */
