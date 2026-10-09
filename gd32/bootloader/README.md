# AnimalRace GD32F407VE Secure Bootloader

This directory contains the TF-card secure IAP bootloader used by the
AnimalRace GD32F407VET6 board.

## Flash layout

| Region | Address | Size |
| --- | --- | --- |
| Bootloader | `0x08000000` | 48 KiB |
| Device identity | `0x0800C000` | 16 KiB |
| Metadata A | `0x08010000` | 64 KiB |
| Application A | `0x08020000` | 128 KiB |
| Application B | `0x08040000` | 128 KiB |
| Metadata B | `0x08060000` | 128 KiB |

The part ends at `0x08080000` (512 KiB). Do not reuse the original Joker
1 MiB layout for this target.

## Build targets

- `GD_BOOT_DEV`: development build, USART0/PA9 diagnostic output at 115200.
- `GD_BOOT_PROD`: production-style build without diagnostic output.

Build `Projects/MDK-ARM/GD_BOOT_SECURE.uvprojx` with ARM Compiler 5.06.

The bootloader reads only the signed MCU A/B update from the TF card. Android
APK installation is handled separately through the Android board USB port.
USART2 PB10/PB11 remains dedicated to normal game communication at 115200.

See the repository-level `README.md` for provisioning, packaging and TF-card
layout instructions.
