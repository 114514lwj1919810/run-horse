# AnimalRace TF firmware upgrade

This test project is based on the Joker secure A/B bootloader design, adapted
for GD32F407VET6 (512 KB code flash) and the AnimalRace board.

## Implemented behavior

- TF card uses SPI0 in the GD32 library: PA4 CS, PA5 SCK, PA6 MISO, PA7 MOSI.
- Android game communication uses USART2 on PB10/PB11 at a fixed 115200 baud.
- APK installation is handled independently by the Android board USB port;
  the MCU serial link is never used for APK transfer.
- MCU firmware keeps Joker-style signed/encrypted A/B update, trial boot and
  rollback.
- Development logging is on the Type-C/CH340 debug UART, USART0 TX PA9,
  115200 8N1.

## Flash layout

```text
0x08000000..0x0800BFFF  Bootloader (48 KB maximum)
0x0800C000..0x0800FFFF  Device identity, sector 3
0x08010000..0x0801FFFF  Metadata copy A, sector 4
0x08020000..0x0803FFFF  Application A, sector 5 (128 KB)
0x08040000..0x0805FFFF  Application B, sector 6 (128 KB)
0x08060000..0x0807FFFF  Metadata copy B, sector 7
```

Do not use the original Joker 1 MB addresses on a GD32F407VE.

## Build

Bootloader project:

```text
bootloader/Projects/MDK-ARM/GD_BOOT_SECURE.uvprojx
```

`GD_BOOT_DEV` keeps option-byte protection disabled and prints diagnostics.
The included signing key is a development test key. Never reuse it for
production machines.

AnimalRace application IAP projects:

```text
E:/game-pro/GD32/GD32F407VET6/GD32F407VET6/MDK-ARM/Project_IAP_A.uvprojx
E:/game-pro/GD32/GD32F407VET6/GD32F407VET6/MDK-ARM/Project_IAP_B.uvprojx
```

They use ARM Compiler 6.12 and link at `0x08020000` and `0x08040000`.
The ordinary `Project.uvprojx` remains a standalone `0x08000000` build and
must not be used as an A/B update image. Verified A/B binaries are copied to:

```text
AppOutput/IAP_A/IAP_A.bin
AppOutput/IAP_B/IAP_B.bin
```

Android AAR:

```powershell
powershell -ExecutionPolicy Bypass -File android-plugin/build_plugin.ps1
```

Copy the resulting `android-plugin/build/serialplugin-release.aar` to the
Unity project's `Assets/Plugins/Android` directory. The matching
`AndroidSerialService.cs` is under `unity/`.

Rebuild the Unity project after replacing the AAR. `RK3128Build` writes the
APK to:

```text
E:/game-pro/apk-test/AnimalRace_RK3128_V32_Redrawn.apk
```

The verified build is also copied to the convenient USB-install path:

```text
E:/unity/project/Unity_AnimalRace/Builds/AnimalRace_GD32_V32.apk
```

Its SHA-256 is
`7951E104CB1691AAA15BBD2FA752B2ED3B3D5BF293260C8D66AE033D8D5654B9`.

## Register a board

For a blank board, `Factory/AnimalRace_FACTORY_DEV.bin` is ready for an SWD
programmer at address `0x08000000`. It contains `GD_BOOT_DEV` plus Application
A; the identity and metadata regions remain erased. Recreate it after an App A
change with `make_factory_image.ps1`.

1. Flash the factory image and read `CHIP_UID` from the CH340 debug port.
2. Register it, for example:

```powershell
python tools/register_device.py --id 100-001 --uid 00112233445566778899AABB
```

3. Back up `产品表/devices.csv` and the signing private key offline.

The secure MCU package cannot be generated for a real board until its UID is
registered. Do not invent or copy another board's UID: device envelopes are
bound to that 96-bit hardware identity.

For a blank board, create `IDENTITY.SEC` with `tools/make_factory_card.py`, or
program the generated `DEVICE_IDENTITY.hex` at 0x0800C000.

## Build one TF card

Both App images must be separately linked for their slot addresses and be no
larger than 128 KB.

After registering the board, the simplest command is:

```powershell
powershell -ExecutionPolicy Bypass -File make_tf_card.ps1
```

The generated card contains only the signed MCU A/B update:

```powershell
python tools/universal_iap_packager.py `
  --app-a AppOutput/IAP_A/IAP_A.bin `
  --app-b AppOutput/IAP_B/IAP_B.bin
```

Copy the generated `TF_CARD/IAP` directory to a FAT32 SDHC/SDXC card. The
compact boot FAT reader requires DOS 8.3 file names.

## Expected card layout

```text
IAP/UPDATE.SEC
IAP/DEVICES.SEC
IAP/APP_A.ENC
IAP/APP_B.ENC
```
