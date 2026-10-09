"""Add one board's provisioning record to a copy of the universal package."""
from __future__ import annotations

import argparse
import shutil
from pathlib import Path

from secure_common import (APP_ADDRESSES, IDENTITY_ADDRESS, build_identity,
                           find_device, intel_hex, validate_image)


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description="生成一块空板的首次初始化卡和合并 HEX")
    parser.add_argument("--id", required=True)
    parser.add_argument("--variant", choices=("FUAH", "NO_FUAH"), default="FUAH")
    parser.add_argument("--registry", type=Path, default=root / "产品表" / "devices.csv")
    args = parser.parse_args()
    uid, key = find_device(args.registry, args.id)
    identity = build_identity(uid, args.id, key)
    source_iap = root / "TF_CARD" / "IAP"
    if not (source_iap / "UPDATE.SEC").exists():
        raise FileNotFoundError("请先生成当前版本的通用升级包")
    out = root / "Factory" / args.id
    iap = out / "IAP"
    if iap.exists():
        shutil.rmtree(iap)
    shutil.copytree(source_iap, iap)
    # Bootloader's compact FAT32 reader supports DOS 8.3 names only.
    (iap / "IDENTITY.SEC").write_bytes(identity)
    (out / "DEVICE_IDENTITY.bin").write_bytes(identity)
    (out / "DEVICE_IDENTITY.hex").write_text(
        intel_hex([(IDENTITY_ADDRESS, identity)]), encoding="ascii"
    )

    app_root = root / "AppOutput"
    if args.variant == "FUAH":
        app_a_path = app_root / "IAP_A" / "IAP_A.bin"
    else:
        app_a_path = app_root / "NO_LOCK_IAP_A" / "NO_LOCK_IAP_A.bin"
    app_a = app_a_path.read_bytes()
    validate_image(app_a, 0)
    for build in ("DEV", "PROD"):
        boot_path = root / "bootloader" / "Output" / f"GD_BOOT_{build}" / f"GD_BOOT_{build}.bin"
        if not boot_path.exists():
            continue
        boot = boot_path.read_bytes()
        (out / f"首次烧录_{build}.hex").write_text(
            intel_hex([(0x08000000, boot), (APP_ADDRESSES[0], app_a),
                       (IDENTITY_ADDRESS, identity)]), encoding="ascii"
        )
    print(f"新板初始化资料已生成：{out}")
    print("此 IAP 目录含本机 IDENTITY.SEC，不要用它初始化其他空板。")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, FileNotFoundError, KeyError) as error:
        raise SystemExit(f"错误：{error}")
