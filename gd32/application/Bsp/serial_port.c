#include "serial_port.h"
#include "board_io.h"
#include <string.h>

#define SERIAL_TX_RING_SIZE    8192U
#define SERIAL_LINE_QUEUE_SIZE 16U
#define SERIAL_TX_RING_MASK    (SERIAL_TX_RING_SIZE - 1U)
#define SERIAL_LINE_QUEUE_MASK (SERIAL_LINE_QUEUE_SIZE - 1U)

static uint32_t s_usart;
static uint8_t s_initialized;
static uint8_t s_tx_ring[SERIAL_TX_RING_SIZE];
static uint16_t s_tx_head;
static uint16_t s_tx_tail;
static char s_line_build[SERIAL_PORT_MAX_LINE_LENGTH + 1U];
static uint16_t s_line_build_length;
static uint8_t s_line_discarding;
static char s_line_queue[SERIAL_LINE_QUEUE_SIZE][SERIAL_PORT_MAX_LINE_LENGTH + 1U];
static uint16_t s_line_lengths[SERIAL_LINE_QUEUE_SIZE];
static uint8_t s_line_head;
static uint8_t s_line_tail;
static SerialPortStats s_stats;

static size_t SerialPort_LineLength(const char *line)
{
    size_t length;

    length = 0U;
    while((length <= SERIAL_PORT_MAX_LINE_LENGTH) && (line[length] != '\0'))
    {
        length++;
    }
    return length;
}

static uint8_t SerialPort_LineQueueIsFull(void)
{
    return (((s_line_head + 1U) & SERIAL_LINE_QUEUE_MASK) == s_line_tail) ? 1U : 0U;
}

static void SerialPort_QueueCompletedLine(void)
{
    uint8_t next;

    next = (uint8_t)((s_line_head + 1U) & SERIAL_LINE_QUEUE_MASK);
    if(next == s_line_tail)
    {
        s_stats.line_queue_overflow++;
        return;
    }

    memcpy(s_line_queue[s_line_head], s_line_build, s_line_build_length);
    s_line_queue[s_line_head][s_line_build_length] = '\0';
    s_line_lengths[s_line_head] = s_line_build_length;
    s_line_head = next;
}

static void SerialPort_ProcessRxByte(uint8_t value)
{
    if(value == '\r')
    {
        return;
    }

    if(value == '\n')
    {
        if(s_line_discarding == 0U)
        {
            SerialPort_QueueCompletedLine();
        }
        s_line_build_length = 0U;
        s_line_discarding = 0U;
        return;
    }

    if(s_line_discarding != 0U)
    {
        return;
    }

    if(s_line_build_length < SERIAL_PORT_MAX_LINE_LENGTH)
    {
        s_line_build[s_line_build_length++] = (char)value;
    }
    else
    {
        s_line_discarding = 1U;
        s_line_build_length = 0U;
        s_stats.line_too_long++;
    }
}

static void SerialPort_PollTx(void)
{
    uint8_t value;

    if((s_initialized == 0U) || (s_tx_tail == s_tx_head))
    {
        return;
    }

    if(usart_flag_get(s_usart, USART_FLAG_TBE) != RESET)
    {
        value = s_tx_ring[s_tx_tail];
        usart_data_transmit(s_usart, value);
        s_tx_tail = (uint16_t)((s_tx_tail + 1U) & SERIAL_TX_RING_MASK);
        if(value == '\n')
        {
            BoardIO_NotifyCommunicationActivity();
        }
    }
}

bool SerialPort_Init(uint32_t usart_periph)
{
    s_usart = usart_periph;
    s_tx_head = 0U;
    s_tx_tail = 0U;
    s_line_build_length = 0U;
    s_line_discarding = 0U;
    s_line_head = 0U;
    s_line_tail = 0U;
    memset(&s_stats, 0, sizeof(s_stats));
    s_initialized = 1U;
    return true;
}

void SerialPort_Poll(void)
{
    while((s_initialized != 0U) &&
          (SerialPort_LineQueueIsFull() == 0U) &&
          (usart_flag_get(s_usart, USART_FLAG_RBNE) != RESET))
    {
        SerialPort_ProcessRxByte((uint8_t)usart_data_receive(s_usart));
    }

    SerialPort_PollTx();
}

bool SerialPort_GetLine(char *output, size_t output_size)
{
    uint16_t length;

    if((output == NULL) || (s_line_tail == s_line_head))
    {
        return false;
    }

    length = s_line_lengths[s_line_tail];
    if(output_size <= (size_t)length)
    {
        return false;
    }

    memcpy(output, s_line_queue[s_line_tail], length + 1U);
    s_line_tail = (uint8_t)((s_line_tail + 1U) & SERIAL_LINE_QUEUE_MASK);
    return true;
}

bool SerialPort_SendLine(const char *line)
{
    size_t length;
    size_t required;
    uint16_t free_space;
    uint16_t index;

    if((s_initialized == 0U) || (line == NULL))
    {
        return false;
    }

    length = SerialPort_LineLength(line);
    if((length == 0U) || (length > SERIAL_PORT_MAX_LINE_LENGTH))
    {
        return false;
    }

    for(index = 0U; index < (uint16_t)length; index++)
    {
        if((line[index] == '\r') || (line[index] == '\n'))
        {
            return false;
        }
    }

    required = length + 2U;
    free_space = (uint16_t)((s_tx_tail - s_tx_head - 1U) & SERIAL_TX_RING_MASK);
    if((size_t)free_space < required)
    {
        s_stats.tx_ring_overflow++;
        return false;
    }

    for(index = 0U; index < (uint16_t)length; index++)
    {
        s_tx_ring[s_tx_head] = (uint8_t)line[index];
        s_tx_head = (uint16_t)((s_tx_head + 1U) & SERIAL_TX_RING_MASK);
    }
    s_tx_ring[s_tx_head] = '\r';
    s_tx_head = (uint16_t)((s_tx_head + 1U) & SERIAL_TX_RING_MASK);
    s_tx_ring[s_tx_head] = '\n';
    s_tx_head = (uint16_t)((s_tx_head + 1U) & SERIAL_TX_RING_MASK);

    SerialPort_PollTx();
    return true;
}

void SerialPort_GetStats(SerialPortStats *output)
{
    if(output != NULL)
    {
        memcpy(output, &s_stats, sizeof(*output));
    }
}
