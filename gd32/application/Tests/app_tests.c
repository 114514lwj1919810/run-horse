#include <assert.h>
#include <stdio.h>
#include <string.h>
#include "../App/app_main.c"

static uint8_t memory[8192];
static uint32_t clock_ms,coins,pulses;
static int storage_fails,motor,queued_key=-1;
static char sent_lines[32768];
uint32_t BoardTimer_Millis(void) { return clock_ms; }
void BoardTimer_Init(void) {}
void InputService_Init(void) {}
void InputService_Poll(void) {}
uint8_t InputService_GetKeyPress(BoardKey *key)
{ if(queued_key<0) return 0; *key=(BoardKey)queued_key; queued_key=-1; return 1; }
void BoardIO_ConfigureCounters(uint8_t a,uint8_t b,uint8_t c) { (void)a;(void)b;(void)c; }
void BoardIO_WriteOutput(uint8_t index,uint8_t active) { (void)index; motor=active; }
void BoardIO_NotifyCommunicationActivity(void) {}
uint32_t BoardIO_TakeCoinCount(void) { uint32_t n=coins;coins=0;return n; }
uint32_t BoardIO_TakePayoutCount(void) { uint32_t n=pulses;pulses=0;return n; }
bool SerialPort_Init(uint32_t uart) { return uart==USART2; }
void SerialPort_Poll(void) {}
bool SerialPort_GetLine(char *out,size_t size) { (void)out;(void)size;return false; }
bool SerialPort_SendLine(const char *line)
{ assert(strlen(sent_lines)+strlen(line)+2<sizeof(sent_lines)); strcat(sent_lines,line);strcat(sent_lines,"\n");return true; }
int AT24CS64_ReadBuffer(uint16_t a,uint8_t *out,uint16_t n) { memcpy(out,memory+a,n);return 0; }
int AT24CS64_WriteBuffer(uint16_t a,const uint8_t *in,uint16_t n)
{ if(storage_fails)return -1; memcpy(memory+a,in,n);return 0; }
static void ClearTx(void) { sent_lines[0]=0; }
int main(void)
{
    GameAuthorityState restored; MachineRequest request; uint32_t id,round,demo_seq,demo_round; uint64_t award;
    memset(memory,255,sizeof(memory)); AppMain_Init(); assert(storage_ok && loaded && !game.fault_code);
    SerialLine("UNITY_READY:app=AnimalRace,version=1.2.0,protocol=3.2"); assert(ready && full_wait);
    snprintf(tx,sizeof(tx),"ACK:seq=%lu,status=OK",(unsigned long)full_seq); SerialLine(tx); assert(!full_wait);
    assert(AppMain_GetStandbyAnimationState()==STANDBY_ANIMATION_RACE);
    assert(AppMain_GetStandbyWinner()>=1 && AppMain_GetStandbyWinner()<=6);
    assert(strstr(sent_lines,"SHOW_RACE:") && strstr(sent_lines,"demo=1"));
    assert(game.state==GAME_STATE_IDLE && game.balance_points==0 && GameAuthority_TotalBet(&game)==0);
    demo_seq=standby_seq; demo_round=standby_round; ClearTx();
    SerialLine("UNITY_READY:app=AnimalRace,version=1.2.0,protocol=3.2");
    assert(full_wait && strstr(sent_lines,"F:") && strstr(sent_lines,"SHOW_RACE:") && strstr(sent_lines,"demo=1"));
    assert(standby_seq==demo_seq && standby_round==demo_round);
    snprintf(tx,sizeof(tx),"ACK:seq=%lu,status=OK",(unsigned long)full_seq); SerialLine(tx); assert(!full_wait);
    ClearTx();
    snprintf(tx,sizeof(tx),"DEMO_RACE_OVER:seq=%lu,round=%lu",
        (unsigned long)demo_seq,(unsigned long)demo_round); SerialLine(tx);
    assert(AppMain_GetStandbyAnimationState()==STANDBY_ANIMATION_RESULT);
    assert(strstr(sent_lines,"SHOW_RESULT:") && strstr(sent_lines,"duration_ms=3000,demo=1"));
    assert(game.balance_points==0 && game.win_remaining_points==0 && game.account.coin_in_count==0 && game.account.payout_count==0);
    demo_seq=standby_seq; ClearTx();
    snprintf(tx,sizeof(tx),"DEMO_RESULT_OVER:seq=%lu,round=%lu",
        (unsigned long)demo_seq,(unsigned long)demo_round); SerialLine(tx);
    assert(AppMain_GetStandbyAnimationState()==STANDBY_ANIMATION_RACE && standby_round==demo_round+1U);
    assert(strstr(sent_lines,"SHOW_RACE:") && strstr(sent_lines,"demo=1"));
    assert(game.state==GAME_STATE_IDLE && game.balance_points==0 && game.account.commitment==0);
    puts("PASS standby race/result loop is isolated from money, payout and paid-round state");
    coins=4; queued_key=BOARD_KEY_DOWN; ClearTx(); AppMain_Poll();
    assert(AppMain_GetStandbyAnimationState()==STANDBY_ANIMATION_OFF);
    assert(game.account.coin_in_count==4 && game.balance_points==30 && GameAuthority_TotalBet(&game)==10);
    id=game.control.tie_seed; round=game.round_id;
    storage_fails=1; ClearTx(); Key(BOARD_KEY_GAME);
    assert(pending_save && game.round_id==round && !strstr(sent_lines,"SHOW_RACE:"));
    assert(pending.round_id==round+1 && pending.control.tie_seed==id); award=pending.account.commitment;
    storage_fails=0; clock_ms+=1001; ClearTx(); AppMain_Poll();
    assert(!pending_save && game.round_id==round+1 && game.account.commitment==award);
    assert(strstr(sent_lines,"SHOW_RACE:"));
    GameAuthority_Init(&restored); assert(RuntimeStorage_Load(&restored));
    assert(restored.winner_id==game.winner_id && restored.committed_award==game.committed_award);
    printf("PASS app START persists locked winner/account before transmitting\n");

    GameAuthority_Init(&game); game.state=GAME_STATE_SETTING; candidate=game; assert(Commit());
    memset(&request,0,sizeof(request)); request.type=MC_SETTINGS_APPLY; request.req=123; request.base=game.settings_revision;
    for(id=0;id<37;id++) request.values[id]=GameSettings_Get(&game.settings,(uint8_t)id);
    request.values[31]=0; request.values[6]=9200; ClearTx(); Control(&request);
    assert(game.settings.target_rtp_bp==9200 && game.settings_revision==2 && strstr(sent_lines,"status=OK"));
    ClearTx(); Control(&request); assert(game.settings_revision==2 && strstr(sent_lines,"status=OK"));
    request.req++; ClearTx(); Control(&request); assert(strstr(sent_lines,"CONFLICT") && game.settings_revision==2);
    request.base=2; request.values[14]=12; ClearTx(); Control(&request);
    assert(strstr(sent_lines,"AUTH_REQUIRED") && game.settings.device_id==1);
    printf("PASS versioned settings, duplicate saves, conflicts and system authentication\n");

    GameAuthority_Init(&game); game.state=GAME_STATE_SETTING; storage_ok=loaded=ready=1;
    pending_save=0; standby_state=STANDBY_ANIMATION_OFF;
    ClearTx(); Key(BOARD_KEY_UP); assert(strstr(sent_lines,"MENU_KEY:key=UP"));
    ClearTx(); Key(BOARD_KEY_DOWN); assert(strstr(sent_lines,"MENU_KEY:key=DOWN"));
    ClearTx(); Key(BOARD_KEY_LEFT); assert(strstr(sent_lines,"MENU_KEY:key=LEFT"));
    ClearTx(); Key(BOARD_KEY_RIGHT); assert(strstr(sent_lines,"MENU_KEY:key=RIGHT"));
    ClearTx(); Key(BOARD_KEY_GAME); assert(strstr(sent_lines,"MENU_KEY:key=CONFIRM"));
    ClearTx(); Key(BOARD_KEY_SETTING); assert(strstr(sent_lines,"MENU_KEY:key=BACK"));
    puts("PASS cabinet directional/game/setting keys map one-to-one in Unity settings");

    GameAuthority_Init(&game); candidate=game; storage_ok=loaded=ready=1; pending_save=0;
    standby_state=STANDBY_ANIMATION_RACE; ClearTx(); Key(BOARD_KEY_SETTING);
    assert(standby_state==STANDBY_ANIMATION_OFF && game.state==GAME_STATE_SETTING);
    assert(strstr(sent_lines,"SERVICE_STATE:open=1"));
    puts("PASS setting key exits standby and opens Unity settings in the same press");

    GameAuthority_Init(&game); game.balance_points=350; game.state=GAME_STATE_BETTING;
    game.settings.payout_input=9; game.settings.payout_output=0; candidate=game; assert(Commit());
    ClearTx(); Key(BOARD_KEY_EXCHANGE); AppMain_Poll(); assert(motor && game.pending_payout_coins==3);
    clock_ms+=5001; ClearTx(); AppMain_Poll(); assert(!motor && game.fault_code==2 && game.account.payout_count==0);
    Key(BOARD_KEY_SERVICE_RESET); AppMain_Poll(); assert(motor && !game.fault_code);
    pulses=1; ClearTx(); AppMain_Poll(); assert(game.account.payout_count==1 && game.pending_payout_coins==2);
    storage_fails=1; pulses=1; ClearTx(); AppMain_Poll(); assert(!motor && pending_save && game.account.payout_count==1);
    storage_fails=0; clock_ms+=1001; ClearTx(); AppMain_Poll(); assert(!pending_save && game.account.payout_count==2);
    pulses=1; ClearTx(); AppMain_Poll(); assert(game.account.payout_count==3 && game.balance_points==50 && !motor);
    puts("PASS real payout pulses, timeout, storage failure stops motor, recovery exactly once");
    return 0;
}
