#ifndef BOOT_SECURE_H
#define BOOT_SECURE_H

#include <stdint.h>
#include "boot_crypto.h"

#define BOOT_DEVICE_IDENTITY_ADDR   0x0800C000UL
#define BOOT_DEVICE_IDENTITY_MAGIC  0x3144494AUL
#define BOOT_SECURE_PACKAGE_MAGIC   0x32434553UL
#define BOOT_RECIPIENT_FILE_MAGIC   0x32564544UL
#define BOOT_SECURE_FORMAT          2UL
#define BOOT_DEVICE_LABEL_SIZE      8U
#define BOOT_DEVICE_UID_SIZE        12U
#define BOOT_SECURE_IMAGE_AAD_SIZE  57U
#define BOOT_RECIPIENT_AAD_SIZE     52U

#pragma pack(push,1)
typedef struct
{
    uint32_t magic;
    uint32_t version;
    uint32_t model;
    uint8_t uid[BOOT_DEVICE_UID_SIZE];
    uint8_t label[BOOT_DEVICE_LABEL_SIZE];
    uint8_t device_key[32];
    uint32_t flags;
    uint8_t reserved[28];
    uint8_t hmac[32];
} BootDeviceIdentity;

typedef struct
{
    uint32_t magic;
    uint32_t format;
    uint32_t model;
    uint32_t release;
    uint32_t recipient_count;
    uint32_t size_a;
    uint32_t crc_a;
    uint32_t size_b;
    uint32_t crc_b;
    uint8_t nonce_a[12];
    uint8_t nonce_b[12];
    uint8_t tag_a[16];
    uint8_t tag_b[16];
    uint8_t recipients_sha256[32];
    uint8_t signature[64];
} BootSecurePackage;

typedef struct
{
    uint32_t magic;
    uint32_t format;
    uint32_t model;
    uint32_t release;
    uint32_t count;
    uint32_t record_size;
} BootRecipientHeader;

typedef struct
{
    uint8_t lookup[16];
    uint8_t nonce[12];
    uint8_t wrapped_key[32];
    uint8_t tag[16];
} BootRecipientRecord;
#pragma pack(pop)

int BootSecure_LoadIdentity(BootDeviceIdentity *identity);
void BootSecure_ReadChipUid(uint8_t uid[BOOT_DEVICE_UID_SIZE]);
int BootSecure_ProvisionIdentity(const BootDeviceIdentity *identity);
int BootSecure_VerifyManifest(const BootSecurePackage *package);
int BootSecure_SignatureSelfTest(void);
void BootSecure_RecipientLookup(const BootDeviceIdentity *identity,
                                uint32_t release, uint8_t lookup[16]);
void BootSecure_DeriveKek(const BootDeviceIdentity *identity, uint8_t key[32]);
void BootSecure_BuildRecipientAad(const BootDeviceIdentity *identity,
                                  const BootSecurePackage *package,
                                  const uint8_t lookup[16],
                                  uint8_t aad[BOOT_RECIPIENT_AAD_SIZE]);
void BootSecure_BuildImageAad(const BootSecurePackage *package, uint32_t slot,
                              uint8_t aad[BOOT_SECURE_IMAGE_AAD_SIZE]);

#endif
