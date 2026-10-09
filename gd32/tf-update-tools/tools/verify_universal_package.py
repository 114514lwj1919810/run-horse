"""Verify signature, authorization list, envelopes and both encrypted apps."""
from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

from cryptography.exceptions import InvalidSignature, InvalidTag
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PublicKey
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

from secure_common import (FORMAT_VERSION, MANIFEST, MANIFEST_PREFIX, MODEL,
                           PACKAGE_MAGIC, RECIPIENT_HEADER, RECIPIENT_MAGIC,
                           RECIPIENT_RECORD, active_devices, build_image_aad,
                           build_recipient_aad, crc32, derive_key, label_bytes,
                           parse_uid, read_registry, recipient_lookup,
                           validate_image)


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description="验证一卡多机升级包")
    parser.add_argument("--variant", choices=("FUAH", "NO_FUAH"), default="FUAH")
    parser.add_argument("--package", type=Path)
    parser.add_argument("--registry", type=Path, default=root / "产品表" / "devices.csv")
    parser.add_argument("--public-key", type=Path,
                        default=root / "产品表" / "签名密钥" / "manufacturer_ed25519_public.bin")
    args = parser.parse_args()
    package = args.package or root / "Release" / "通用升级包" / args.variant / "IAP"

    manifest_data = (package / "UPDATE.SEC").read_bytes()
    if len(manifest_data) != MANIFEST.size:
        raise ValueError(f"UPDATE.SEC 长度应为 {MANIFEST.size} 字节")
    fields = MANIFEST.unpack(manifest_data)
    (magic, fmt, model, release, count, size_a, crc_a, size_b, crc_b,
     nonce_a, nonce_b, tag_a, tag_b, recipients_hash, signature) = fields
    if (magic, fmt, model) != (PACKAGE_MAGIC, FORMAT_VERSION, MODEL):
        raise ValueError("UPDATE.SEC 型号或版本不匹配")
    public = Ed25519PublicKey.from_public_bytes(args.public_key.read_bytes())
    try:
        public.verify(signature, manifest_data[:MANIFEST_PREFIX.size])
    except InvalidSignature as error:
        raise ValueError("UPDATE.SEC 厂家数字签名无效") from error

    recipients_data = (package / "DEVICES.SEC").read_bytes()
    if hashlib.sha256(recipients_data).digest() != recipients_hash:
        raise ValueError("DEVICES.SEC 哈希与已签名清单不一致")
    if len(recipients_data) < RECIPIENT_HEADER.size:
        raise ValueError("DEVICES.SEC 太短")
    header = RECIPIENT_HEADER.unpack_from(recipients_data)
    if header != (RECIPIENT_MAGIC, FORMAT_VERSION, MODEL, release, count, RECIPIENT_RECORD.size):
        raise ValueError("DEVICES.SEC 文件头不匹配")
    expected_size = RECIPIENT_HEADER.size + count * RECIPIENT_RECORD.size
    if len(recipients_data) != expected_size:
        raise ValueError("DEVICES.SEC 记录数与文件长度不匹配")

    rows = read_registry(args.registry)
    active = active_devices(args.registry)
    if count != len(active):
        raise ValueError("升级包的授权数与当前 active 产品表不一致")
    records = [RECIPIENT_RECORD.unpack_from(
        recipients_data, RECIPIENT_HEADER.size + index * RECIPIENT_RECORD.size
    ) for index in range(count)]
    by_lookup = {record[0]: record for record in records}
    if len(by_lookup) != len(records):
        raise ValueError("DEVICES.SEC 存在重复收件人记录")

    content_key = None
    for row in active:
        uid = parse_uid(row["uid_hex"])
        label8 = label_bytes(row["device_id"])
        device_key = bytes.fromhex(row["device_key_hex"])
        lookup = recipient_lookup(device_key, release)
        record = by_lookup.get(lookup)
        if record is None:
            raise ValueError(f"缺少启用设备 {row['device_id']} 的授权记录")
        _, nonce, wrapped, tag = record
        kek = derive_key(device_key, uid, label8, b"KEK")
        try:
            unwrapped = AESGCM(kek).decrypt(
                nonce, wrapped + tag,
                build_recipient_aad(release, lookup, uid, label8),
            )
        except InvalidTag as error:
            raise ValueError(f"设备 {row['device_id']} 密钥信封认证失败") from error
        if content_key is None:
            content_key = unwrapped
        elif content_key != unwrapped:
            raise ValueError("不同设备信封解出的固件密钥不一致")

    for row in rows:
        if row.get("status", "active").lower() == "active":
            continue
        lookup = recipient_lookup(bytes.fromhex(row["device_key_hex"]), release)
        if lookup in by_lookup:
            raise ValueError(f"停用设备 {row['device_id']} 仍出现在授权清单中")

    if content_key is None:
        raise ValueError("没有可用的固件密钥")
    for slot, values in enumerate((("APP_A.ENC", size_a, crc_a, nonce_a, tag_a),
                                   ("APP_B.ENC", size_b, crc_b, nonce_b, tag_b))):
        name, size, expected_crc, nonce, tag = values
        ciphertext = (package / name).read_bytes()
        if len(ciphertext) != size:
            raise ValueError(f"{name} 长度不匹配")
        try:
            plain = AESGCM(content_key).decrypt(
                nonce, ciphertext + tag,
                build_image_aad(release, slot, size, expected_crc, recipients_hash),
            )
        except InvalidTag as error:
            raise ValueError(f"{name} 认证失败") from error
        validate_image(plain, slot)
        if crc32(plain) != expected_crc:
            raise ValueError(f"{name} 明文 CRC 错误")
    print(f"验证通过：{args.variant}，授权 {count} 台，release=0x{release:08X}")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, FileNotFoundError, KeyError, TypeError) as error:
        raise SystemExit(f"错误：{error}")
