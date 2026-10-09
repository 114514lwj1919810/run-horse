#ifndef BOOT_SECURITY_H
#define BOOT_SECURITY_H

/*
 * GD_BOOT_DEV sets this to 0 and never changes option bytes.
 * GD_BOOT_PROD sets it to 1. PROD first verifies that the per-device identity,
 * BootRecord and at least one App are valid, then enables the reviewed sector
 * write-protection policy plus low-level read protection.
 */
#ifndef BOOT_OPTION_BYTE_PROTECTION_ENABLE
#define BOOT_OPTION_BYTE_PROTECTION_ENABLE 0
#endif

int BootSecurity_ApplyPolicy(void);

#endif
