/*!
    \file    usart.h
    \brief   definitions for GD32F470_EVAL's leds, keys and COM ports hardware resources

    \version 2026-02-12, V3.3.3, demo for GD32F4xx
*/


#ifndef USART_H
#define USART_H

#ifdef __cplusplus
extern "C" {
#endif

#include "gd32f4xx.h"

/* configure USART0 */
/* TX1    <---> PA9  <---> USART0_TX (AF7) */
/* RX1    <---> PA10 <---> USART0_RX (AF7) */
void USART0_init(void);

/* configure USART1 */
/* FX1-TX <---> PA2  <---> USART1_TX (AF7) */
/* FX1-RX <---> PA3  <---> USART1_RX (AF7) */
void USART1_init(void);

/* configure USART2 */
/* TX3    <---> PB10 <---> USART2_TX (AF7) */
/* RX3    <---> PB11 <---> USART2_RX (AF7) */
void USART2_init(void);

/* configure UART3 */
/* FX2-TX <---> PA0  <---> UART3_TX (AF8)  */
/* FX2-RX <---> PA1  <---> UART3_RX (AF8)  */
void UART3_init(void);

/* configure UART4 */
/* TX5    <---> PC12 <---> UART4_TX (AF8)  */
/* RX5    <---> PD2  <---> UART4_RX (AF8)  */
void UART4_init(void);

/* configure USART5 */
/* TX6    <---> PC6  <---> USART5_TX (AF8) RS485 */
/* RX6    <---> PC7  <---> USART5_RX (AF8) RS485 */
void USART5_init(void);

void USART_send_byte( uint32_t usart_periph, uint8_t data);
void USART_send_bytes( uint32_t usart_periph, uint8_t *data, uint8_t length);
uint8_t USART_receive_byte(uint32_t usart_periph);

#ifdef __cplusplus
}

#endif

#endif /* USART_H */
