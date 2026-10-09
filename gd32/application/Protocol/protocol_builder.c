/*
 * protocol_builder.c
 *
 * H743 -> Unity 鍗忚缁勫寘妯″潡銆? *
 * 妯″潡鍔熻兘锛? * 鎵€鏈夎緭鍑虹粰 Unity/APK 鐨勫崗璁瓧绗︿覆閮藉湪杩欓噷鐢熸垚銆? *
 * 褰撳墠琚皝璋冪敤锛? * App/app_main.c 鐨?AppMain_Send* 鍑芥暟璋冪敤鏈ā鍧椼€? *
 * 褰撳墠璋冪敤浜嗕粈涔堬細
 * snprintf()
 * GameAuthority_StateName()
 * GameAuthority_TotalBet()
 * GameAuthority_SettingItemName()
 * GameAuthority_SettingValue()
 * GameAuthority_SettingMinimum()
 * GameAuthority_SettingMaximum()
 *
 * 娉ㄦ剰锛? * 鏈ā鍧楀彧璐熻矗鎶婂凡鏈夋潈濞佺姸鎬佺紪鐮佹垚瀛楃涓诧紝涓嶄慨鏀?GameAuthorityState銆? */

#include "protocol_builder.h"
#include "board_timer.h"
#include <stdio.h>

static unsigned long Remaining(uint32_t deadline,uint32_t allowance)
{
    int32_t left=(int32_t)(deadline-BoardTimer_Millis())-(int32_t)allowance;
    return left>0?(unsigned long)left:1UL;
}

static uint8_t ProtocolBuilder_CheckLength(int length, size_t size)
{
    return ((length > 0) && (length < (int)size)) ? 1U : 0U;
}

uint8_t ProtocolBuilder_Hello(char *buffer, size_t size)
{
    int length;

    if ((buffer == 0) || (size == 0U))
    {
        return 0U;
    }
    length = snprintf(buffer, size, "hello");
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_Empty(char *buffer, size_t size)
{
    if ((buffer == 0) || (size == 0U))
    {
        return 0U;
    }
    buffer[0] = '\0';
    return 1U;
}

uint8_t ProtocolBuilder_FullState(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "F:s=%lu,p=%s,r=%lu,c=%lu,t=%lu,i=%u,"
        "b=%lu|%lu|%lu|%lu|%lu|%lu,o=%lu|%lu|%lu|%lu|%lu|%lu,"
        "w=%u,wt=%lu,wr=%lu,l=%lu,"
        "rs=%lu,es=%lu,cs=%lu,ca=%u,"
        "pp=%lu,f=%lu,pt=%lu,pd=%lu,rt=%lu,et=%lu,ct=%lu",
        (unsigned long)sequence,
        GameAuthority_StateName(state->state),
        (unsigned long)state->round_id,
        (unsigned long)state->balance_points,
        (unsigned long)GameAuthority_TotalBet(state),
        (unsigned int)state->selected_id,
        (unsigned long)state->bets[0],
        (unsigned long)state->bets[1],
        (unsigned long)state->bets[2],
        (unsigned long)state->bets[3],
        (unsigned long)state->bets[4],
        (unsigned long)state->bets[5],
        (unsigned long)state->odds[0],
        (unsigned long)state->odds[1],
        (unsigned long)state->odds[2],
        (unsigned long)state->odds[3],
        (unsigned long)state->odds[4],
        (unsigned long)state->odds[5],
        (unsigned int)state->winner_id,
        (unsigned long)state->win_total_points,
        (unsigned long)state->win_remaining_points,
        (unsigned long)state->last_win_points,
        (unsigned long)state->race_seq,
        (unsigned long)state->result_seq,
        (unsigned long)state->collect_seq,
        (unsigned int)((state->state == GAME_STATE_WIN_COLLECTING) ? 1U : 0U),
        (unsigned long)state->pending_payout_coins,
        (unsigned long)state->fault_code,
        (unsigned long)state->payout_target,
        (unsigned long)state->payout_done,
        state->state==GAME_STATE_WAIT_RACE_OVER?Remaining(state->race_deadline_tick,5000):0UL,
        state->state==GAME_STATE_WAIT_RESULT_OVER?Remaining(state->result_deadline_tick,2000):0UL,
        state->state==GAME_STATE_WIN_COLLECTING?Remaining(state->collect_deadline_tick,0):0UL);

    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowIdle(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_IDLE:seq=%lu,round=%lu,credit=%lu,last_win=%lu,win=%lu",
        (unsigned long)sequence,
        (unsigned long)state->round_id,
        (unsigned long)state->balance_points,
        (unsigned long)state->last_win_points,
        (unsigned long)state->win_remaining_points);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowBetting(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_BETTING:seq=%lu,round=%lu,credit=%lu,total_bet=%lu,selected=%u,"
        "bets=%lu|%lu|%lu|%lu|%lu|%lu,odds=%lu|%lu|%lu|%lu|%lu|%lu,"
        "pending_payout=%lu,fault=%lu",
        (unsigned long)sequence,
        (unsigned long)state->round_id,
        (unsigned long)state->balance_points,
        (unsigned long)GameAuthority_TotalBet(state),
        (unsigned int)state->selected_id,
        (unsigned long)state->bets[0],
        (unsigned long)state->bets[1],
        (unsigned long)state->bets[2],
        (unsigned long)state->bets[3],
        (unsigned long)state->bets[4],
        (unsigned long)state->bets[5],
        (unsigned long)state->odds[0],
        (unsigned long)state->odds[1],
        (unsigned long)state->odds[2],
        (unsigned long)state->odds[3],
        (unsigned long)state->odds[4],
        (unsigned long)state->odds[5],
        (unsigned long)state->pending_payout_coins,
        (unsigned long)state->fault_code);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowSetting(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_SETTING:seq=%lu,index=%u,total=%u,item=%s,value=%lu,min=%lu,max=%lu",
        (unsigned long)sequence,
        (unsigned int)(state->setting_index + 1U),
        (unsigned int)GAME_AUTHORITY_SETTINGS_COUNT,
        GameAuthority_SettingItemName(state->setting_index),
        (unsigned long)GameAuthority_SettingValue(state, state->setting_index),
        (unsigned long)GameAuthority_SettingMinimum(state->setting_index),
        (unsigned long)GameAuthority_SettingMaximum(state->setting_index));
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowPayout(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_PAYOUT:seq=%lu,balance_before=%lu,coins=%lu,remainder=%lu",
        (unsigned long)sequence,
        (unsigned long)(state->balance_points + state->pending_payout_coins * state->payout_unit_locked),
        (unsigned long)state->pending_payout_coins,
        (unsigned long)state->balance_points);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowPayoutDone(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence, uint32_t coins)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_PAYOUT_DONE:seq=%lu,coins=%lu,balance=%lu",
        (unsigned long)sequence,
        (unsigned long)coins,
        (unsigned long)state->balance_points);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowRace(char *buffer, size_t size, const GameAuthorityState *state)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_RACE:seq=%lu,round=%lu,winner=%u,duration_ms=%lu,"
        "bets=%lu|%lu|%lu|%lu|%lu|%lu,odds=%lu|%lu|%lu|%lu|%lu|%lu",
        (unsigned long)state->race_seq,
        (unsigned long)state->round_id,
        (unsigned int)state->winner_id,
        (unsigned long)state->settings.race_duration_ms,
        (unsigned long)state->locked_bets[0],
        (unsigned long)state->locked_bets[1],
        (unsigned long)state->locked_bets[2],
        (unsigned long)state->locked_bets[3],
        (unsigned long)state->locked_bets[4],
        (unsigned long)state->locked_bets[5],
        (unsigned long)state->locked_odds[0],
        (unsigned long)state->locked_odds[1],
        (unsigned long)state->locked_odds[2],
        (unsigned long)state->locked_odds[3],
        (unsigned long)state->locked_odds[4],
        (unsigned long)state->locked_odds[5]);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowResult(char *buffer, size_t size, const GameAuthorityState *state)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_RESULT:seq=%lu,round=%lu,result=%s,winner=%u,odds=%lu,"
        "win_total=%lu,win_remaining=%lu,credit=%lu,duration_ms=%lu",
        (unsigned long)state->result_seq,
        (unsigned long)state->round_id,
        (state->win_points > 0U) ? "WIN" : "LOSE",
        (unsigned int)state->winner_id,
        (unsigned long)state->locked_odds[state->winner_id - 1U],
        (unsigned long)state->win_total_points,
        (unsigned long)state->win_remaining_points,
        (unsigned long)state->balance_points,
        (unsigned long)state->settings.result_duration_ms);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowDemoRace(char *buffer, size_t size, const GameAuthorityState *state,
    uint32_t sequence, uint32_t round, uint8_t winner, uint32_t duration_ms)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_RACE:seq=%lu,round=%lu,winner=%u,duration_ms=%lu,"
        "bets=0|0|0|0|0|0,odds=%lu|%lu|%lu|%lu|%lu|%lu,demo=1",
        (unsigned long)sequence,
        (unsigned long)round,
        (unsigned int)winner,
        (unsigned long)duration_ms,
        (unsigned long)state->odds[0],
        (unsigned long)state->odds[1],
        (unsigned long)state->odds[2],
        (unsigned long)state->odds[3],
        (unsigned long)state->odds[4],
        (unsigned long)state->odds[5]);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowDemoResult(char *buffer, size_t size, const GameAuthorityState *state,
    uint32_t sequence, uint32_t round, uint8_t winner, uint32_t duration_ms)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_RESULT:seq=%lu,round=%lu,result=LOSE,winner=%u,odds=%lu,"
        "win_total=0,win_remaining=0,credit=%lu,duration_ms=%lu,demo=1",
        (unsigned long)sequence,
        (unsigned long)round,
        (unsigned int)winner,
        (unsigned long)state->odds[winner - 1U],
        (unsigned long)state->balance_points,
        (unsigned long)duration_ms);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_ShowWinReady(char *buffer, size_t size, const GameAuthorityState *state, uint32_t sequence)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "SHOW_WIN_READY:seq=%lu,round=%lu,win=%lu,credit=%lu",
        (unsigned long)sequence,
        (unsigned long)state->round_id,
        (unsigned long)state->win_remaining_points,
        (unsigned long)state->balance_points);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_WinTransferUpdate(char *buffer, size_t size, const GameAuthorityState *state)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "WIN_TRANSFER_UPDATE:seq=%lu,round=%lu,win=%lu,credit=%lu,delta=%lu,source=WIN_COLLECT",
        (unsigned long)state->collect_seq,
        (unsigned long)state->round_id,
        (unsigned long)state->win_remaining_points,
        (unsigned long)state->balance_points,
        (unsigned long)state->collect_last_delta);
    return ProtocolBuilder_CheckLength(length, size);
}

uint8_t ProtocolBuilder_WinTransferDone(char *buffer, size_t size, const GameAuthorityState *state, uint8_t fast)
{
    int length;

    length = snprintf(
        buffer,
        size,
        "WIN_TRANSFER_DONE:seq=%lu,round=%lu,win=0,credit=%lu,fast=%u",
        (unsigned long)state->collect_seq,
        (unsigned long)state->round_id,
        (unsigned long)state->balance_points,
        (unsigned int)fast);
    return ProtocolBuilder_CheckLength(length, size);
}
