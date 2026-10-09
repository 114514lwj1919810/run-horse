"""Register one AnimalRace board and create its protected sector-3 identity."""
from __future__ import annotations

import argparse
import csv
import secrets
from datetime import datetime
from pathlib import Path

from secure_common import IDENTITY_ADDRESS, build_identity, intel_hex, label_bytes, parse_uid, read_registry

FIELDS = ["device_id", "uid_hex", "device_key_hex", "status", "notes", "created_at"]


def main() -> None:
    parser = argparse.ArgumentParser(description="登记一台 GD32 赛马机")
    parser.add_argument("--id", required=True, help="设备号，例如 143-001")
    parser.add_argument("--uid", required=True, help="12字节 UID，例如 001122...AABB")
    parser.add_argument("--registry", type=Path, default=Path(__file__).resolve().parents[1] / "产品表" / "devices.csv")
    parser.add_argument("--notes", default="")
    args = parser.parse_args()

    label_bytes(args.id)
    uid = parse_uid(args.uid)
    rows = read_registry(args.registry)
    if any(row["device_id"] == args.id for row in rows):
        raise SystemExit(f"拒绝覆盖：设备号 {args.id} 已存在")
    if any(parse_uid(row["uid_hex"]) == uid for row in rows):
        raise SystemExit("拒绝覆盖：这个 UID 已登记")

    device_key = secrets.token_bytes(32)
    rows.append({
        "device_id": args.id,
        "uid_hex": uid.hex().upper(),
        "device_key_hex": device_key.hex().upper(),
        "status": "active",
        "notes": args.notes,
        "created_at": datetime.now().isoformat(timespec="seconds"),
    })
    with args.registry.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows(rows)

    out = args.registry.parent / "身份文件" / args.id
    out.mkdir(parents=True, exist_ok=True)
    identity = build_identity(uid, args.id, device_key)
    (out / "DEVICE_IDENTITY.bin").write_bytes(identity)
    (out / "DEVICE_IDENTITY.hex").write_text(
        intel_hex([(IDENTITY_ADDRESS, identity)]), encoding="ascii"
    )
    print(f"已登记 {args.id}，UID={uid.hex().upper()}")
    print(f"身份文件：{out}")
    print("重要：立即离线备份 devices.csv；丢失 device_key 后无法再为本机制作升级包。")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, FileNotFoundError, KeyError) as error:
        raise SystemExit(f"错误：{error}")
