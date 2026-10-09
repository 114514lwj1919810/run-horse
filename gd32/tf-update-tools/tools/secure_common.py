"""Shared formats and cryptographic rules for AnimalRace secure IAP v2."""
from __future__ import annotations

import csv
import hashlib
import hmac
import os
import re
import struct
import zlib
from pathlib import Path

from cryptography.hazmat.primitives.ciphers.aead import AESGCM

MODEL = 0x31305241
IDENTITY_MAGIC = 0x3144494A
PACKAGE_MAGIC = 0x32434553
RECIPIENT_MAGIC = 0x32564544
FORMAT_VERSION = 2
IDENTITY_ADDRESS = 0x0800C000
APP_ADDRESSES = (0x08020000, 0x08040000)
APP_LIMIT = 0x20000
SALT = b"ANIMAL-RACE-SECURE-IAP-v1"
RECIPIENT_CONTEXT = b"ANIMAL-RECIPIENT-v2"
LABEL_RE = re.compile(r"^[0-9]{3}-[0-9]{3}$")

MANIFEST_PREFIX = struct.Struct("<9I12s12s16s16s32s")
MANIFEST = struct.Struct("<9I12s12s16s16s32s64s")
RECIPIENT_HEADER = struct.Struct("<6I")
RECIPIENT_RECORD = struct.Struct("<16s12s32s16s")


def label_bytes(label: str) -> bytes:
    if not LABEL_RE.fullmatch(label):
        raise ValueError("设备编号必须是 143-001 这种 3位-3位 格式")
    return label.encode("ascii") + b"\0"


def parse_uid(value: str) -> bytes:
    compact = value.replace(" ", "").replace("-", "").replace(":", "")
    if len(compact) != 24 or any(c not in "0123456789abcdefABCDEF" for c in compact):
        raise ValueError("UID 必须是按地址递增顺序读取的 12字节/24位十六进制")
    return bytes.fromhex(compact)


def derive_key(device_key: bytes, uid: bytes, label8: bytes, purpose: bytes) -> bytes:
    if len(device_key) != 32 or len(uid) != 12 or len(label8) != 8 or len(purpose) != 3:
        raise ValueError("派生密钥参数长度错误")
    prk = hmac.new(SALT, device_key, hashlib.sha256).digest()
    info = purpose + uid + label8 + struct.pack("<I", MODEL) + b"\x01"
    return hmac.new(prk, info, hashlib.sha256).digest()


def build_identity(uid: bytes, label: str, device_key: bytes) -> bytes:
    prefix = struct.pack(
        "<III12s8s32sI28s", IDENTITY_MAGIC, 1, MODEL,
        uid, label_bytes(label), device_key, 1, bytes(28)
    )
    return prefix + hmac.new(device_key, prefix, hashlib.sha256).digest()


def crc32(data: bytes) -> int:
    return zlib.crc32(data) & 0xFFFFFFFF


def validate_image(image: bytes, slot: int) -> None:
    if not 8 <= len(image) <= APP_LIMIT:
        raise ValueError(f"App {slot} 长度 {len(image)} 超出 8..{APP_LIMIT}")
    sp, reset = struct.unpack_from("<II", image)
    if not (0x20000000 <= sp < 0x20020000 or 0x10000000 <= sp < 0x10010000):
        raise ValueError(f"App {slot} 初始栈指针 0x{sp:08X} 不在 GD32 SRAM")
    base = APP_ADDRESSES[slot]
    if not (base <= (reset & ~1) < base + APP_LIMIT):
        raise ValueError(f"App {slot} 复位向量 0x{reset:08X} 没有链接到 0x{base:08X}")


def recipient_lookup(device_key: bytes, release: int) -> bytes:
    message = RECIPIENT_CONTEXT + struct.pack("<II", MODEL, release)
    return hmac.new(device_key, message, hashlib.sha256).digest()[:16]


def build_recipient_aad(release: int, lookup: bytes, uid: bytes, label8: bytes) -> bytes:
    return struct.pack(
        "<IIII16s12s8s", RECIPIENT_MAGIC, FORMAT_VERSION, MODEL,
        release, lookup, uid, label8
    )


def build_image_aad(release: int, slot: int, size: int, plain_crc: int,
                    recipients_hash: bytes) -> bytes:
    return struct.pack(
        "<IIIIBII32s", PACKAGE_MAGIC, FORMAT_VERSION, MODEL, release,
        slot, size, plain_crc, recipients_hash
    )


def build_recipients(rows: list[dict[str, str]], release: int,
                     content_key: bytes) -> tuple[bytes, list[str]]:
    records: list[bytes] = []
    ids: list[str] = []
    for row in sorted(rows, key=lambda item: item["device_id"]):
        device_id = row["device_id"]
        uid = parse_uid(row["uid_hex"])
        label8 = label_bytes(device_id)
        device_key = bytes.fromhex(row["device_key_hex"])
        if len(device_key) != 32:
            raise ValueError(f"设备 {device_id} 的 device_key_hex 不是32字节")
        lookup = recipient_lookup(device_key, release)
        nonce = os.urandom(12)
        kek = derive_key(device_key, uid, label8, b"KEK")
        wrapped = AESGCM(kek).encrypt(
            nonce, content_key, build_recipient_aad(release, lookup, uid, label8)
        )
        records.append(RECIPIENT_RECORD.pack(lookup, nonce, wrapped[:-16], wrapped[-16:]))
        ids.append(device_id)
    header = RECIPIENT_HEADER.pack(
        RECIPIENT_MAGIC, FORMAT_VERSION, MODEL, release,
        len(records), RECIPIENT_RECORD.size
    )
    return header + b"".join(records), ids


def read_registry(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        rows = list(csv.DictReader(handle))
    seen_id: set[str] = set()
    seen_uid: set[bytes] = set()
    for row in rows:
        device_id = row.get("device_id", "")
        uid = parse_uid(row.get("uid_hex", ""))
        label_bytes(device_id)
        if device_id in seen_id or uid in seen_uid:
            raise ValueError("产品表存在重复设备号或重复 UID")
        seen_id.add(device_id)
        seen_uid.add(uid)
    return rows


def active_devices(path: Path) -> list[dict[str, str]]:
    rows = [row for row in read_registry(path)
            if row.get("status", "active").strip().lower() == "active"]
    if not rows:
        raise ValueError("产品表中没有任何 active 设备")
    return rows


def find_device(path: Path, device_id: str, require_active: bool = True) -> tuple[bytes, bytes]:
    matches = [row for row in read_registry(path) if row.get("device_id") == device_id]
    if len(matches) != 1:
        raise ValueError(f"产品表中必须恰好有一条 {device_id}，当前为 {len(matches)} 条")
    row = matches[0]
    if require_active and row.get("status", "active").lower() != "active":
        raise ValueError(f"设备 {device_id} 状态不是 active")
    key = bytes.fromhex(row["device_key_hex"])
    if len(key) != 32:
        raise ValueError(f"设备 {device_id} 的 device_key_hex 不是32字节")
    return parse_uid(row["uid_hex"]), key


def intel_hex(chunks: list[tuple[int, bytes]]) -> str:
    lines: list[str] = []
    current_upper = None
    for address, data in sorted(chunks):
        for offset in range(0, len(data), 16):
            absolute = address + offset
            upper = absolute >> 16
            if upper != current_upper:
                body = struct.pack(">BHBH", 2, 0, 4, upper)
                lines.append(":" + (body + bytes([(-sum(body)) & 0xFF])).hex().upper())
                current_upper = upper
            block = data[offset:offset + 16]
            low = absolute & 0xFFFF
            body = bytes([len(block), low >> 8, low & 0xFF, 0]) + block
            lines.append(":" + (body + bytes([(-sum(body)) & 0xFF])).hex().upper())
    lines.append(":00000001FF")
    return "\n".join(lines) + "\n"
