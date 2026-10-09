#include "game_authority.h"
#include "settings_flash.h"
#include "machine_protocol.h"
#include "protocol_builder.h"
#include <stdio.h>
#include <assert.h>
#include <stddef.h>
#include <string.h>
#include <stdlib.h>
static unsigned char ee[8192];
static int fail_after=-1;
typedef struct {
    uint32_t magic,version,length,sequence;
    GameAuthorityState state;
    uint32_t crc,commit;
} LegacyRecordV3;
static uint32_t TestCrc(const void *p,uint32_t n)
{
    const uint8_t *b=p; uint32_t crc=0xffffffffU; uint8_t bit;
    while(n--) { crc^=*b++; for(bit=0;bit<8;bit++) crc=(crc&1U)?(crc>>1)^0xedb88320U:crc>>1; }
    return crc^0xffffffffU;
}
uint32_t BoardTimer_Millis(void) { return 0; }
int AT24CS64_ReadBuffer(uint16_t a,uint8_t *p,uint16_t n)
{ assert((unsigned)a+n<=sizeof(ee)); memcpy(p,ee+a,n); return 0; }
int AT24CS64_WriteBuffer(uint16_t a,const uint8_t *p,uint16_t n)
{
    assert((unsigned)a+n<=sizeof(ee));
    while(n--) { if(fail_after==0) return -1; if(fail_after>0) fail_after--; ee[a++]=*p++; } return 0;
}
static void Round(GameAuthorityState *s,uint32_t now)
{
    uint32_t award; uint64_t committed;
    assert(GameAuthority_PrepareRace(s,now)); award=s->committed_award; committed=s->account.commitment;
    assert(award==s->locked_bets[s->winner_id-1]*s->locked_odds[s->winner_id-1]);
    assert(GameAuthority_HandleRaceOver(s,s->race_seq,s->round_id,now+15000));
    assert(!GameAuthority_HandleRaceOver(s,s->race_seq,s->round_id,now+15000));
    assert(s->win_points==award && s->account.commitment==committed);
    assert(GameAuthority_HandleResultOver(s,s->result_seq,s->round_id));
    if(award) { assert(GameAuthority_StartWinCollect(s,now+18000)); assert(GameAuthority_FastFinishWinCollect(s)); }
}
int main(void)
{
    GameAuthorityState s,recovered,before; LegacyRecordV3 v3; MachineRequest req; char line[1025];
    unsigned i,n,mode,level,seed,big=0,small=0; uint8_t saved[8192]; FILE *f;
    double min_return=1000,max_return=0;
    memset(ee,255,sizeof(ee)); GameAuthority_Init(&s); assert(GameSettings_Validate(&s.settings));
    assert(RuntimeStorage_Load(&s));
    /* V3 used the wrong default coin input (IN3/index 2).  Loading it must
       rewrite only that field to the production IN4/index 3 and preserve
       all money/account state. */
    memset(ee,255,sizeof(ee)); memset(&v3,0,sizeof(v3)); GameAuthority_Init(&v3.state);
    v3.state.settings.coin_input=2; v3.state.account.coin_in_count=123;
    v3.state.account.coin_in_value=456; v3.magic=0x33524147UL; v3.version=3;
    v3.length=sizeof(GameAuthorityState); v3.sequence=7; v3.commit=0xC04D1732UL;
    v3.crc=TestCrc(&v3,offsetof(LegacyRecordV3,crc)); memcpy(ee,&v3,sizeof(v3));
    GameAuthority_Init(&recovered); assert(RuntimeStorage_Load(&recovered));
    assert(recovered.settings.coin_input==3 && recovered.account.coin_in_count==123);
    assert(recovered.account.coin_in_value==456);
    GameAuthority_Init(&s); assert(RuntimeStorage_Load(&s));
    assert(s.settings.coin_input==3 && s.account.coin_in_count==123 && s.account.coin_in_value==456);
    puts("PASS V3 IN3-to-IN4 migration preserves wallet and account state");
    /* Upgrade from the old factory EEPROM test must not be mistaken for a
       damaged financial record. */
    memset(ee,255,sizeof(ee));
    for(i=0;i<10;i++) ee[i]=(uint8_t)(0x10U+i);
    ee[0x1234]=0x55;
    GameAuthority_Init(&s); assert(RuntimeStorage_Load(&s));
    GameAuthority_Init(&recovered); assert(RuntimeStorage_Load(&recovered));
    /* Only the complete known signature is recoverable. */
    memset(ee,255,sizeof(ee)); ee[0]=0x10;
    GameAuthority_Init(&recovered); assert(!RuntimeStorage_Load(&recovered));
    memset(ee,255,sizeof(ee)); GameAuthority_Init(&s); assert(RuntimeStorage_Load(&s));
    for(i=0;i<50;i++) assert(GameAuthority_AddCoin(&s));
    assert(GameAuthority_PlaceBet(&s)); WinnerControl_Evaluate(&s); before=s;
    for(i=0;i<100;i++) WinnerControl_Evaluate(&s);
    assert(!memcmp(&before,&s,sizeof(s)));
    assert(GameAuthority_AddCoin(&s)); assert(s.plan_dirty&PLAN_DIRTY_COIN_IN);
    WinnerControl_Evaluate(&s); assert(s.account.coin_in_count==51);
    assert(RuntimeStorage_Save(&s)); memcpy(saved,ee,sizeof(ee)); before=s;
    assert(GameAuthority_PrepareRace(&s,0));
    /* Every possible torn byte returns either the old or the complete new transaction. */
    for(n=0;n<sizeof(s)+40;n++) {
        memcpy(ee,saved,sizeof(ee)); fail_after=-1; recovered=before; assert(RuntimeStorage_Load(&recovered));
        fail_after=(int)n; (void)RuntimeStorage_Save(&s); fail_after=-1;
        GameAuthority_Init(&recovered); assert(RuntimeStorage_Load(&recovered));
        assert(recovered.round_id==before.round_id || recovered.round_id==s.round_id);
        if(recovered.round_id==s.round_id) assert(recovered.account.commitment==s.account.commitment && recovered.winner_id==s.winner_id);
        else assert(recovered.account.commitment==before.account.commitment);
    }
    puts("PASS atomic EEPROM interruption sweep");
    memset(ee,0x5a,sizeof(ee)); assert(!RuntimeStorage_Load(&recovered));
    assert(!MachineProtocol_Parse("SETTINGS_APPLY:req=-1,base_rev=1,values=1",&req));
    assert(!MachineProtocol_Parse("SERVICE_AUTH:req=4294967296,password=1",&req));
    assert(!MachineProtocol_Parse("SERVICE_EXIT:req=1,extra=1",&req));
    assert(MachineProtocol_Parse("SERVICE_ENTER:req=7",&req));
    for(mode=0;mode<=6;mode++) for(level=1;level<=5;level++) for(seed=1;seed<=4;seed++) {
        GameAuthority_Init(&s); s.settings.multiplier_mode=mode; s.settings.control_level=level;
        s.control.token_valid=0; s.control.rng=seed*17321; memset(s.control.remaining,0,5); GameAuthority_UpdateOdds(&s);
        for(i=0;i<2000;i++) {
            /* All animals and zero-bet outcomes, with fresh input every round. */
            assert(GameAuthority_AddCoin(&s)); s.selected_id=(uint8_t)(1+(i+seed)%6);
            assert(GameAuthority_PlaceBet(&s)); WinnerControl_Evaluate(&s);
            if(s.plan.tier>=3) big++; if(s.plan.tier==1 || s.plan.paced) small++;
            Round(&s,i*20000);
        }
        assert(s.account.rounds==2000);
        assert(s.account.coin_in_count==2000);
        {
            double rate=(double)s.account.commitment*100/s.account.coin_in_value;
            if(rate<min_return) min_return=rate;
            if(rate>max_return) max_return=rate;
            assert(rate<=100);
        }
    }
    assert(big>0); assert(small>0); printf("PASS 280000 rounds, 7 modes x 5 levels x 4 seeds; big=%u small=%u\n",big,small);
    printf("Fresh-coin mixed-animal scenario return range %.2f%%..%.2f%% (not a universal guarantee)\n",min_return,max_return);
    GameAuthority_Init(&s); s.settings.multiplier_mode=4; GameAuthority_UpdateOdds(&s);
    for(i=0;i<100;i++) assert(GameAuthority_AddCoin(&s));
    s.selected_id=6; assert(GameAuthority_PlaceBet(&s)); WinnerControl_Evaluate(&s);
    assert(s.plan.award==0); before=s;
    s.account.payout_value=999999; WinnerControl_Evaluate(&s);
    assert(s.plan.winner==before.plan.winner && s.plan.award==before.plan.award);
    puts("PASS physical coin-in and coin-out are audit-only; unplayed credit creates no RTP budget");

    s.account.commitment=100000; s.account.bet_total=0; s.account.payout_value=0;
    s.control.pace_scaled=0; s.control.miss_streak=(uint8_t)(s.control.guarantee_threshold-1);
    WinnerControl_Evaluate(&s); assert(s.plan.award==30 && s.plan.tier==1 && s.plan.paced);
    Round(&s,0); assert(!s.control.pace_scaled && s.account.bet_total==10);
    s.selected_id=6; assert(GameAuthority_PlaceBet(&s)); Round(&s,20000);
    assert(s.control.pace_scaled==93000 && s.account.bet_total==20);
    n=(unsigned)s.control.pace_scaled; assert(GameAuthority_AddCoin(&s));
    assert(s.control.pace_scaled==n);
    puts("PASS persistent 3-5 round small-prize guarantee and isolated recovery budget use game turnover");

    GameAuthority_Init(&s); s.settings.multiplier_mode=4; s.settings.payout_unit_points=10;
    GameAuthority_UpdateOdds(&s); assert(GameAuthority_AddCoin(&s)); s.selected_id=6;
    assert(GameAuthority_PlaceBet(&s)); s.control.miss_streak=(uint8_t)(s.control.guarantee_threshold-1);
    assert(GameAuthority_PrepareRace(&s,0)); assert(s.committed_award==30 && s.account.commitment==30);
    assert(GameAuthority_HandleRaceOver(&s,s.race_seq,s.round_id,15000));
    assert(GameAuthority_HandleResultOver(&s,s.result_seq,s.round_id));
    assert(GameAuthority_StartWinCollect(&s,18000)); assert(GameAuthority_FastFinishWinCollect(&s));
    assert(s.account.commitment==30 && s.balance_points==30 && s.account.bet_total==10);
    s.selected_id=6; assert(GameAuthority_PlaceBet(&s)); Round(&s,20000);
    assert(s.account.coin_in_count==1 && s.account.bet_total==20 && s.balance_points==20);
    s.settings.payout_input=9; s.settings.payout_output=0;
    assert(GameAuthority_StartPayout(&s)); assert(GameAuthority_ConfirmPayout(&s,2));
    assert(s.account.commitment==30 && s.account.payout_value==20 && s.account.payout_count==2);
    puts("PASS award is committed once; recycled credit adds game turnover and later redemption adds only cash audit");

    s.account.payout_count=0; s.account.payout_value=0;
    s.settings.payout_unit_points=100; s.settings.payout_input=9; s.settings.payout_output=0; s.balance_points=350;
    assert(GameAuthority_StartPayout(&s)); assert(!GameAuthority_FinishPayout(&s));
    assert(GameAuthority_ConfirmPayout(&s,1)); assert(s.pending_payout_coins==2 && s.account.payout_count==1);
    assert(GameAuthority_ConfirmPayout(&s,2)); assert(!s.pending_payout_coins && s.balance_points==50);
    assert(!GameAuthority_ConfirmPayout(&s,1));
    puts("PASS payout requires real feedback and preserves remainder");
    GameAuthority_Init(&s);
    f=fopen("protocol-fixtures.txt","wb"); assert(f);
    assert(MachineProtocol_Settings(line,sizeof(line),&s,1)); fprintf(f,"%s\n",line);
    assert(MachineProtocol_Account(line,sizeof(line),&s)); fprintf(f,"%s\n",line);
    assert(ProtocolBuilder_FullState(line,sizeof(line),&s,1)); fprintf(f,"%s\n",line);
    for(i=0;i<37;i++) assert(GameSettings_Set(&s.settings,(uint8_t)i,GameAuthority_SettingMaximum((uint8_t)i)));
    assert(GameSettings_Validate(&s.settings));
    assert(MachineProtocol_Settings(line,sizeof(line),&s,1)); fprintf(f,"%s\n",line);
    GameAuthority_Init(&s); for(i=0;i<30;i++) assert(GameAuthority_AddCoin(&s));
    assert(GameAuthority_PlaceBet(&s)); assert(GameAuthority_PrepareRace(&s,0));
    assert(ProtocolBuilder_ShowRace(line,sizeof(line),&s)); fprintf(f,"%s\n",line);
    assert(GameAuthority_HandleRaceOver(&s,s.race_seq,s.round_id,15000));
    assert(ProtocolBuilder_ShowResult(line,sizeof(line),&s)); fprintf(f,"%s\n",line);
    for(i=0;i<37;i++) { if(i==31) continue; assert(GameSettings_Get(&s.settings,(uint8_t)i)<=GameAuthority_SettingMaximum((uint8_t)i)); }
    fclose(f); printf("state size=%u; PASS protocol fixtures\n",(unsigned)sizeof(s));
    return 0;
}
