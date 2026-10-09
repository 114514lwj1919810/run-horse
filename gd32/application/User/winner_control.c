#include "game_authority.h"
#include <limits.h>
#include <string.h>

/* Unity animal IDs: lion, elephant, gorilla, zebra, giraffe, camel. */
static const uint8_t tables[6][6]={
    {24,24,12,8,4,2},{24,15,12,10,4,2},{24,20,12,6,5,2},
    {24,24,8,6,3,3},{24,15,10,6,3,3},{24,12,12,6,3,3}
};
/* Finite bags: miss, small, medium, big, jackpot. All levels retain big prizes. */
static const uint8_t bags[5][5]={{4,20,8,5,3},{6,19,8,5,2},{8,18,8,4,2},{10,18,7,3,2},{12,18,6,3,1}};
static uint32_t Next(uint32_t *s)
{
    uint32_t x=*s?*s:0x6d2b79f5U;
    x^=x<<13; x^=x>>17; x^=x<<5; *s=x; return x;
}
static uint32_t Mix(uint32_t x)
{
    x^=x>>16; x*=0x7feb352dU; x^=x>>15; x*=0x846ca68bU; return x^(x>>16);
}
static uint64_t TargetFor(uint64_t game_use,uint32_t rtp_bp)
{
    return (game_use/10000U)*rtp_bp+(game_use%10000U)*rtp_bp/10000U;
}
static int64_t Headroom(uint64_t target,uint64_t used)
{
    uint64_t distance=target>=used?target-used:used-target;
    if(distance>(uint64_t)INT64_MAX) distance=(uint64_t)INT64_MAX;
    return target>=used?(int64_t)distance:-(int64_t)distance;
}
static uint8_t ChooseGuaranteeThreshold(GameAuthorityState *s)
{
    uint32_t strength=s->settings.small_pace_percent;
    uint8_t span=strength>=21U?3U:(strength>=11U?2U:1U);
    return (uint8_t)(5U-Next(&s->control.rng)%span);
}
uint8_t WinnerControl_Migrate(GameAuthorityState *s)
{
    uint8_t changed=0;
    if(!s) return 0;
    if(s->control.accounting_version!=1U) {
        /* Old firmware stored coin-driven small-prize credit in this slot. */
        s->control.pace_scaled=0;
        s->control.miss_streak=0;
        s->control.guarantee_threshold=ChooseGuaranteeThreshold(s);
        s->control.accounting_version=1;
        s->control.recovery_active=(uint8_t)(s->account.commitment>
            TargetFor(s->account.bet_total,s->settings.target_rtp_bp));
        changed=1;
    }
    if(s->control.guarantee_threshold<3U || s->control.guarantee_threshold>5U) {
        s->control.guarantee_threshold=ChooseGuaranteeThreshold(s); changed=1;
    }
    if(s->control.miss_streak>5U) { s->control.miss_streak=5U; changed=1; }
    if(s->control.recovery_active>1U) { s->control.recovery_active=0; changed=1; }
    return changed;
}
void WinnerControl_Begin(GameAuthorityState *s)
{
    uint8_t i; uint32_t total=0,pick;
    WinnerControl_Migrate(s);
    if(s->control.token_valid) return;
    for(i=0;i<5;i++) total+=s->control.remaining[i];
    if(!total) {
        memcpy(s->control.remaining,bags[s->settings.control_level-1],5);
        total=40;
    }
    pick=Next(&s->control.rng)%total;
    for(i=0;i<5;i++) {
        if(pick<s->control.remaining[i]) break;
        pick-=s->control.remaining[i];
    }
    s->control.token=i;
    s->control.tie_seed=Next(&s->control.rng);
    s->control.odds_mode=(uint8_t)(s->settings.multiplier_mode?s->settings.multiplier_mode:1+Next(&s->control.rng)%6);
    s->control.token_valid=1;
    s->plan_dirty|=PLAN_DIRTY_POOL|PLAN_DIRTY_ODDS;
}
void GameAuthority_UpdateOdds(GameAuthorityState *s)
{
    uint8_t i,mode;
    if(!s || (s->state!=GAME_STATE_IDLE && s->state!=GAME_STATE_BETTING && s->state!=GAME_STATE_SETTING)) return;
    WinnerControl_Begin(s);
    mode=s->control.odds_mode;
    if(s->settings.multiplier_mode) mode=(uint8_t)s->settings.multiplier_mode;
    for(i=0;i<6;i++) s->odds[i]=tables[mode-1][i];
    /* Existing lion promotion is part of the displayed quote. */
    if(s->bets[0]>=500) s->odds[0]=40;
    else if(s->bets[0]>=300) s->odds[0]=32;
    s->plan_dirty|=PLAN_DIRTY_ODDS;
}
static uint8_t Tier(uint32_t award,uint32_t total)
{
    if(!award) return 0;
    if((uint64_t)award<=(uint64_t)total*3) return 1;
    if((uint64_t)award<=(uint64_t)total*8) return 2;
    if((uint64_t)award<=(uint64_t)total*16) return 3;
    return 4;
}
void WinnerControl_Evaluate(GameAuthorityState *s)
{
    uint8_t i,wanted,best=0xff,tiers[6];
    uint8_t guarantee=0;
    uint32_t awards[6],total,min_any=UINT32_MAX;
    uint32_t best_tie=0;
    uint64_t used,normal_before,target,budget,best_score=UINT64_MAX;
    uint64_t projected_use,recovery_scaled;
    uint32_t recovery_rate;
    if(!s || (s->state!=GAME_STATE_BETTING && s->state!=GAME_STATE_IDLE)) return;
    memset(&s->plan,0,sizeof(s->plan)); s->plan_dirty=0;
    total=GameAuthority_TotalBet(s); if(!total) return;
    used=s->account.commitment;
    projected_use=s->account.bet_total>UINT64_MAX-total?UINT64_MAX:s->account.bet_total+total;
    normal_before=TargetFor(s->account.bet_total,s->settings.target_rtp_bp);
    target=TargetFor(projected_use,s->settings.target_rtp_bp);
    s->plan.headroom=Headroom(target,used);
    recovery_rate=s->settings.target_rtp_bp>s->settings.return_band_bp?
        s->settings.target_rtp_bp-s->settings.return_band_bp:0;
    if(used>normal_before) {
        recovery_scaled=(uint64_t)total*recovery_rate;
        if(UINT64_MAX-s->control.pace_scaled<recovery_scaled) recovery_scaled=UINT64_MAX;
        else recovery_scaled+=s->control.pace_scaled;
        budget=recovery_scaled/10000U;
    } else budget=target>=used?target-used:0;
    wanted=s->control.token;
    for(i=0;i<6;i++) {
        awards[i]=s->bets[i]*s->odds[i]; tiers[i]=Tier(awards[i],total);
        if(awards[i]<min_any) min_any=awards[i];
    }
    if(s->settings.target_rtp_bp==0) { budget=0; wanted=0; }
    guarantee=(uint8_t)(s->settings.target_rtp_bp && s->settings.small_pace_percent &&
        s->control.miss_streak+1U>=s->control.guarantee_threshold);
    if(guarantee) {
        uint32_t smallest=UINT32_MAX;
        for(i=0;i<6;i++) {
            uint32_t tie=Mix(s->control.tie_seed^((uint32_t)i*0x9e3779b9U));
            if(tiers[i]!=1U || awards[i]>s->settings.max_round_award) continue;
            if(awards[i]<smallest || (awards[i]==smallest && tie>best_tie)) {
                best=i; smallest=awards[i]; best_tie=tie;
            }
        }
        if(best!=0xff) {
            s->plan.winner=best+1; s->plan.award=awards[best]; s->plan.tier=1;
            s->plan.paced=1; s->plan.valid=1; return;
        }
    }
    /* A deferred big result gets priority only when a real payable candidate exists. */
    if(s->control.jackpot_debt && s->settings.target_rtp_bp) {
        for(i=0;i<6;i++) if(tiers[i]>=3 && awards[i]<=budget) { wanted=tiers[i]; break; }
    }
    for(i=0;i<6;i++) {
        uint64_t distance,score;
        uint32_t tie=Mix(s->control.tie_seed^((uint32_t)i*0x9e3779b9U));
        uint8_t funded=awards[i]<=budget;
        if(awards[i]>s->settings.max_round_award) continue;
        if(!funded && awards[i]!=min_any) continue;
        distance=awards[i]>budget?awards[i]-budget:budget-awards[i];
        score=(uint64_t)(tiers[i]>wanted?tiers[i]-wanted:wanted-tiers[i])*100000000ULL+distance;
        if(!funded) score+=100000000000ULL;
        if(score<best_score || (score==best_score && tie>best_tie)) {
            best=i; best_score=score; best_tie=tie;
        }
    }
    if(best==0xff) return;
    s->plan.winner=best+1; s->plan.award=awards[best]; s->plan.tier=tiers[best];
    s->plan.paced=0;
    s->plan.valid=1;
}
void WinnerControl_Commit(GameAuthorityState *s)
{
    uint32_t total=GameAuthority_TotalBet(s);
    uint32_t recovery_rate=s->settings.target_rtp_bp>s->settings.return_band_bp?
        s->settings.target_rtp_bp-s->settings.return_band_bp:0;
    uint64_t normal_before=TargetFor(s->account.bet_total,s->settings.target_rtp_bp);
    uint8_t was_recovery=(uint8_t)(s->account.commitment>normal_before);
    uint64_t earned=(uint64_t)total*recovery_rate;
    uint64_t cost=(uint64_t)s->plan.award*10000U;
    if(s->control.remaining[s->control.token]) s->control.remaining[s->control.token]--;
    if(s->plan.tier>=3) {
        if(s->control.jackpot_debt) s->control.jackpot_debt--;
        s->account.jackpot_count++;
    } else if(s->control.token>=3 && s->control.jackpot_debt<1000000) s->control.jackpot_debt++;
    if(was_recovery) {
        if(UINT64_MAX-s->control.pace_scaled<earned) s->control.pace_scaled=UINT64_MAX;
        else s->control.pace_scaled+=earned;
        s->control.pace_scaled=s->control.pace_scaled>cost?s->control.pace_scaled-cost:0;
    } else s->control.pace_scaled=0;
    s->account.commitment+=s->plan.award;
    s->account.bet_total+=total; s->account.rounds++;
    if(s->account.commitment>TargetFor(s->account.bet_total,s->settings.target_rtp_bp)) {
        if(!was_recovery) s->control.pace_scaled=0;
        s->control.recovery_active=1;
    } else {
        s->control.pace_scaled=0; s->control.recovery_active=0;
    }
    if(!s->settings.target_rtp_bp || !s->settings.small_pace_percent) {
        s->control.miss_streak=0;
    } else if(s->plan.award) {
        s->control.miss_streak=0;
        s->control.guarantee_threshold=ChooseGuaranteeThreshold(s);
    } else if(s->control.miss_streak<5U) s->control.miss_streak++;
    s->control.token_valid=0;
    s->plan_dirty|=PLAN_DIRTY_COMMITMENT|PLAN_DIRTY_POOL;
}
