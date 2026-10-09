/*!
    \file    output.h
    \brief   the header file of output pin of users.

    \version 2026-08-02, V3.3.3, demo for GD32F4xx
*/

#ifndef OUTPUT_H
#define OUTPUT_H

#include "gd32f4xx_rcu.h"
#include "systick.h"
#include <stdint.h>

typedef struct
{
    uint32_t gpio_periph;
    uint32_t pin;
    rcu_periph_enum periph;
} output_pin_t;


/* OUT-Hx initialize  */
void OUT_init(void);
void OUT_bit_write(uint32_t pin);
void OUT_bit_set(uint32_t pin);
void OUT_bit_reset(uint32_t pin);
void OUT_chaser_lights(uint32_t ms);

/* LED1 initialize  */
void LED1_init(void);
void LED1_on(void);
void LED1_off(void);
void LED1_flash(uint32_t ms);

#endif /* OUTPUT_H */
