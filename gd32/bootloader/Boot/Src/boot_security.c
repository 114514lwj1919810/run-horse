#include "boot_security.h"

#if BOOT_OPTION_BYTE_PROTECTION_ENABLE != 0

#include "boot_contract.h"
#include "boot_secure.h"
#include "gd32f4xx_fmc.h"
#include <string.h>

/*
 * GD32F407 1 MiB Flash layout used by this product:
 *   Sector 0..4  : Bootloader             -> write protected
 *   Sector 5..6  : App A                  -> must remain writable
 *   Sector 7..8  : App B                  -> must remain writable
 *   Sector 9..10 : two BootRecord copies  -> must remain writable
 *   Sector 11    : UID/label/device key   -> write protected
 *
 * The global low security level prevents an external debugger/programmer from
 * reading the Flash. It does not stop this Bootloader from writing sectors
 * 5..10, so signed TF-card A/B updates and trial confirmation keep working.
 */
#define BOOT_PROTECTED_WP_MASK \
    (OB_WP_0 | OB_WP_1 | OB_WP_2 | OB_WP_3 | OB_WP_4 | OB_WP_11)
#define BOOT_UPDATABLE_WP_MASK \
    (OB_WP_5 | OB_WP_6 | OB_WP_7 | OB_WP_8 | OB_WP_9 | OB_WP_10)

static int BootSecurity_ProductReady(void)
{
    BootDeviceIdentity identity;
    BootRecord record;
    int identity_valid;
    int app_valid;

    /* Never lock an unprovisioned board or a board without a bootable App. */
    identity_valid = BootSecure_LoadIdentity(&identity);
    memset(&identity, 0, sizeof(identity));
    if (!identity_valid || !Boot_Load(&record))
    {
        return 0;
    }

    app_valid = Boot_AppValid(&record, 0U) || Boot_AppValid(&record, 1U);
    return app_valid;
}

#endif

int BootSecurity_ApplyPolicy(void)
{
#if BOOT_OPTION_BYTE_PROTECTION_ENABLE != 0
    uint16_t wp0;
    uint8_t spc;
    uint8_t protected_ready;
    uint8_t updatable_ready;

    if (!BootSecurity_ProductReady())
    {
        return 0;
    }

    /* DRP changes the meaning of WP bits. This product deliberately does not
     * use DRP, and refuses to rewrite an unexpected existing configuration. */
    if ((FMC_OBCTL0 & FMC_OBCTL0_DRP) != 0U)
    {
        return 0;
    }

    wp0 = ob_write_protection0_get();
    spc = (uint8_t)(FMC_OBCTL0 >> 8U);
    protected_ready = ((wp0 & (uint16_t)BOOT_PROTECTED_WP_MASK) == 0U) ? 1U : 0U;
    updatable_ready = ((wp0 & (uint16_t)BOOT_UPDATABLE_WP_MASK) ==
                       (uint16_t)BOOT_UPDATABLE_WP_MASK) ? 1U : 0U;

    if (spc == FMC_LSPC)
    {
        /* Already protected: accept only the exact sector policy we expect. */
        return ((protected_ready != 0U) && (updatable_ready != 0U)) ? 1 : 0;
    }
    if (spc != FMC_NSPC)
    {
        /* Never alter or downgrade an unexpected/high security level. */
        return 0;
    }

    ob_unlock();
    if (ob_write_protection_enable(BOOT_PROTECTED_WP_MASK) != SUCCESS)
    {
        ob_lock();
        return 0;
    }
    if (ob_write_protection_disable(BOOT_UPDATABLE_WP_MASK) != SUCCESS)
    {
        ob_lock();
        return 0;
    }
    ob_security_protection_config(FMC_LSPC);
    ob_start();
    if (fmc_ready_wait(FMC_TIMEOUT_COUNT) != FMC_READY)
    {
        ob_lock();
        return 0;
    }
    ob_lock();

    /* Option bytes take effect only after reset. On the next boot the function
     * verifies the loaded protection bits and then allows normal secure boot. */
    NVIC_SystemReset();
    for (;;)
    {
    }
#else
    /* GD_BOOT_DEV intentionally leaves all option-byte protection unchanged. */
    return 1;
#endif
}
