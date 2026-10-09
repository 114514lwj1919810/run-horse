#ifndef BOOT_CRYPTO_H
#define BOOT_CRYPTO_H

#include <stdint.h>

#define BOOT_SHA256_SIZE       32U
#define BOOT_AES256_KEY_SIZE   32U
#define BOOT_GCM_NONCE_SIZE    12U
#define BOOT_GCM_TAG_SIZE      16U

typedef struct
{
    uint32_t state[8];
    uint64_t total_size;
    uint8_t block[64];
    uint32_t block_size;
} BootSha256;

typedef struct
{
    uint8_t round_key[240];
    uint8_t hash_subkey[16];
    uint8_t j0[16];
    uint8_t ghash[16];
    uint8_t partial[16];
    uint32_t partial_size;
    uint64_t aad_size;
    uint64_t cipher_size;
} BootGcmAuth;

typedef struct
{
    uint8_t round_key[240];
    uint8_t counter[16];
    uint8_t stream[16];
    uint32_t stream_used;
} BootGcmCtr;

void BootCrypto_HmacSha256(const uint8_t *key, uint32_t key_size,
                           const uint8_t *data, uint32_t data_size,
                           uint8_t output[BOOT_SHA256_SIZE]);
void BootCrypto_Sha256Init(BootSha256 *context);
void BootCrypto_Sha256Update(BootSha256 *context, const uint8_t *data,
                             uint32_t size);
void BootCrypto_Sha256Final(BootSha256 *context,
                            uint8_t output[BOOT_SHA256_SIZE]);
void BootCrypto_DeriveKey(const uint8_t device_key[32],
                          const uint8_t uid[12], const uint8_t label[8],
                          uint32_t model, const char purpose[3],
                          uint8_t output[32]);
void BootCrypto_GcmAuthInit(BootGcmAuth *context, const uint8_t key[32],
                            const uint8_t nonce[12],
                            const uint8_t *aad, uint32_t aad_size);
void BootCrypto_GcmAuthUpdate(BootGcmAuth *context,
                              const uint8_t *ciphertext, uint32_t size);
void BootCrypto_GcmAuthFinish(BootGcmAuth *context, uint8_t tag[16]);
void BootCrypto_GcmCtrInit(BootGcmCtr *context, const uint8_t key[32],
                           const uint8_t nonce[12]);
void BootCrypto_GcmCtrCrypt(BootGcmCtr *context, const uint8_t *input,
                            uint8_t *output, uint32_t size);
int BootCrypto_Equal(const uint8_t *left, const uint8_t *right, uint32_t size);
int BootCrypto_SelfTest(void);

#endif
