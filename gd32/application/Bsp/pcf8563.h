/*!
    \file    pcf8563.h
    \brief   the header file of PCF8563 driver

    \version 2026-02-12, V3.3.3, demo for GD32F4xx
*/

#ifndef PCF8563_H
#define PCF8563_H

#include "gd32f4xx.h"

#define PCF8563_ADDR    0x51

#define PCF8563_SEC     0x02
#define PCF8563_MIN     0x03
#define PCF8563_HOUR    0x04
#define PCF8563_DAY     0x05
#define PCF8563_WEEK    0x06
#define PCF8563_MON     0x07
#define PCF8563_YEAR    0x08

typedef struct
{
    uint8_t sec;
    uint8_t min;
    uint8_t hour;

    uint8_t day;
    uint8_t week;
    uint8_t mon;
    uint8_t year;
} rtc_time_t;


uint8_t BCD2DEC(uint8_t bcd);
uint8_t DEC2BCD(uint8_t dec);
int PCF8563_SetTime(uint8_t year, uint8_t mon, uint8_t day,
                    uint8_t hour, uint8_t min, uint8_t sec);
int PCF8563_GetTime(uint8_t *hour, uint8_t *min, uint8_t *sec);
void PCF8563_test(void);

#endif /* PCF8563 */
