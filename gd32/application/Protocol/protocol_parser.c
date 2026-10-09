/*
 * protocol_parser.c
 *
 * Unity -> H743 鍗忚瑙ｆ瀽妯″潡銆? *
 * 妯″潡鍔熻兘锛? * 鎶婁覆鍙ｆ敹鍒扮殑涓€琛?ASCII 鏂囨湰瑙ｆ瀽鎴?ProtocolRxMessage銆? *
 * 褰撳墠琚皝璋冪敤锛? * AppMain_ProcessSerialLine() 璋冪敤 ProtocolParser_ParseLine()銆? *
 * 褰撳墠璋冪敤浜嗕粈涔堬細
 * strcmp(), strncmp(), strstr(), strlen(), strtoul()銆? *
 * 宸叉敮鎸佹姤鏂囷細
 * A
 * PING_TEST
 * UNITY_READY:app=AnimalRace,version=1.2.0,protocol=3.1
 * ACK:seq=10,status=OK
 * RACE_OVER:seq=11,round=1
 * RESULT_OVER:seq=12,round=1
 * DISPLAY_ERROR:seq=11,round=1
 */

#include "protocol_parser.h"
#include <stdlib.h>
#include <string.h>

static uint8_t ProtocolParser_ParseU32Field(const char *line, const char *key, uint32_t *value)
{
    const char *text;
    const char *end_text;
    uint32_t parsed=0;
    size_t key_length;

    if ((line == 0) || (key == 0) || (value == 0))
    {
        return 0U;
    }

    text = strstr(line, key);
    if (text == 0 || text==line || (text[-1]!=':' && text[-1]!=',') || strstr(text+strlen(key),key)!=0)
    {
        return 0U;
    }

    key_length = strlen(key);
    end_text=text+key_length;
    while(*end_text>='0' && *end_text<='9') {
        uint32_t digit=(uint32_t)(*end_text++-'0');
        if(parsed>(UINT32_MAX-digit)/10) return 0;
        parsed=parsed*10+digit;
    }
    if ((end_text == (text + key_length)) ||
        ((*end_text != ',') && (*end_text != '\0')))
    {
        return 0U;
    }

    *value = (uint32_t)parsed;
    return 1U;
}

uint8_t ProtocolParser_ParseLine(const char *line, ProtocolRxMessage *message)
{
    /*
     * 璋冪敤浣嶇疆锛?     *   App/app_main.c -> AppMain_ProcessSerialLine()
     *
     * 璋冪敤鍑芥暟锛?     *   strcmp(), strncmp(), strstr(), ProtocolParser_ParseU32Field()
     *
     * 鍔熻兘锛?     *   灏嗕竴琛屼覆鍙ｆ枃鏈浆鎹负缁撴瀯鍖栨秷鎭€?     */
    if ((line == 0) || (message == 0))
    {
        return 0U;
    }

    message->type = PROTOCOL_RX_NONE;
    message->seq = 0U;
    message->round = 0U;

    if (strcmp(line, "A") == 0)
    {
        message->type = PROTOCOL_RX_TEST_A;
        return 1U;
    }

    if (strcmp(line, "PING_TEST") == 0)
    {
        message->type = PROTOCOL_RX_PING_TEST;
        return 1U;
    }

    if ((strncmp(line, "UNITY_READY:", 12U) == 0) &&
        (strstr(line, "app=AnimalRace") != 0) &&
        (strstr(line, ",protocol=3.2") != 0) &&
        (strstr(line, ",protocol=3.2")[13]=='\0' || strstr(line, ",protocol=3.2")[13]==','))
    {
        message->type = PROTOCOL_RX_UNITY_READY;
        return 1U;
    }

    if ((strncmp(line, "ACK:", 4U) == 0) &&
        (strstr(line, "status=OK") != 0) &&
        (ProtocolParser_ParseU32Field(line, "seq=", &message->seq) != 0U))
    {
        message->type = PROTOCOL_RX_ACK;
        return 1U;
    }

    if ((strncmp(line, "RACE_OVER:", 10U) == 0) &&
        (ProtocolParser_ParseU32Field(line, "seq=", &message->seq) != 0U) &&
        (ProtocolParser_ParseU32Field(line, "round=", &message->round) != 0U))
    {
        message->type = PROTOCOL_RX_RACE_OVER;
        return 1U;
    }

    if ((strncmp(line, "RESULT_OVER:", 12U) == 0) &&
        (ProtocolParser_ParseU32Field(line, "seq=", &message->seq) != 0U) &&
        (ProtocolParser_ParseU32Field(line, "round=", &message->round) != 0U))
    {
        message->type = PROTOCOL_RX_RESULT_OVER;
        return 1U;
    }

    if ((strncmp(line, "DEMO_RACE_OVER:", 15U) == 0) &&
        (ProtocolParser_ParseU32Field(line, "seq=", &message->seq) != 0U) &&
        (ProtocolParser_ParseU32Field(line, "round=", &message->round) != 0U))
    {
        message->type = PROTOCOL_RX_DEMO_RACE_OVER;
        return 1U;
    }

    if ((strncmp(line, "DEMO_RESULT_OVER:", 17U) == 0) &&
        (ProtocolParser_ParseU32Field(line, "seq=", &message->seq) != 0U) &&
        (ProtocolParser_ParseU32Field(line, "round=", &message->round) != 0U))
    {
        message->type = PROTOCOL_RX_DEMO_RESULT_OVER;
        return 1U;
    }

    if ((strncmp(line, "DISPLAY_ERROR:", 14U) == 0) &&
        (ProtocolParser_ParseU32Field(line, "seq=", &message->seq) != 0U))
    {
        ProtocolParser_ParseU32Field(line, "round=", &message->round);
        message->type = PROTOCOL_RX_DISPLAY_ERROR;
        return 1U;
    }

    return 0U;
}
