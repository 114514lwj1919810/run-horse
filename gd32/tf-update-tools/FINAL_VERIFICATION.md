# Final verification

## GD32 bootloader

- `GD_BOOT_DEV`: 0 errors, 0 warnings; Code 20916, RO-data 5696,
  RW-data 12, ZI-data 5156; binary 26624 bytes.
- `GD_BOOT_PROD`: 0 errors, 0 warnings; Code 19988, RO-data 5680,
  RW-data 12, ZI-data 5156; binary 25680 bytes.
- `boot_apk.c/.h`, `BootApk_TryTransfer` and the USART2 APK baud switching
  path have been removed from both targets.
- FAT32 access, device envelopes, Ed25519 verification, AES-256-GCM image
  decryption, A/B programming, trial confirmation and rollback remain linked.
- The application jump now restores global interrupts after clearing the
  bootloader IRQ state. This is required for the application SysTick, PC13
  heartbeat, protocol timers and trial confirmation to run.

## AnimalRace A/B application

- A image: 40896 bytes, initial SP `0x20008450`, reset `0x0802019D`.
- B image: 40896 bytes, initial SP `0x20008450`, reset `0x0804019D`.
- Both images remain below the 128 KiB slot limit.
- Factory DEV image: 171968 bytes; it contains the rebuilt DEV bootloader and
  Application A while leaving identity and metadata gaps erased (`0xFF`).
- Factory image SHA-256:
  `05D1DB876186D267F58062EA91DFB1455E6DC7C24803BF23E4BED4047162A465`.

## TF firmware package

- `make_tf_card.ps1` and `universal_iap_packager.py` no longer accept an APK.
- A synthetic registered-device packaging test completed successfully.
- The resulting `IAP` directory contained exactly `UPDATE.SEC`,
  `DEVICES.SEC`, `APP_A.ENC` and `APP_B.ENC`.
- The production registry currently has no active board, so a production card
  still requires device registration before packaging.

## Android and Unity

- The Android AAR rebuilt successfully and is 6736 bytes.
- AAR SHA-256:
  `AE1B0BB112370F09ADCDA23DC32BBBBD63F2ADF9451EF1F2B4AF8F27A763F675`.
- Public bridge API is limited to normal serial configuration, fixed-115200
  open, connection status, text send and close.
- Raw APK frames, 2000000-baud switching, APK caching, PackageInstaller,
  `REQUEST_INSTALL_PACKAGES` and `InstallResultReceiver` are absent.
- The rebuilt AAR has been copied to the Unity project's
  `Assets/Plugins/Android/serialplugin-release.aar`.
- Unity 2018 completed the full Android batch build successfully with the new
  AAR and `AndroidSerialService.cs`.
- APK package: `com.example.animalrace`, version `0.1` (code 1), minSdk 19,
  targetSdk 26; size 24296950 bytes.
- APK SHA-256:
  `7951E104CB1691AAA15BBD2FA752B2ED3B3D5BF293260C8D66AE033D8D5654B9`.
- The APK is signed with APK Signature Scheme v1 and v2. Its manifest has no
  `REQUEST_INSTALL_PACKAGES` permission or APK-install receiver.
- Exact scans of `classes.dex` and `Assembly-CSharp.dll` found no former APK
  serial-upgrade commands, callbacks, package-installer classes or
  `ARACE.APK` references. Normal `/dev/ttyS2`, serial receive and text-send
  symbols remain present.
- Unity EditMode tests: 32 passed and 4 failed. All four failures are the same
  pre-existing visual-layout expectation in
  `PhotoAlignedStartFitsEveryAnimalAndCanReachTheFinish`: expected non-winner
  finish X 300, current race plan returns 379.9999. No failed test exercises
  the serial bridge, protocol, APK removal or Android packaging.

## Resulting responsibility split

- TF card and GD32 bootloader: GD32 firmware only.
- USART2 PB10/PB11 at 115200: normal GD32/Unity game protocol only.
- Android board USB port: APK installation, implemented independently from
  this serial plugin.
