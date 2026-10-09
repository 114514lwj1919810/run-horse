/*
 * GD32 Joker secure A/B Bootloader main flow.
 *
 * Security order is deliberate:
 *   1. Read and authenticate this board's identity record.
 *   2. Verify the Ed25519 signature and this board's anonymous key envelope.
 *   3. Authenticate UPDATE.SEC, DEVICES.SEC and both encrypted images.
 *   4. Only then erase and program the inactive application slot.
 *   5. Start the new image as a trial; the application must confirm it.
 *
 * Option-byte read/write protection is intentionally disabled in both DEV and
 * PROD targets during validation.  See boot_security.c.
 */
#include "boot_config.h"
#include "boot_fat32.h"
#include "boot_secure.h"
#include "boot_security.h"
#include <string.h>

void boot_port_init(void);
void boot_jump(uint32_t base);

/* FAT32 reads one sector at a time.  Bootloader execution is single-threaded,
 * therefore one shared 512-byte buffer is sufficient. */
static uint8_t buffer[512];
static const char *const image_name[2] = {"APP_A.ENC", "APP_B.ENC"};

static void secure_zero(void *data, uint32_t size)
{
    volatile uint8_t *p = (volatile uint8_t *)data;
    while (size-- != 0U) {
        *p++ = 0U;
    }
}

#if defined(BOOT_TRACE)
static void log_uid(const uint8_t uid[BOOT_DEVICE_UID_SIZE])
{
    static const char hex[] = "0123456789ABCDEF";
    char line[40];
    uint32_t i;

    memcpy(line, "CHIP_UID=", 9U);
    for (i = 0U; i < BOOT_DEVICE_UID_SIZE; ++i) {
        line[9U + i * 2U] = hex[uid[i] >> 4];
        line[10U + i * 2U] = hex[uid[i] & 0x0FU];
    }
    line[9U + BOOT_DEVICE_UID_SIZE * 2U] = '\r';
    line[10U + BOOT_DEVICE_UID_SIZE * 2U] = '\n';
    line[11U + BOOT_DEVICE_UID_SIZE * 2U] = '\0';
    boot_log(line);
}
#endif

static int open_image(uint32_t slot, uint32_t size, boot_fat32_file_t *file)
{
    return boot_fat32_open_path("IAP", image_name[slot], file) == BOOT_FAT32_OK &&
           file->size == size;
}

/* Authenticate a complete ciphertext before any flash erase.  GCM tags cover
 * the ciphertext plus AAD containing product, UID, machine number, slot,
 * plaintext length and plaintext CRC. */
static int authenticate_image(uint32_t slot, const BootSecurePackage *package,
                              const uint8_t key[32])
{
    boot_fat32_file_t file;
    BootGcmAuth auth;
    BootGcmCtr ctr;
    uint8_t aad[BOOT_SECURE_IMAGE_AAD_SIZE];
    uint8_t actual_tag[BOOT_GCM_TAG_SIZE];
    uint8_t first_block[512];
    const uint8_t *nonce = slot == 0U ? package->nonce_a : package->nonce_b;
    const uint8_t *expected_tag = slot == 0U ? package->tag_a : package->tag_b;
    uint32_t size = slot == 0U ? package->size_a : package->size_b;
    uint32_t count;
    uint32_t total = 0U;
    uint32_t first_size = 0U;
    int ok = 0;

    if (!open_image(slot, size, &file)) {
        return 0;
    }

    BootSecure_BuildImageAad(package, slot, aad);
    BootCrypto_GcmAuthInit(&auth, key, nonce, aad, sizeof(aad));
    while (total < size) {
        if (boot_fat32_read(&file, buffer, sizeof(buffer), &count) != BOOT_FAT32_OK ||
            count == 0U || count > size - total) {
            goto done;
        }
        if (total == 0U) {
            first_size = count;
            memcpy(first_block, buffer, count);
        }
        BootCrypto_GcmAuthUpdate(&auth, buffer, count);
        total += count;
    }
    BootCrypto_GcmAuthFinish(&auth, actual_tag);
    if (!BootCrypto_Equal(actual_tag, expected_tag, sizeof(actual_tag))) {
        goto done;
    }

    /* A valid tag authenticates the whole file.  Decrypt only the first sector
     * now to reject an image linked for the wrong application slot. */
    BootCrypto_GcmCtrInit(&ctr, key, nonce);
    BootCrypto_GcmCtrCrypt(&ctr, first_block, first_block, first_size);
    if (!Boot_VectorValid(first_block, slot, size)) {
        goto done;
    }
    ok = 1;

done:
    secure_zero(&auth, sizeof(auth));
    secure_zero(&ctr, sizeof(ctr));
    secure_zero(aad, sizeof(aad));
    secure_zero(actual_tag, sizeof(actual_tag));
    secure_zero(first_block, sizeof(first_block));
    return ok;
}

/* DEVICES.SEC contains anonymous records.  The board computes its own
 * 16-byte lookup value, verifies the signed file hash, and decrypts only the
 * matching AES-GCM key envelope.  Neither UID nor 143-xxx appears in the file. */
static int load_content_key(const BootDeviceIdentity *identity,
                            const BootSecurePackage *package,
                            uint8_t content_key[32])
{
    boot_fat32_file_t file;
    BootRecipientHeader header;
    BootRecipientRecord record;
    BootSha256 sha;
    BootGcmAuth auth;
    BootGcmCtr ctr;
    uint8_t lookup[16],digest[32],kek[32];
    uint8_t aad[BOOT_RECIPIENT_AAD_SIZE],actual_tag[16];
    uint32_t count,index,matches=0U;
    int ok=0;

    BootSecure_RecipientLookup(identity,package->release,lookup);
    if(boot_fat32_open_path("IAP","DEVICES.SEC",&file)!=BOOT_FAT32_OK ||
       file.size<sizeof(header) ||
       boot_fat32_read(&file,(uint8_t *)&header,sizeof(header),&count)!=BOOT_FAT32_OK ||
       count!=sizeof(header))goto done;
    if(header.magic!=BOOT_RECIPIENT_FILE_MAGIC || header.format!=BOOT_SECURE_FORMAT ||
       header.model!=BOOT_MODEL || header.release!=package->release ||
       header.count!=package->recipient_count || header.record_size!=sizeof(record) ||
       file.size!=sizeof(header)+header.count*sizeof(record))goto done;

    BootCrypto_Sha256Init(&sha);
    BootCrypto_Sha256Update(&sha,(const uint8_t *)&header,sizeof(header));
    for(index=0U;index<header.count;++index)
    {
        if(boot_fat32_read(&file,(uint8_t *)&record,sizeof(record),&count)!=BOOT_FAT32_OK ||
           count!=sizeof(record))goto done;
        BootCrypto_Sha256Update(&sha,(const uint8_t *)&record,sizeof(record));
        if(BootCrypto_Equal(record.lookup,lookup,sizeof(lookup)))
        {
            ++matches;
            BootSecure_DeriveKek(identity,kek);
            BootSecure_BuildRecipientAad(identity,package,lookup,aad);
            BootCrypto_GcmAuthInit(&auth,kek,record.nonce,aad,sizeof(aad));
            BootCrypto_GcmAuthUpdate(&auth,record.wrapped_key,sizeof(record.wrapped_key));
            BootCrypto_GcmAuthFinish(&auth,actual_tag);
            if(!BootCrypto_Equal(actual_tag,record.tag,sizeof(actual_tag)))goto done;
            BootCrypto_GcmCtrInit(&ctr,kek,record.nonce);
            BootCrypto_GcmCtrCrypt(&ctr,record.wrapped_key,content_key,32U);
        }
    }
    BootCrypto_Sha256Final(&sha,digest);
    if(matches!=1U || !BootCrypto_Equal(digest,package->recipients_sha256,32U))goto done;
    ok=1;

done:
    secure_zero(&header,sizeof(header));secure_zero(&record,sizeof(record));
    secure_zero(&sha,sizeof(sha));secure_zero(&auth,sizeof(auth));secure_zero(&ctr,sizeof(ctr));
    secure_zero(lookup,sizeof(lookup));secure_zero(digest,sizeof(digest));secure_zero(kek,sizeof(kek));
    secure_zero(aad,sizeof(aad));secure_zero(actual_tag,sizeof(actual_tag));
    if(!ok)secure_zero(content_key,32U);
    return ok;
}

static int load_secure_manifest(BootSecurePackage *package,
                                BootDeviceIdentity *identity)
{
    boot_fat32_file_t file;
    uint32_t count;

    if (!BootSecure_LoadIdentity(identity)) {
        boot_log("IDENTITY_INVALID_OR_MISSING\r\n");
        /* A new board can be provisioned once from the TF card.  Existing or
         * corrupt identity data is never overwritten by this path. */
        /* The compact FAT32 reader supports DOS 8.3 names only.  Keep the
         * identity filename at exactly 8+3 characters; PROVISION.SEC has a
         * nine-character base name and therefore cannot be found here. */
        if (boot_fat32_open_path("IAP", "IDENTITY.SEC", &file) != BOOT_FAT32_OK ||
            file.size != sizeof(*identity) ||
            boot_fat32_read(&file, (uint8_t *)identity, sizeof(*identity), &count) != BOOT_FAT32_OK ||
            count != sizeof(*identity) || !BootSecure_ProvisionIdentity(identity) ||
            !BootSecure_LoadIdentity(identity)) {
            boot_log("NO_VALID_ONE_TIME_PROVISION\r\n");
            return 0;
        }
        boot_log("IDENTITY_PROVISIONED_ONCE\r\n");
    }
    if (boot_fat32_open_path("IAP", "UPDATE.SEC", &file) != BOOT_FAT32_OK) {
        boot_log("NO_SECURE_PACKAGE\r\n");
        return 0;
    }
    if (file.size != sizeof(*package) ||
        boot_fat32_read(&file, (uint8_t *)package, sizeof(*package), &count) != BOOT_FAT32_OK ||
        count != sizeof(*package) || !BootSecure_VerifyManifest(package)) {
        boot_log("SECURE_MANIFEST_REJECTED\r\n");
        return 0;
    }
    return 1;
}

static void try_update(BootRecord *record, int *have_record)
{
    boot_fat32_file_t file;
    BootDeviceIdentity identity;
    BootSecurePackage package;
    BootGcmCtr ctr;
    uint8_t content_key[32];
    const uint8_t *nonce;
    uint32_t slot;
    uint32_t size;
    uint32_t crc;
    uint32_t offset;
    uint32_t count;

    memset(&identity, 0, sizeof(identity));
    memset(&package, 0, sizeof(package));
    memset(content_key, 0, sizeof(content_key));

    if (boot_fat32_mount() != BOOT_FAT32_OK) {
        boot_log("TF_NOT_READY\r\n");
        goto done;
    }
    boot_log("TF_FAT32_OK\r\n");
    if (!load_secure_manifest(&package, &identity)) {
        goto done;
    }
    if (*have_record &&
        (record->phase == BOOT_TRIAL ||
         record->rejected_release == package.release ||
         package.release <= (record->release[0] > record->release[1] ?
                             record->release[0] : record->release[1]) ||
         (record->release[record->active] == package.release &&
          Boot_AppValid(record, record->active)))) {
        boot_log("PACKAGE_SKIPPED\r\n");
        goto done;
    }

    if (!load_content_key(&identity, &package, content_key)) {
        boot_log("DEVICE_NOT_AUTHORIZED_OR_LIST_INVALID\r\n");
        goto done;
    }
    /* Verify both files before changing metadata or flash. */
    if (!authenticate_image(0U, &package, content_key) ||
        !authenticate_image(1U, &package, content_key)) {
        boot_log("CIPHERTEXT_TAG_OR_VECTOR_FAIL\r\n");
        goto done;
    }
    boot_log("SECURE_PACKAGE_VERIFIED\r\n");

    slot = *have_record ? (1U - record->active) : 0U;
    size = slot == 0U ? package.size_a : package.size_b;
    crc = slot == 0U ? package.crc_a : package.crc_b;
    nonce = slot == 0U ? package.nonce_a : package.nonce_b;
    if (!open_image(slot, size, &file)) {
        goto done;
    }

    if (!*have_record) {
        memset(record, 0, sizeof(*record));
        record->active = 1U;
        record->previous = 1U;
        record->pending = BOOT_NONE;
    }
    record->pending = slot;
    record->phase = BOOT_WRITING;
    record->previous = record->active;
    record->attempts = 0U;
    record->size[slot] = size;
    record->crc[slot] = crc;
    record->release[slot] = package.release;
    if (!Boot_Save(record)) {
        boot_log("META_FAIL\r\n");
        goto done;
    }
    *have_record = 1;
    boot_log(slot == 0U ? "WRITE_A\r\n" : "WRITE_B\r\n");

    for (offset = 0U; offset < BOOT_APP_SIZE; offset += 0x20000UL) {
        if (!Boot_FlashErase(Boot_Base(slot) + offset)) {
            boot_log("ERASE_FAIL\r\n");
            goto done;
        }
    }

    BootCrypto_GcmCtrInit(&ctr, content_key, nonce);
    offset = 0U;
    while (offset < size) {
        if (boot_fat32_read(&file, buffer, sizeof(buffer), &count) != BOOT_FAT32_OK ||
            count == 0U || count > size - offset) {
            boot_log("READ_ENCRYPTED_IMAGE_FAIL\r\n");
            goto done;
        }
        BootCrypto_GcmCtrCrypt(&ctr, buffer, buffer, count);
        if (!Boot_FlashWrite(Boot_Base(slot) + offset, buffer, count)) {
            boot_log("WRITE_FAIL\r\n");
            goto done;
        }
        offset += count;
    }

    if (Boot_Crc((const void *)Boot_Base(slot), size) != crc ||
        !Boot_VectorValid((const void *)Boot_Base(slot), slot, size)) {
        boot_log("FLASH_PLAINTEXT_VERIFY_FAIL\r\n");
        goto done;
    }
    record->active = slot;
    record->phase = BOOT_TRIAL;
    record->rejected_release = 0U;
    if (!Boot_Save(record)) {
        boot_log("META_COMMIT_FAIL\r\n");
        goto done;
    }
    boot_log("UPDATE_OK_TRIAL\r\n");
    boot_led(3U);

done:
    secure_zero(&ctr, sizeof(ctr));
    secure_zero(content_key, sizeof(content_key));
    secure_zero(&identity, sizeof(identity));
    secure_zero(&package, sizeof(package));
}

/* First factory installation may contain an App-A image programmed by SWD
 * while both BootRecord sectors are erased. */
static int adopt_factory(BootRecord *record)
{
    uint32_t i;
    for (i = 0U; i < sizeof(*record) / 4U; ++i) {
        if (((const uint32_t *)BOOT_META_A)[i] != 0xFFFFFFFFUL ||
            ((const uint32_t *)BOOT_META_B)[i] != 0xFFFFFFFFUL) {
            return 0;
        }
    }
    if (!Boot_VectorValid((const void *)BOOT_APP_A, 0U, BOOT_APP_SIZE)) {
        return 0;
    }
    memset(record, 0, sizeof(*record));
    record->active = 0U;
    record->previous = 0U;
    record->pending = BOOT_NONE;
    record->size[0] = BOOT_APP_SIZE;
    record->crc[0] = Boot_Crc((const void *)BOOT_APP_A, BOOT_APP_SIZE);
    boot_log("FACTORY_ADOPT_A\r\n");
    return Boot_Save(record);
}

int main(void)
{
    BootRecord record;
    int have_record;
    uint32_t slot;
#if defined(BOOT_TRACE)
    uint8_t chip_uid[BOOT_DEVICE_UID_SIZE];
#endif

    boot_port_init();
#if defined(BOOT_BUILD_PROD)
    boot_log("\r\nANIMAL_BOOT_PROD_SECURE 115200 8N1\r\n");
#else
    boot_log("\r\nANIMAL_BOOT_DEV_SECURE 115200 8N1\r\n");
#endif
    if (!BootCrypto_SelfTest()) {
        boot_log("CRYPTO_SELFTEST_FAIL\r\n");
        goto halt;
    }
    if (!BootSecure_SignatureSelfTest()) {
        boot_log("ED25519_SELFTEST_FAIL\r\n");
        goto halt;
    }
    boot_log("CRYPTO_SELFTEST_OK\r\n");
    /* Apply production option bytes only after the code used to authenticate
     * device identity and firmware has passed its own startup self-tests. */
    if (!BootSecurity_ApplyPolicy()) {
        boot_log("SECURITY_POLICY_FAIL\r\n");
        goto halt;
    }
#if BOOT_OPTION_BYTE_PROTECTION_ENABLE != 0
    boot_log("OPTION_BYTES_PROTECTED\r\n");
#else
    boot_log("OPTION_BYTES_UNLOCKED_BY_DESIGN\r\n");
#endif
#if defined(BOOT_TRACE)
    /* Factory UID discovery must work even when no TF card is inserted. */
    BootSecure_ReadChipUid(chip_uid);
    log_uid(chip_uid);
    secure_zero(chip_uid, sizeof(chip_uid));
#endif

    have_record = Boot_Load(&record);
    if (!have_record) {
        have_record = adopt_factory(&record);
    }
    try_update(&record, &have_record);

    /* Reload committed state; never trust RAM after a failed metadata write. */
    have_record = Boot_Load(&record);
    if (have_record && record.phase == BOOT_TRIAL) {
        slot = record.pending;
        if (record.attempts >= BOOT_MAX_ATTEMPTS || !Boot_AppValid(&record, slot)) {
            record.rejected_release = record.release[slot];
            record.size[slot] = 0U;
            record.crc[slot] = 0U;
            record.active = record.previous;
            record.pending = BOOT_NONE;
            record.phase = 0U;
            record.attempts = 0U;
            boot_log("TRIAL_ROLLBACK\r\n");
        } else {
            ++record.attempts;
        }
        if (!Boot_Save(&record)) {
            have_record = 0;
        }
    }
    if (have_record) {
        slot = record.active;
        if (!Boot_AppValid(&record, slot)) {
            slot = 1U - slot;
        }
        if (Boot_AppValid(&record, slot)) {
            if (slot != record.active) {
                record.active = slot;
                record.pending = BOOT_NONE;
                record.phase = 0U;
                if (!Boot_Save(&record)) {
                    goto halt;
                }
            }
            boot_log(slot == 0U ? "BOOT_A\r\n" : "BOOT_B\r\n");
            boot_jump(Boot_Base(slot));
        }
    }

halt:
    boot_log("NO_BOOTABLE_APP_RESET_WITH_VALID_CARD\r\n");
    while (1) {
        boot_led(1U);
        HAL_Delay(800U);
    }
}
