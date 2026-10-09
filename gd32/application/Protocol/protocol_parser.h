#ifndef PROTOCOL_PARSER_H
#define PROTOCOL_PARSER_H

#include <stdint.h>

typedef enum
{
    PROTOCOL_RX_NONE = 0,
    PROTOCOL_RX_TEST_A,
    PROTOCOL_RX_PING_TEST,
    PROTOCOL_RX_UNITY_READY,
    PROTOCOL_RX_ACK,
    PROTOCOL_RX_RACE_OVER,
    PROTOCOL_RX_RESULT_OVER,
    PROTOCOL_RX_DEMO_RACE_OVER,
    PROTOCOL_RX_DEMO_RESULT_OVER,
    PROTOCOL_RX_DISPLAY_ERROR
} ProtocolRxType;

typedef struct
{
    ProtocolRxType type;
    uint32_t seq;
    uint32_t round;
} ProtocolRxMessage;

uint8_t ProtocolParser_ParseLine(const char *line, ProtocolRxMessage *message);

#endif
