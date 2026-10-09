#include "game_authority.h"
#include <string.h>
#include <limits.h>

void GameAuthority_Init(GameAuthorityState *s)
{
    memset(s,0,sizeof(*s)); GameSettings_Default(&s->settings);
    s->selected_id=1; s->settings_revision=1; s->control.rng=0x13572468;
    GameAuthority_UpdateOdds(s);
}
uint32_t GameAuthority_TotalBet(const GameAuthorityState *s)
{
    uint8_t i; uint32_t total=0;
    for(i=0;i<6;i++) total+=s->bets[i];
    return total;
}
uint8_t GameAuthority_AddCoin(GameAuthorityState *s)
{
    uint32_t reserved=s->state==GAME_STATE_WAIT_RACE_OVER?s->committed_award:s->win_remaining_points;
    if((uint64_t)s->balance_points+reserved+s->settings.coin_credit_points>INT32_MAX) return 0;
    if(s->account.coin_in_value>INT64_MAX-1000000ULL || s->account.coin_in_count==UINT64_MAX) return 0;
    s->balance_points+=s->settings.coin_credit_points;
    s->account.coin_in_count++; s->account.coin_in_value+=s->settings.coin_credit_points;
    if(s->state==GAME_STATE_IDLE) s->state=GAME_STATE_BETTING;
    s->plan_dirty|=PLAN_DIRTY_COIN_IN;
    return 1;
}
uint8_t GameAuthority_SelectPrevious(GameAuthorityState *s)
{
    if(s->state!=GAME_STATE_BETTING) return 0;
    s->selected_id=s->selected_id<=1?6:s->selected_id-1; return 1;
}
uint8_t GameAuthority_SelectNext(GameAuthorityState *s)
{
    if(s->state!=GAME_STATE_BETTING) return 0;
    s->selected_id=s->selected_id>=6?1:s->selected_id+1; return 1;
}
uint8_t GameAuthority_PlaceBet(GameAuthorityState *s)
{
    uint8_t i; uint32_t next,odds,limit;
    if(s->state!=GAME_STATE_BETTING || s->selected_id<1 || s->selected_id>6 ||
        s->balance_points<s->settings.bet_points) return 0;
    i=s->selected_id-1; next=s->bets[i]+s->settings.bet_points;
    limit=i==0?s->settings.lion_max_bet:s->settings.other_max_bet;
    odds=s->odds[i]; if(i==0) { if(next>=500) odds=40; else if(next>=300) odds=32; }
    if(next>limit || GameAuthority_TotalBet(s)+s->settings.bet_points>s->settings.max_round_bet ||
        (uint64_t)next*odds>s->settings.max_round_award) return 0;
    s->balance_points-=s->settings.bet_points; s->bets[i]=next;
    GameAuthority_UpdateOdds(s); s->plan_dirty|=PLAN_DIRTY_BET; return 1;
}
uint8_t GameAuthority_CanPrepareRace(const GameAuthorityState *s)
{ return s->state==GAME_STATE_BETTING && !s->fault_code && GameAuthority_TotalBet(s)>0; }
uint8_t GameAuthority_PrepareRace(GameAuthorityState *s,uint32_t now)
{
    uint32_t total;
    if(!GameAuthority_CanPrepareRace(s)) return 0;
    GameAuthority_UpdateOdds(s); WinnerControl_Evaluate(s);
    total=GameAuthority_TotalBet(s);
    if(!s->plan.valid || s->account.commitment>(uint64_t)INT64_MAX-s->plan.award ||
        s->account.bet_total>UINT64_MAX-total ||
        s->balance_points>INT32_MAX-s->plan.award) return 0;
    memcpy(s->locked_bets,s->bets,sizeof(s->bets)); memcpy(s->locked_odds,s->odds,sizeof(s->odds));
    s->winner_id=s->plan.winner; s->committed_award=s->plan.award;
    WinnerControl_Commit(s);
    if(++s->round_id==0) s->round_id=1;
    s->win_points=s->win_total_points=s->win_remaining_points=0;
    s->race_result_credited=0; s->state=GAME_STATE_WAIT_RACE_OVER;
    s->race_seq=GameAuthority_NextSequence(s);
    s->race_deadline_tick=now+s->settings.race_duration_ms+5000;
    return 1;
}
static uint8_t CreditResult(GameAuthorityState *s,uint32_t now)
{
    if(s->state!=GAME_STATE_WAIT_RACE_OVER || s->race_result_credited) return 0;
    s->win_points=s->win_total_points=s->win_remaining_points=s->committed_award;
    s->race_result_credited=1; s->state=GAME_STATE_WAIT_RESULT_OVER;
    s->result_seq=GameAuthority_NextSequence(s);
    s->result_deadline_tick=now+s->settings.result_duration_ms+2000; return 1;
}
uint8_t GameAuthority_HandleRaceOver(GameAuthorityState *s,uint32_t seq,uint32_t round,uint32_t now)
{ return seq==s->race_seq && round==s->round_id?CreditResult(s,now):0; }
uint8_t GameAuthority_HandleRaceTimeout(GameAuthorityState *s,uint32_t now)
{ return (int32_t)(now-s->race_deadline_tick)>=0?CreditResult(s,now):0; }
static uint8_t ResetRound(GameAuthorityState *s)
{
    s->last_win_points=s->win_points;
    memset(s->bets,0,sizeof(s->bets)); memset(s->locked_bets,0,sizeof(s->locked_bets));
    s->selected_id=1; s->winner_id=0; s->win_points=0; s->committed_award=0;
    s->race_seq=s->result_seq=s->race_deadline_tick=s->result_deadline_tick=0;
    s->race_result_credited=0;
    s->state=s->balance_points?GAME_STATE_BETTING:GAME_STATE_IDLE;
    GameAuthority_UpdateOdds(s); return 1;
}
static uint8_t ResultDone(GameAuthorityState *s)
{
    if(s->state!=GAME_STATE_WAIT_RESULT_OVER) return 0;
    if(s->win_remaining_points) { s->state=GAME_STATE_WIN_WAIT_START; return 1; }
    return ResetRound(s);
}
uint8_t GameAuthority_HandleResultOver(GameAuthorityState *s,uint32_t seq,uint32_t round)
{ return seq==s->result_seq && round==s->round_id?ResultDone(s):0; }
uint8_t GameAuthority_HandleResultTimeout(GameAuthorityState *s,uint32_t now)
{ return (int32_t)(now-s->result_deadline_tick)>=0?ResultDone(s):0; }
uint8_t GameAuthority_StartWinCollect(GameAuthorityState *s,uint32_t now)
{
    if(s->state!=GAME_STATE_WIN_WAIT_START || !s->win_remaining_points) return 0;
    s->state=GAME_STATE_WIN_COLLECTING; s->collect_seq=GameAuthority_NextSequence(s);
    s->collect_next_tick=now+1000; s->collect_deadline_tick=now+(s->win_remaining_points/10+1)*1000;
    s->collect_last_delta=0; s->collect_fast_done=0; return 1;
}
uint8_t GameAuthority_PollWinCollect(GameAuthorityState *s,uint32_t now)
{
    uint32_t n,delta;
    if(s->state!=GAME_STATE_WIN_COLLECTING || !s->win_remaining_points || (int32_t)(now-s->collect_next_tick)<0) return 0;
    n=(now-s->collect_next_tick)/1000+1; delta=n*10;
    if(delta>s->win_remaining_points) delta=s->win_remaining_points;
    s->win_remaining_points-=delta; s->balance_points+=delta; s->collect_last_delta=delta;
    s->collect_seq=GameAuthority_NextSequence(s); s->collect_next_tick+=n*1000;
    if(!s->win_remaining_points) ResetRound(s);
    return 1;
}
uint8_t GameAuthority_FastFinishWinCollect(GameAuthorityState *s)
{
    if(s->state!=GAME_STATE_WIN_COLLECTING || !s->win_remaining_points) return 0;
    s->collect_last_delta=s->win_remaining_points; s->balance_points+=s->win_remaining_points;
    s->win_remaining_points=0; s->collect_fast_done=1; s->collect_seq=GameAuthority_NextSequence(s);
    return ResetRound(s);
}
uint8_t GameAuthority_ToggleSetting(GameAuthorityState *s)
{
    if((s->state==GAME_STATE_IDLE || s->state==GAME_STATE_BETTING) && !GameAuthority_TotalBet(s)) {
        s->setting_index=0; s->state=GAME_STATE_SETTING; return 1;
    }
    if(s->state==GAME_STATE_SETTING) {
        s->state=s->balance_points?GAME_STATE_BETTING:GAME_STATE_IDLE;
        GameAuthority_UpdateOdds(s); return 1;
    }
    return 0;
}
uint8_t GameAuthority_SettingPrevious(GameAuthorityState *s)
{ if(s->state!=GAME_STATE_SETTING) return 0; s->setting_index=(s->setting_index+GAME_AUTHORITY_SETTINGS_COUNT-1)%GAME_AUTHORITY_SETTINGS_COUNT; return 1; }
uint8_t GameAuthority_SettingNext(GameAuthorityState *s)
{ if(s->state!=GAME_STATE_SETTING) return 0; s->setting_index=(s->setting_index+1)%GAME_AUTHORITY_SETTINGS_COUNT; return 1; }
uint8_t GameAuthority_StartPayout(GameAuthorityState *s)
{
    uint32_t unit=s->settings.payout_unit_points;
    if((s->state!=GAME_STATE_IDLE && s->state!=GAME_STATE_BETTING) || GameAuthority_TotalBet(s) ||
        !unit || s->balance_points<unit || s->settings.payout_input==255 || s->settings.payout_output==255) return 0;
    s->pending_payout_coins=s->balance_points/unit; s->balance_points%=unit;
    s->payout_target=s->pending_payout_coins; s->payout_done=0; s->payout_unit_locked=unit;
    s->state=GAME_STATE_PAYOUT; return 1;
}
uint8_t GameAuthority_ConfirmPayout(GameAuthorityState *s,uint32_t n)
{
    if(s->state!=GAME_STATE_PAYOUT || !n || n>s->pending_payout_coins) return 0;
    s->pending_payout_coins-=n; s->payout_done+=n;
    s->account.payout_count+=n; s->account.payout_value+=(uint64_t)n*s->payout_unit_locked;
    s->plan_dirty|=PLAN_DIRTY_PAYOUT;
    if(!s->pending_payout_coins) GameAuthority_FinishPayout(s);
    return 1;
}
uint8_t GameAuthority_FinishPayout(GameAuthorityState *s)
{
    if(s->state!=GAME_STATE_PAYOUT || s->pending_payout_coins) return 0;
    s->state=s->balance_points?GAME_STATE_BETTING:GAME_STATE_IDLE; return 1;
}
uint8_t GameAuthority_ServiceReset(GameAuthorityState *s)
{
    if(s->fault_code==2 || s->fault_code==3) { s->fault_code=0; return 1; }
    return 0;
}
uint32_t GameAuthority_NextSequence(GameAuthorityState *s) { if(++s->seq==0) s->seq=1; return s->seq; }
const char *GameAuthority_StateName(GameState s)
{
    static const char *names[]={"IDLE","BETTING","RACE_PREPARE","WAIT_RACE_OVER","RESULT","WAIT_RESULT_OVER","WIN_WAIT_START","WIN_COLLECTING","SETTING","PAYOUT","FAULT"};
    return s<=GAME_STATE_FAULT?names[s]:"FAULT";
}
void GameAuthority_RestoreTimers(GameAuthorityState *s,uint32_t now)
{
    if(s->state==GAME_STATE_SETTING) s->state=s->balance_points?GAME_STATE_BETTING:GAME_STATE_IDLE;
    s->race_deadline_tick=now+s->settings.race_duration_ms+5000;
    s->result_deadline_tick=now+s->settings.result_duration_ms+2000;
    s->collect_next_tick=now+1000;
    s->collect_deadline_tick=now+(s->win_remaining_points/10+1)*1000;
    s->plan_dirty=127;
}
