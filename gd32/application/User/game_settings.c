#include "game_authority.h"
#include <stddef.h>
#include <string.h>

typedef struct { const char *name; size_t offset; uint32_t min, max, step, value; } SettingDef;
#define S(field, low, high, step, value) {#field, offsetof(GameSettings, field), low, high, step, value}
static const SettingDef defs[GAME_AUTHORITY_SETTINGS_COUNT] = {
    S(race_duration_ms,5000,60000,500,15000), S(result_duration_ms,1000,10000,500,3000),
    S(coin_credit_points,1,100,1,10), S(bet_points,1,100,1,10),
    S(max_round_bet,100,60000,100,60000), S(payout_unit_points,1,1000,1,100),
    S(target_rtp_bp,0,10000,100,9500),
    S(base_odds[0],1,99,1,24), S(base_odds[1],1,99,1,24), S(base_odds[2],1,99,1,8),
    S(base_odds[3],1,99,1,6), S(base_odds[4],1,99,1,3), S(base_odds[5],1,99,1,3),
    S(volume_percent,0,16,1,9), S(device_id,1,99999999,1,1), S(serial_debug_enabled,0,1,1,1),
    S(control_level,1,5,1,3), S(small_pace_percent,0,30,1,30), S(multiplier_mode,0,6,1,0),
    S(lion_max_bet,100,10000,100,1000), S(other_max_bet,100,10000,100,1000),
    S(ticket_coins,1,99,1,1), S(machine_mode,0,1,1,0), S(branch_number,1,8,1,1),
    S(branch_volume,0,16,1,9), S(branch_type,0,1,1,0), S(branch_source,0,1,1,0),
    S(acceptor_type,0,1,1,0), S(acceptor_source,0,1,1,0),
    S(running_minutes,0,99900,100,0), S(system_formula,0,99999999,1,0),
    S(system_password,1,99999999,1,0), S(max_round_award,100,1000000,100,400000),
    /* Index 33: RTP reduction used only while committed awards exceed the normal target. */
    S(return_band_bp,0,1000,50,200),
    S(coin_input,0,27,1,3), S(payout_input,0,255,1,255), S(payout_output,0,255,1,255)
};
#undef S

uint32_t GameSettings_Get(const GameSettings *s, uint8_t i)
{
    uint32_t value=0;
    if (s && i<GAME_AUTHORITY_SETTINGS_COUNT) memcpy(&value,(const char*)s+defs[i].offset,4);
    return value;
}
uint8_t GameSettings_Set(GameSettings *s,uint8_t i,uint32_t v)
{
    if (!s || i>=GAME_AUTHORITY_SETTINGS_COUNT || v<defs[i].min || v>defs[i].max) return 0;
    memcpy((char*)s+defs[i].offset,&v,4);
    return 1;
}
void GameSettings_Default(GameSettings *s)
{
    uint8_t i;
    memset(s,0,sizeof(*s));
    for(i=0;i<GAME_AUTHORITY_SETTINGS_COUNT;i++) GameSettings_Set(s,i,defs[i].value);
}
static uint8_t InSet(uint32_t v,const uint8_t *set,uint8_t n)
{
    uint8_t i; for(i=0;i<n;i++) if(v==set[i]) return 1;
    return 0;
}
uint8_t GameSettings_Validate(const GameSettings *s)
{
    static const uint8_t coins[]={1,2,5,10,20,50,100}, bets[]={1,5,10,20,50,100};
    uint8_t i;
    if(!s) return 0;
    for(i=0;i<GAME_AUTHORITY_SETTINGS_COUNT;i++) {
        uint32_t v=GameSettings_Get(s,i);
        if(v<defs[i].min || v>defs[i].max) return 0;
    }
    return InSet(s->coin_credit_points,coins,7) && InSet(s->bet_points,bets,6) &&
        s->lion_max_bet%100==0 && s->other_max_bet%100==0 &&
        s->bet_points<=s->lion_max_bet && s->bet_points<=s->other_max_bet &&
        (s->payout_input<28 || s->payout_input==255) &&
        (s->payout_output<16 || s->payout_output==255) &&
        ((s->payout_input==255)==(s->payout_output==255)) &&
        s->payout_input!=s->coin_input;
}
uint32_t GameAuthority_SettingValue(const GameAuthorityState *s,uint8_t i) { return s?GameSettings_Get(&s->settings,i):0; }
const char *GameAuthority_SettingItemName(uint8_t i) { return i<GAME_AUTHORITY_SETTINGS_COUNT?defs[i].name:"unknown"; }
uint32_t GameAuthority_SettingMinimum(uint8_t i) { return i<GAME_AUTHORITY_SETTINGS_COUNT?defs[i].min:0; }
uint32_t GameAuthority_SettingMaximum(uint8_t i) { return i<GAME_AUTHORITY_SETTINGS_COUNT?defs[i].max:0; }
uint8_t GameAuthority_SettingAdjustUp(GameAuthorityState *s)
{
    uint8_t i=s->setting_index; uint32_t v=GameSettings_Get(&s->settings,i);
    if(s->state!=GAME_STATE_SETTING || i>=GAME_AUTHORITY_SETTINGS_COUNT) return 0;
    return GameSettings_Set(&s->settings,i,v>defs[i].max-defs[i].step?defs[i].max:v+defs[i].step);
}
uint8_t GameAuthority_SettingAdjustDown(GameAuthorityState *s)
{
    uint8_t i=s->setting_index; uint32_t v=GameSettings_Get(&s->settings,i);
    if(s->state!=GAME_STATE_SETTING || i>=GAME_AUTHORITY_SETTINGS_COUNT) return 0;
    return GameSettings_Set(&s->settings,i,v<defs[i].min+defs[i].step?defs[i].min:v-defs[i].step);
}
