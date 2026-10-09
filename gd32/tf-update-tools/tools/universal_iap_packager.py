"""Build one signed TF package containing MCU A/B images."""
from __future__ import annotations

import argparse
import csv
import hashlib
import os
import time
from datetime import datetime
from pathlib import Path

from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

from secure_common import (FORMAT_VERSION, MANIFEST, MANIFEST_PREFIX, MODEL,
                           PACKAGE_MAGIC, active_devices, build_image_aad,
                           build_recipients, crc32, validate_image)


def load_private(path: Path) -> Ed25519PrivateKey:
    key = serialization.load_pem_private_key(path.read_bytes(), password=None)
    if not isinstance(key, Ed25519PrivateKey):
        raise ValueError("厂家私钥不是 Ed25519")
    return key


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description="生成赛马机一张卡升级包")
    parser.add_argument("--variant", choices=("FUAH", "NO_FUAH"), default="FUAH")
    parser.add_argument("--app-a", type=Path)
    parser.add_argument("--app-b", type=Path)
    parser.add_argument("--registry", type=Path, default=root / "产品表" / "devices.csv")
    parser.add_argument("--private-key", type=Path,
                        default=root / "产品表" / "签名密钥" / "manufacturer_ed25519_private.pem")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    app_root = root / "AppOutput"
    default_a = app_root / ("IAP_A" if args.variant == "FUAH" else "NO_LOCK_IAP_A")
    default_b = app_root / ("IAP_B" if args.variant == "FUAH" else "NO_LOCK_IAP_B")
    app_a_path = args.app_a or default_a / ("IAP_A.bin" if args.variant == "FUAH" else "NO_LOCK_IAP_A.bin")
    app_b_path = args.app_b or default_b / ("IAP_B.bin" if args.variant == "FUAH" else "NO_LOCK_IAP_B.bin")
    apps = [app_a_path.read_bytes(), app_b_path.read_bytes()]
    for slot, image in enumerate(apps):
        validate_image(image, slot)

    rows = active_devices(args.registry)
    private = load_private(args.private_key)
    release = int(time.time()) & 0xFFFFFFFF
    if release == 0:
        release = 1
    content_key = os.urandom(32)
    recipients, ids = build_recipients(rows, release, content_key)
    recipients_hash = hashlib.sha256(recipients).digest()
    sizes = [len(image) for image in apps]
    crcs = [crc32(image) for image in apps]
    nonces = [os.urandom(12), os.urandom(12)]
    ciphertexts: list[bytes] = []
    tags: list[bytes] = []
    for slot, image in enumerate(apps):
        aad = build_image_aad(release, slot, sizes[slot], crcs[slot], recipients_hash)
        encrypted = AESGCM(content_key).encrypt(nonces[slot], image, aad)
        ciphertexts.append(encrypted[:-16])
        tags.append(encrypted[-16:])

    prefix = MANIFEST_PREFIX.pack(
        PACKAGE_MAGIC, FORMAT_VERSION, MODEL, release, len(ids),
        sizes[0], crcs[0], sizes[1], crcs[1], nonces[0], nonces[1],
        tags[0], tags[1], recipients_hash
    )
    manifest = prefix + private.sign(prefix)
    if len(manifest) != MANIFEST.size:
        raise ValueError("内部错误：UPDATE.SEC 长度不正确")

    out = args.output or root / "TF_CARD"
    iap = out / "IAP"
    iap.mkdir(parents=True, exist_ok=True)
    for old in iap.glob("*"):
        if old.is_file():
            old.unlink()
    (iap / "APP_A.ENC").write_bytes(ciphertexts[0])
    (iap / "APP_B.ENC").write_bytes(ciphertexts[1])
    (iap / "DEVICES.SEC").write_bytes(recipients)
    (iap / "UPDATE.SEC").write_bytes(manifest)
    # Do not export a plaintext ID list beside the card image.  Operators can
    # view active/inactive devices through manage_devices.py without creating
    # an easy-to-copy authorization roster.
    old_plaintext_list = out / "AUTHORIZED_DEVICES.txt"
    if old_plaintext_list.exists():
        old_plaintext_list.unlink()
    (out / "PACKAGE_INFO.txt").write_text(
        f"variant={args.variant}\nrelease=0x{release:08X}\nauthorized_count={len(ids)}\n"
        f"app_a_size={sizes[0]}\napp_a_crc32=0x{crcs[0]:08X}\n"
        f"app_b_size={sizes[1]}\napp_b_crc32=0x{crcs[1]:08X}\n"
        "format=AES-256-GCM + per-device key envelopes + Ed25519\n",
        encoding="utf-8",
    )
    history = root / "产品表" / "universal_package_history.csv"
    exists = history.exists() and history.stat().st_size > 0
    with history.open("a", encoding="utf-8-sig", newline="") as handle:
        fields = ["generated_at", "variant", "release", "authorized_count",
                  "app_a_sha256", "app_b_sha256", "output_path"]
        writer = csv.DictWriter(handle, fieldnames=fields)
        if not exists:
            writer.writeheader()
        writer.writerow({
            "generated_at": datetime.now().isoformat(timespec="seconds"),
            "variant": args.variant,
            "release": f"0x{release:08X}",
            "authorized_count": len(ids),
            "app_a_sha256": hashlib.sha256(apps[0]).hexdigest().upper(),
            "app_b_sha256": hashlib.sha256(apps[1]).hexdigest().upper(),
            "output_path": str(out.resolve()),
        })
    print(f"通用包生成完成：{out}")
    print(f"授权设备：{len(ids)} 台，release=0x{release:08X}")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, FileNotFoundError, KeyError, TypeError) as error:
        raise SystemExit(f"错误：{error}")
