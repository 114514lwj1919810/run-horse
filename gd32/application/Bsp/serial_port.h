#ifndef SERIAL_PORT_H
#define SERIAL_PORT_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include "gd32f4xx.h"

#define SERIAL_PORT_MAX_LINE_LENGTH 1024U

typedef struct
{
    uint32_t rx_ring_overflow;
    uint32_t rx_restart_error;
    uint32_t line_too_long;
    uint32_t line_queue_overflow;
    uint32_t tx_ring_overflow;
    uint32_t tx_start_error;
    uint32_t uart_error;
} SerialPortStats;

bool SerialPort_Init(uint32_t usart_periph);
void SerialPort_Poll(void);
bool SerialPort_GetLine(char *output, size_t output_size);
bool SerialPort_SendLine(const char *line);
void SerialPort_GetStats(SerialPortStats *output);

#endif
