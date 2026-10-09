/*
 * win_collect_service.c
 *
 * WIN 鏀跺垎鏈嶅姟妯″潡銆? *
 * 妯″潡鍔熻兘锛? * 1. 绠＄悊 RESULT 鍚?WIN 鍒嗘暟杞叆 CREDIT 鐨勬湇鍔″叆鍙ｃ€? * 2. 鎶娾€滃紑濮嬫敹鍒嗐€侀€愭鏀跺垎銆佸揩閫熸敹瀹屸€濅笁涓姩浣滀粠 AppMain 鎷嗗嚭鏉ャ€? * 3. 淇濇寔 V3.1 瑙勫垯锛氫腑濂栧厛杩涘叆 WIN锛屼笉鑷姩鍔?CREDIT锛涚帺瀹舵寜寮€濮嬮敭鍚庢墠鏀跺垎銆? *
 * 褰撳墠琚皝璋冪敤锛? * AppMain_Init()       -> WinCollectService_Init()
 * AppMain_ProcessKey() -> WinCollectService_Start()
 * AppMain_ProcessKey() -> WinCollectService_FastFinish()
 * AppMain_Poll()       -> WinCollectService_Poll()
 *
 * 褰撳墠璋冪敤浜嗕粈涔堝嚱鏁帮細
 * GameAuthority_StartWinCollect()
 * GameAuthority_PollWinCollect()
 * GameAuthority_FastFinishWinCollect()
 *
 * 鍏ㄥ眬鍙橀噺锛? * 褰撳墠娌℃湁妯″潡绉佹湁鍏ㄥ眬鍙橀噺銆? * WIN 鎬诲垎銆佸墿浣?WIN銆佹敹鍒?seq銆佹瘡娆¤浆鍏?delta 鍧囦繚瀛樺湪 GameAuthorityState銆? */

#include "win_collect_service.h"

void WinCollectService_Init(void)
{
    /*
     * 璋冪敤浣嶇疆锛?     *   AppMain_Init()
     *
     * 褰撳墠璋冪敤鍑芥暟锛?     *   鏃?     *
     * 鍔熻兘锛?     *   棰勭暀 WIN 鏀跺垎鐩稿叧纭欢鎴栬鏃惰祫婧愬垵濮嬪寲鍏ュ彛銆?     *   褰撳墠鏀跺垎鑺傚鐢?GameAuthorityState 鍐呯殑鏃堕棿鎴虫帶鍒躲€?     */
}

uint8_t WinCollectService_Start(GameAuthorityState *state, uint32_t now_tick)
{
    /*
     * 璋冪敤浣嶇疆锛?     *   AppMain_ProcessKey() 鍦?GAME_STATE_WIN_WAIT_START 鏀跺埌寮€濮嬮敭鏃躲€?     *
     * 褰撳墠璋冪敤鍑芥暟锛?     *   GameAuthority_StartWinCollect()
     *
     * 鍔熻兘锛?     *   浠庘€滅瓑寰呯帺瀹舵敹鍒嗏€濊繘鍏モ€滄鍦ㄩ€愭鏀跺垎鈥濄€?     */
    if (state == 0)
    {
        return 0U;
    }

    return GameAuthority_StartWinCollect(state, now_tick);
}

uint8_t WinCollectService_Poll(GameAuthorityState *state, uint32_t now_tick)
{
    /*
     * 璋冪敤浣嶇疆锛?     *   AppMain_Poll() 姣忚疆涓诲惊鐜皟鐢ㄣ€?     *
     * 褰撳墠璋冪敤鍑芥暟锛?     *   GameAuthority_PollWinCollect()
     *
     * 鍔熻兘锛?     *   闈為樆濉炴帹杩?WIN -> CREDIT 鐨勯€愭鏀跺垎杩囩▼銆?     *   杩斿洖 1 琛ㄧず鏈疆鍙戠敓浜嗗垎鏁板彉鍖栵紝AppMain 闇€瑕侀€氱煡 Unity銆?     */
    if (state == 0)
    {
        return 0U;
    }

    return GameAuthority_PollWinCollect(state, now_tick);
}

uint8_t WinCollectService_FastFinish(GameAuthorityState *state)
{
    /*
     * 璋冪敤浣嶇疆锛?     *   AppMain_ProcessKey() 鍦?GAME_STATE_WIN_COLLECTING 鍐嶆鏀跺埌寮€濮嬮敭鏃躲€?     *
     * 褰撳墠璋冪敤鍑芥暟锛?     *   GameAuthority_FastFinishWinCollect()
     *
     * 鍔熻兘锛?     *   鎶婂墿浣?WIN 涓€娆℃€ц浆鍏?CREDIT锛岀敤浜庣帺瀹剁煭鎸夊紑濮嬮敭蹇€熸敹鍒嗐€?     */
    if (state == 0)
    {
        return 0U;
    }

    return GameAuthority_FastFinishWinCollect(state);
}
