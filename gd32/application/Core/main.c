/*!
    \file    main.c
    \brief   Animal Game

    \version 2026-02-12, V3.3.3, demo for GD32F4xx
*/

#include "main.h"
#include "app_main.h"

int main(void)
{
    systick_config();

    IN_init();
    SW_init();
    OUT_init();
    OUT_bit_reset(0xFFFFU);
    LED1_init();
    LED1_off();

    USART2_init();
    I2C0_Init();

    AppMain_Init();

    while(1)
    {
        AppMain_Poll();
    }
}

