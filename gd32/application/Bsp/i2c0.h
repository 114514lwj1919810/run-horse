/*!
    \file    i2c0.h
    \brief   the header file of I2C0 driver

    \version 2026-02-12, V3.3.3, demo for GD32F4xx
*/

#ifndef I2C0_H
#define I2C0_H

#include "gd32f4xx.h"

#define I2C_SPEED        100000
#define I2C_TIMEOUT_MAX  100000

#define I2C_OK       0
#define I2C_ERROR   -1
#define I2C_TIMEOUT -2
#define I2C_NACK    -3
#define I2C_BUS_ERR -4
#define I2C_ARB_LOST -5
#define I2C_OVERRUN -6


void I2C0_Init(void);
void I2C0_BusReset(void);
int I2C0_WriteByte(uint8_t dev_addr, const uint8_t *buf, uint16_t len);
int I2C0_ReadByte(uint8_t dev_addr, uint8_t *buf, uint16_t len);
int I2C0_WriteRead(uint8_t dev_addr,
                   const uint8_t *write_buf, uint16_t write_len,
                   uint8_t *read_buf, uint16_t read_len);
int I2C0_Probe(uint8_t dev_addr);

#endif /* I2C0_H */
