"""List devices and change the active/inactive package authorization state."""
from __future__ import annotations

import argparse
import csv
from pathlib import Path

from secure_common import read_registry

FIELDS = ["device_id", "uid_hex", "device_key_hex", "status", "notes", "created_at"]


def write_registry(path: Path, rows: list[dict[str, str]]) -> None:
    temporary = path.with_suffix(".tmp")
    with temporary.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows({field: row.get(field, "") for field in FIELDS} for row in rows)
    temporary.replace(path)


def show(rows: list[dict[str, str]]) -> None:
    print("\n设备编号      状态       UID")
    print("------------  ---------  ------------------------")
    for row in sorted(rows, key=lambda item: item["device_id"]):
        state = "启用" if row.get("status", "active").lower() == "active" else "停用"
        print(f"{row['device_id']:<12}  {state:<9}  {row['uid_hex']}")
    active = sum(row.get("status", "active").lower() == "active" for row in rows)
    print(f"\n共 {len(rows)} 台，启用 {active} 台，停用 {len(rows)-active} 台。")


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser()
    parser.add_argument("--registry", type=Path, default=root / "产品表" / "devices.csv")
    parser.add_argument("--list", action="store_true")
    parser.add_argument("--set", choices=("active", "inactive"))
    parser.add_argument("--ids", help="逗号分隔的设备编号")
    args = parser.parse_args()
    rows = read_registry(args.registry)
    if args.set:
        wanted = {value.strip() for value in (args.ids or "").split(",") if value.strip()}
        known = {row["device_id"] for row in rows}
        missing = sorted(wanted - known)
        if not wanted:
            raise ValueError("没有输入设备编号")
        if missing:
            raise ValueError("产品表中找不到：" + ", ".join(missing))
        for row in rows:
            if row["device_id"] in wanted:
                row["status"] = args.set
        write_registry(args.registry, rows)
        print(f"已将 {len(wanted)} 台设备设为 {args.set}。")
    show(rows)


if __name__ == "__main__":
    try:
        main()
    except (ValueError, FileNotFoundError, KeyError) as error:
        raise SystemExit(f"错误：{error}")
