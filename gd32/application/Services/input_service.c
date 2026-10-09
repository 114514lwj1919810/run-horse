/*
 * input_service.c
 *
 * 杈撳叆鏈嶅姟妯″潡銆? *
 * 妯″潡鍔熻兘锛? * 缁熶竴璋冪敤 BSP 鐨勬寜閿壂鎻忥紝骞跺悜 AppMain 鎻愪緵鎸夐敭浜嬩欢銆? *
 * 褰撳墠琚皝璋冪敤锛? * AppMain_Init() -> InputService_Init()
 * AppMain_Poll() -> InputService_Poll()
 * AppMain_Poll() -> InputService_GetKeyPress()
 *
 * 褰撳墠璋冪敤浜嗕粈涔堬細
 * BoardIO_Init()
 * BoardIO_Poll()
 * BoardIO_GetKeyPress()
 *
 * 鍚庣画鎵╁睍锛? * 鎽囨潌銆佺粍鍚堥敭銆侀暱鎸夈€佹湇鍔￠敭銆佹姇甯佷腑鏂簨浠堕兘搴斾粠杩欓噷缁熶竴杈撳嚭 AppEvent銆? */

#include "input_service.h"

void InputService_Init(void)
{
    BoardIO_Init();
}

void InputService_Poll(void)
{
    BoardIO_Poll();
}

uint8_t InputService_GetKeyPress(BoardKey *key)
{
    return BoardIO_GetKeyPress(key);
}
