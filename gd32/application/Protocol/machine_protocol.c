#include "machine_protocol.h"
#include <string.h>
#include <stdio.h>
#include <stdint.h>

static uint8_t Number(const char **p,uint32_t *v)
{
    uint32_t n=0; const char *a=*p;
    if(*a<'0'||*a>'9') return 0;
    while(*a>='0'&&*a<='9') { uint32_t d=(uint32_t)(*a++-'0'); if(n>(UINT32_MAX-d)/10) return 0; n=n*10+d; }
    *p=a; *v=n; return 1;
}
static uint8_t Literal(const char **p,const char *text)
{ size_t n=strlen(text); if(strncmp(*p,text,n)) return 0; *p+=n; return 1; }
uint8_t MachineProtocol_Parse(const char *line,MachineRequest *r)
{
    const char *p=line; uint8_t i;
    memset(r,0,sizeof(*r));
    if(!strcmp(line,"SETTINGS_GET")) { r->type=MC_SETTINGS_GET; return 1; }
    if(!strcmp(line,"ACCOUNT_GET")) { r->type=MC_ACCOUNT_GET; return 1; }
    if(Literal(&p,"SETTINGS_APPLY:req=")) r->type=MC_SETTINGS_APPLY;
    else if(Literal(&p,"SERVICE_ENTER:req=")) r->type=MC_ENTER;
    else if(Literal(&p,"SERVICE_EXIT:req=")) r->type=MC_EXIT;
    else if(Literal(&p,"SERVICE_AUTH:req=")) r->type=MC_AUTH;
    else if(Literal(&p,"ACCOUNT_REPORT:req=")) r->type=MC_REPORT;
    else if(Literal(&p,"ACCOUNT_CLEAR:req=")) r->type=MC_CLEAR;
    else return 0;
    if(!Number(&p,&r->req) || !r->req) return 0;
    if(r->type==MC_AUTH) return Literal(&p,",password=") && Number(&p,&r->password) && !*p;
    if(r->type==MC_SETTINGS_APPLY || r->type==MC_REPORT || r->type==MC_CLEAR) {
        if(!Literal(&p,",base_rev=") || !Number(&p,&r->base)) return 0;
    }
    if(r->type==MC_SETTINGS_APPLY) {
        if(!Literal(&p,",values=")) return 0;
        for(i=0;i<GAME_AUTHORITY_SETTINGS_COUNT;i++) {
            if(i && !Literal(&p,"|")) return 0;
            if(!Number(&p,&r->values[i])) return 0;
        }
    }
    return !*p;
}
uint8_t MachineProtocol_Settings(char *out,size_t size,const GameAuthorityState *s,uint8_t storage)
{
    uint8_t i; int n=snprintf(out,size,"SETTINGS_SNAPSHOT:schema=1,rev=%lu,storage=%u,values=",(unsigned long)s->settings_revision,storage);
    if(n<=0 || (size_t)n>=size) return 0;
    for(i=0;i<GAME_AUTHORITY_SETTINGS_COUNT;i++) {
        int k=snprintf(out+n,size-(size_t)n,"%s%lu",i?"|":"",(unsigned long)(i==31?0:GameSettings_Get(&s->settings,i)));
        if(k<=0 || (size_t)k>=size-(size_t)n) return 0; n+=k;
    }
    return 1;
}
uint8_t MachineProtocol_Account(char *out,size_t size,const GameAuthorityState *s)
{
    const AccountData *a=&s->account;
    int n=snprintf(out,size,"A:v=1,d=%llu|%llu|%llu|%llu|%llu|%llu|%llu|%llu|%llu|%llu|%lu|%llu|%llu|%llu|%llu|%llu|%lu|%lu|%lu|%lu",
        (unsigned long long)a->coin_in_count,(unsigned long long)a->payout_count,
        (unsigned long long)a->coin_in_value,(unsigned long long)a->payout_value,(unsigned long long)a->commitment,
        (unsigned long long)a->bet_total,(unsigned long long)a->rounds,(unsigned long long)a->jackpot_count,
        (unsigned long long)a->ticket_count,(unsigned long long)a->runtime_seconds,(unsigned long)a->report_count,
        (unsigned long long)a->report_coin_in,(unsigned long long)a->report_coin_out,
        (unsigned long long)a->report_in_value,(unsigned long long)a->report_out_value,
        (unsigned long long)s->control.pace_scaled,(unsigned long)s->control.jackpot_debt,
        (unsigned long)s->balance_points,(unsigned long)s->win_remaining_points,(unsigned long)s->pending_payout_coins);
    return n>0 && (size_t)n<size;
}
