#include "settings_flash.h"
#include "at24cs64.h"
#include <stddef.h>
#include <string.h>
#include <limits.h>

/* One atomic record owns settings, wallet, accounts, bag and round/payout journals.
 * 0x0000..0x1DFF: ten 768-byte slots. Legacy V2 settings remain at 0x1F00. */
#define SLOT_SIZE 768U
#define SLOT_COUNT 10U
#define MAGIC 0x33524147UL
#define COMMIT 0xC04D1732UL
typedef struct {
    uint32_t magic,version,length,sequence;
    GameAuthorityState state;
    uint32_t crc,commit;
} Record;
typedef char FitsSlot[(sizeof(Record)<=SLOT_SIZE)?1:-1];
static uint32_t sequence;
static uint8_t next_slot;
static Record record,verify;
#define LEGACY_TEST_SIGNATURE_MASK 0x07FFU

/* Early factory-test firmware wrote 0x10..0x19 at 0x0000..0x0009 and
 * 0x55 at 0x1234.  Those bytes predate the V3 runtime slots.  Recognise
 * only the complete, exact signature; any partial or additional dirty data
 * remains a storage error so an account can never be silently reset. */
static uint8_t LegacyTestByte(uint32_t address,uint8_t value,uint16_t *mask)
{
    if(address<10U && value==(uint8_t)(0x10U+address)) {
        *mask|=(uint16_t)(1U<<address); return 1;
    }
    if(address==0x1234U && value==0x55U) {
        *mask|=(uint16_t)(1U<<10); return 1;
    }
    return 0;
}
static uint32_t Crc(const void *p,uint32_t n)
{
    const uint8_t *b=p; uint32_t crc=0xffffffff; uint8_t bit;
    while(n--) { crc^=*b++; for(bit=0;bit<8;bit++) crc=(crc&1)?(crc>>1)^0xedb88320:crc>>1; }
    return crc^0xffffffff;
}
static uint8_t Valid(const Record *r)
{
    return r->magic==MAGIC && (r->version==3 || r->version==4) && r->length==sizeof(GameAuthorityState) &&
        r->commit==COMMIT && r->crc==Crc(r,offsetof(Record,crc)) &&
        GameSettings_Validate(&r->state.settings) && r->state.state<=GAME_STATE_FAULT &&
        r->state.selected_id>=1 && r->state.selected_id<=6 &&
        r->state.winner_id<=6 && r->state.control.token<5 &&
        r->state.control.odds_mode>=1 && r->state.control.odds_mode<=6 &&
        r->state.control.token_valid<=1 && r->state.settings_revision!=0 &&
        (uint64_t)r->state.balance_points+r->state.win_remaining_points<=INT32_MAX &&
        r->state.payout_done<=r->state.payout_target &&
        r->state.pending_payout_coins==r->state.payout_target-r->state.payout_done &&
        r->state.account.coin_in_value<=INT64_MAX && r->state.account.commitment<=INT64_MAX;
}
uint8_t RuntimeStorage_Load(GameAuthorityState *s)
{
    uint8_t i,found=0,dirty=0,migrated,value; uint16_t legacy_test_mask=0;
    uint32_t j,address,loaded_version=0U;
    sequence=0; next_slot=0;
    for(i=0;i<SLOT_COUNT;i++) {
        if(AT24CS64_ReadBuffer(i*SLOT_SIZE,(uint8_t*)&record,sizeof(record))!=0) return 0;
        if(Valid(&record)) {
            if(!found || (int32_t)(record.sequence-sequence)>0) {
                *s=record.state; sequence=record.sequence; next_slot=(i+1)%SLOT_COUNT; found=1;
                loaded_version=record.version;
            }
        } else {
            for(j=0;j<sizeof(record);j++) {
                value=((uint8_t*)&record)[j];
                if(value==0xff || value==0) continue;
                address=(uint32_t)i*SLOT_SIZE+j;
                if(!LegacyTestByte(address,value,&legacy_test_mask)) dirty=1;
            }
        }
    }
    if(found) {
        /* V3 shipped with the cabinet coin pulse mapped to IN3.  The
         * production harness uses IN4.  Preserve every account/state field
         * and migrate only the known old default. */
        if(loaded_version==3U && s->settings.coin_input==2U) {
            s->settings.coin_input=3U;
            migrated=1U;
        } else migrated=0U;
        migrated|=WinnerControl_Migrate(s);
        GameAuthority_UpdateOdds(s);
        return migrated?RuntimeStorage_Save(s):1;
    }
    /* Never silently zero a damaged existing account.  A partial factory
     * test signature is also treated as damage rather than guessed away. */
    if(legacy_test_mask!=0U && legacy_test_mask!=LEGACY_TEST_SIGNATURE_MASK) dirty=1;
    if(dirty) return 0;
    (void)SettingsFlash_Load(&s->settings);
    GameAuthority_UpdateOdds(s);
    return RuntimeStorage_Save(s);
}
uint8_t RuntimeStorage_Save(const GameAuthorityState *s)
{
    uint16_t addr=(uint16_t)(next_slot*SLOT_SIZE); uint32_t invalid=0;
    memset(&record,0,sizeof(record)); record.magic=MAGIC; record.version=4;
    record.length=sizeof(*s); record.sequence=sequence+1; record.state=*s;
    record.crc=Crc(&record,offsetof(Record,crc)); record.commit=COMMIT;
    if(AT24CS64_WriteBuffer(addr+offsetof(Record,commit),(const uint8_t*)&invalid,4)!=0) return 0;
    if(AT24CS64_WriteBuffer(addr,(const uint8_t*)&record,offsetof(Record,commit))!=0) return 0;
    if(AT24CS64_ReadBuffer(addr,(uint8_t*)&verify,offsetof(Record,commit))!=0 ||
        memcmp(&record,&verify,offsetof(Record,commit))) return 0;
    if(AT24CS64_WriteBuffer(addr+offsetof(Record,commit),(const uint8_t*)&record.commit,4)!=0) return 0;
    if(AT24CS64_ReadBuffer(addr,(uint8_t*)&verify,sizeof(verify))!=0 || !Valid(&verify)) return 0;
    sequence=record.sequence; next_slot=(next_slot+1)%SLOT_COUNT; return 1;
}
uint8_t SettingsFlash_Load(GameSettings *s)
{
    struct { uint32_t magic,version,length,values[16],crc; } old;
    GameSettings migrated=*s;
    uint8_t i;
    if(AT24CS64_ReadBuffer(0x1f00,(uint8_t*)&old,sizeof(old))!=0 || old.magic!=0x41525632 ||
        old.version!=2 || old.length!=64 || old.crc!=Crc(old.values,64)) return 0;
    for(i=0;i<16;i++) if(i!=6 && i!=13 && !GameSettings_Set(&migrated,i,old.values[i])) return 0;
    migrated.volume_percent=old.values[13]<=100?old.values[13]*16/100:9;
    if(!GameSettings_Validate(&migrated)) return 0;
    *s=migrated; return 1;
}
uint8_t SettingsFlash_Save(const GameSettings *s) { (void)s; return 0; /* Use the atomic runtime transaction. */ }
