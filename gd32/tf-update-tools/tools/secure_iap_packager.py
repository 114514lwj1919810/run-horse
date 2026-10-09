"""Compatibility entry point: v2 packages are universal, not device-specific."""
from universal_iap_packager import main

if __name__ == "__main__":
    try:
        main()
    except (ValueError, FileNotFoundError, KeyError, TypeError) as error:
        raise SystemExit(f"错误：{error}")
