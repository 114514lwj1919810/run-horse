#include "boot_secure.h"

#include "boot_contract.h"
#include "boot_signing_public_key.h"
#include "gd32f4xx.h"
#include "monocypher-ed25519.h"
#include <stddef.h>
#include <string.h>

#define BOOT_UID_ADDRESS 0x1FFF7A10UL

typedef char BootIdentitySizeMustBe128[(sizeof(BootDeviceIdentity)==128U)?1:-1];
typedef char BootManifestSizeMustBe188[(sizeof(BootSecurePackage)==188U)?1:-1];
typedef char BootRecipientHeaderSizeMustBe24[(sizeof(BootRecipientHeader)==24U)?1:-1];
typedef char BootRecipientRecordSizeMustBe76[(sizeof(BootRecipientRecord)==76U)?1:-1];

static void put_u32_le(uint8_t *p,uint32_t value)
{
    p[0]=(uint8_t)value;p[1]=(uint8_t)(value>>8U);
    p[2]=(uint8_t)(value>>16U);p[3]=(uint8_t)(value>>24U);
}

void BootSecure_ReadChipUid(uint8_t uid[BOOT_DEVICE_UID_SIZE])
{
    memcpy(uid,(const void *)BOOT_UID_ADDRESS,BOOT_DEVICE_UID_SIZE);
}

static int label_valid(const uint8_t label[8])
{
    uint32_t i;
    for(i=0U;i<3U;++i)if(label[i]<'0'||label[i]>'9')return 0;
    if(label[3]!='-')return 0;
    for(i=4U;i<7U;++i)if(label[i]<'0'||label[i]>'9')return 0;
    return label[7U]==0U;
}

static int identity_fields_valid(const BootDeviceIdentity *identity)
{
    uint8_t calculated[32];int valid;
    if(identity->magic!=BOOT_DEVICE_IDENTITY_MAGIC || identity->version!=1U ||
       identity->model!=BOOT_MODEL || identity->flags!=1U || !label_valid(identity->label) ||
       memcmp(identity->uid,(const void *)BOOT_UID_ADDRESS,12U)!=0)return 0;
    BootCrypto_HmacSha256(identity->device_key,32U,(const uint8_t *)identity,
                          (uint32_t)offsetof(BootDeviceIdentity,hmac),calculated);
    valid=BootCrypto_Equal(calculated,identity->hmac,32U);
    memset(calculated,0,sizeof(calculated));return valid;
}

int BootSecure_LoadIdentity(BootDeviceIdentity *identity)
{
    memcpy(identity,(const void *)BOOT_DEVICE_IDENTITY_ADDR,sizeof(*identity));
    return identity_fields_valid(identity);
}

int BootSecure_ProvisionIdentity(const BootDeviceIdentity *identity)
{
    const uint32_t *stored=(const uint32_t *)BOOT_DEVICE_IDENTITY_ADDR;
    const uint32_t *source=(const uint32_t *)identity;
    uint32_t i,primask;fmc_state_enum status;
    for(i=0U;i<sizeof(*identity)/4U;++i)if(stored[i]!=0xFFFFFFFFUL)return 0;
    if(!identity_fields_valid(identity))return 0;
    primask=__get_PRIMASK();__disable_irq();fwdgt_counter_reload();fmc_unlock();
    fmc_flag_clear(FMC_FLAG_END|FMC_FLAG_OPERR|FMC_FLAG_WPERR|FMC_FLAG_PGMERR|FMC_FLAG_PGSERR);
    status=fmc_sector_erase(CTL_SN(3U));
    if(status==FMC_READY)for(i=0U;i<sizeof(*identity)/4U;++i)
    {
        fwdgt_counter_reload();status=fmc_word_program(BOOT_DEVICE_IDENTITY_ADDR+i*4U,source[i]);
        if(status!=FMC_READY || stored[i]!=source[i])break;
    }
    fmc_lock();__DSB();__ISB();__set_PRIMASK(primask);
    return status==FMC_READY && memcmp((const void *)BOOT_DEVICE_IDENTITY_ADDR,identity,sizeof(*identity))==0;
}

int BootSecure_VerifyManifest(const BootSecurePackage *package)
{
    if(package->magic!=BOOT_SECURE_PACKAGE_MAGIC || package->format!=BOOT_SECURE_FORMAT ||
       package->model!=BOOT_MODEL || package->release==0U || package->recipient_count==0U ||
       package->size_a<8U || package->size_a>BOOT_APP_SIZE ||
       package->size_b<8U || package->size_b>BOOT_APP_SIZE)return 0;
    return crypto_ed25519_check(package->signature,g_boot_signing_public_key,
                                (const uint8_t *)package,
                                offsetof(BootSecurePackage,signature))==0;
}

int BootSecure_SignatureSelfTest(void)
{
    static const uint8_t public_key[32]={
        0xD7,0x5A,0x98,0x01,0x82,0xB1,0x0A,0xB7,0xD5,0x4B,0xFE,0xD3,0xC9,0x64,0x07,0x3A,
        0x0E,0xE1,0x72,0xF3,0xDA,0xA6,0x23,0x25,0xAF,0x02,0x1A,0x68,0xF7,0x07,0x51,0x1A};
    static const uint8_t signature[64]={
        0xE5,0x56,0x43,0x00,0xC3,0x60,0xAC,0x72,0x90,0x86,0xE2,0xCC,0x80,0x6E,0x82,0x8A,
        0x84,0x87,0x7F,0x1E,0xB8,0xE5,0xD9,0x74,0xD8,0x73,0xE0,0x65,0x22,0x49,0x01,0x55,
        0x5F,0xB8,0x82,0x15,0x90,0xA3,0x3B,0xAC,0xC6,0x1E,0x39,0x70,0x1C,0xF9,0xB4,0x6B,
        0xD2,0x5B,0xF5,0xF0,0x59,0x5B,0xBE,0x24,0x65,0x51,0x41,0x43,0x8E,0x7A,0x10,0x0B};
    static const uint8_t empty=0U;
    return crypto_ed25519_check(signature,public_key,&empty,0U)==0;
}

void BootSecure_RecipientLookup(const BootDeviceIdentity *identity,
                                uint32_t release,uint8_t lookup[16])
{
    static const uint8_t context[]="ANIMAL-RECIPIENT-v2";
    uint8_t message[sizeof(context)-1U+8U],digest[32];
    memcpy(message,context,sizeof(context)-1U);
    put_u32_le(message+sizeof(context)-1U,identity->model);
    put_u32_le(message+sizeof(context)-1U+4U,release);
    BootCrypto_HmacSha256(identity->device_key,32U,message,sizeof(message),digest);
    memcpy(lookup,digest,16U);memset(digest,0,sizeof(digest));memset(message,0,sizeof(message));
}

void BootSecure_DeriveKek(const BootDeviceIdentity *identity,uint8_t key[32])
{
    BootCrypto_DeriveKey(identity->device_key,identity->uid,identity->label,
                         identity->model,"KEK",key);
}

void BootSecure_BuildRecipientAad(const BootDeviceIdentity *identity,
                                  const BootSecurePackage *package,
                                  const uint8_t lookup[16],uint8_t aad[52])
{
    put_u32_le(aad,BOOT_RECIPIENT_FILE_MAGIC);put_u32_le(aad+4U,BOOT_SECURE_FORMAT);
    put_u32_le(aad+8U,identity->model);put_u32_le(aad+12U,package->release);
    memcpy(aad+16U,lookup,16U);memcpy(aad+32U,identity->uid,12U);
    memcpy(aad+44U,identity->label,8U);
}

void BootSecure_BuildImageAad(const BootSecurePackage *package,uint32_t slot,uint8_t aad[57])
{
    uint32_t size=(slot==0U)?package->size_a:package->size_b;
    uint32_t crc=(slot==0U)?package->crc_a:package->crc_b;
    put_u32_le(aad,package->magic);put_u32_le(aad+4U,package->format);
    put_u32_le(aad+8U,package->model);put_u32_le(aad+12U,package->release);
    aad[16U]=(uint8_t)slot;put_u32_le(aad+17U,size);put_u32_le(aad+21U,crc);
    memcpy(aad+25U,package->recipients_sha256,32U);
}
