/*
 * coin_service.c
 *
 * 鎶曞竵鏈嶅姟妯″潡銆? *
 * 妯″潡鍔熻兘锛? * 1. 鎶娾€滄姇甯侀敭/鎶曞竵鍣ㄨ剦鍐测€濈粺涓€杞崲涓?CREDIT 澧炲姞鍔ㄤ綔銆? * 2. 闅旂 AppMain 鍜?GameAuthority_AddCoin()锛屽悗缁鏋滄姇甯佹敼涓轰腑鏂€? *    鍏夌數璁℃暟銆佷覆鍙ｅ竵鏈烘垨闃叉姈缁熻锛屽彧鏀规湰妯″潡銆? *
 * 褰撳墠琚皝璋冪敤锛? * AppMain_Init()       -> CoinService_Init()
 * AppMain_Poll()       -> CoinService_Poll()
 * AppMain_ProcessKey() -> CoinService_HandlePulse()
 *
 * 褰撳墠璋冪敤浜嗕粈涔堝嚱鏁帮細
 * GameAuthority_AddCoin()
 *
 * 鍏ㄥ眬鍙橀噺锛? * 褰撳墠娌℃湁妯″潡绉佹湁鍏ㄥ眬鍙橀噺銆傛姇甯佺Н鍒嗐€佷綑棰濆拰鐘舵€侀兘淇濆瓨鍦? * GameAuthorityState锛岀敱 AppMain 浼犲叆銆? */

#include "coin_service.h"

void CoinService_Init(void)
{
    /*
     * 璋冪敤浣嶇疆锛?     *   AppMain_Init()
     *
     * 褰撳墠璋冪敤鍑芥暟锛?     *   鏃?     *
     * 鍔熻兘锛?     *   棰勭暀鎶曞竵纭欢鍒濆鍖栧叆鍙ｃ€?     *   鐜板湪 PC8 浣滀负鏅€氱煭鎸夐敭鐢?InputService/BoardIO 鎵弿锛屾墍浠ヨ繖閲屼负绌恒€?     */
}

void CoinService_Poll(void)
{
    /*
     * 璋冪敤浣嶇疆锛?     *   AppMain_Poll()
     *
     * 褰撳墠璋冪敤鍑芥暟锛?     *   鏃?     *
     * 鍔熻兘锛?     *   棰勭暀鎶曞竵鍣ㄩ潪闃诲杞鍏ュ彛銆?     *   鍚庣画濡傛灉鐪熷疄鎶曞竵鍣ㄦ湁鑴夊啿缂撳瓨銆佽秴鏃跺垽鏂€侀槻杩炲嚮缁熻锛屽彲浠ユ斁杩欓噷銆?     */
}

uint8_t CoinService_HandlePulse(GameAuthorityState *state)
{
    /*
     * 璋冪敤浣嶇疆锛?     *   AppMain_ProcessKey() 鏀跺埌 BOARD_KEY_COIN 鏃躲€?     *
     * 褰撳墠璋冪敤鍑芥暟锛?     *   GameAuthority_AddCoin()
     *
     * 鍔熻兘锛?     *   澶勭悊 1 娆℃湁鏁堟姇甯佽剦鍐层€?     *   V3.1 WIN 鏀跺垎瑙勫垯涓紝鎶曞竵鍙鍔?CREDIT锛屼笉鏀瑰彉 WIN锛屼篃涓嶆竻绌轰笅娉ㄣ€?     */
    if (state == 0)
    {
        return 0U;
    }

    return GameAuthority_AddCoin(state);
}
